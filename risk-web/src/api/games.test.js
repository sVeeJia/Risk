import assert from 'node:assert/strict'
import { test } from 'node:test'
import { ApiError } from './client.js'
import { getGame, listGames, startGame } from './games.js'

const game = { id: 'game-1', territories: [], players: [] }

test('startGame posts only the starting country as JSON with the auth cookie and signal', async t => {
  const controller = new AbortController()
  t.mock.method(globalThis, 'fetch', async (path, options) => {
    assert.equal(path, '/api/games')
    assert.equal(options.method, 'POST')
    assert.equal(options.headers['Content-Type'], 'application/json')
    assert.deepEqual(JSON.parse(options.body), { startingCountryId: 'ZA' })
    assert.equal(options.credentials, 'same-origin')
    assert.equal(options.signal, controller.signal)
    return Response.json(game, { status: 201 })
  })
  assert.deepEqual(await startGame('ZA', { signal: controller.signal }), game)
})

test('getGame and listGames use the expected authenticated GET endpoints', async t => {
  const paths = []
  const controller = new AbortController()
  t.mock.method(globalThis, 'fetch', async (path, options) => {
    paths.push(path)
    assert.equal(options.method, 'GET')
    assert.equal(options.credentials, 'same-origin')
    if (path !== '/api/games') assert.equal(options.signal, controller.signal)
    return Response.json(path === '/api/games' ? [game] : game)
  })
  assert.deepEqual(await getGame('game-1', { signal: controller.signal }), game)
  assert.deepEqual(await listGames(), [game])
  assert.deepEqual(paths, ['/api/games/game-1', '/api/games'])
})

test('failed game creation exposes ApiError status and message for non-2xx responses', async t => {
  for (const status of [400, 401, 500]) {
    const message = `Game request failed (${status})`
    const mock = t.mock.method(globalThis, 'fetch', async () => Response.json({ message }, { status }))
    await assert.rejects(startGame('ZA'), error => error instanceof ApiError && error.status === status && error.message === message)
    mock.mock.restore()
  }
})

test('failed connection exposes ApiError and cancellation reaches the in-flight POST', async t => {
  const mock = t.mock.method(globalThis, 'fetch', async () => { throw new TypeError('Network unavailable') })
  await assert.rejects(startGame('ZA'), error => error instanceof ApiError && error.status === 0 && Boolean(error.message))
  mock.mock.restore()
  const controller = new AbortController()
  t.mock.method(globalThis, 'fetch', (_path, options) => new Promise((_resolve, reject) => {
    options.signal.addEventListener('abort', () => reject(options.signal.reason), { once: true })
  }))
  const pending = startGame('ZA', { signal: controller.signal })
  controller.abort()
  await assert.rejects(pending, { name: 'AbortError' })
})

import { attack, rollAttack, stopAttack, transferConquest, placeReinforcements, transfer, endTurn, playNpcTurn, getGameEvents, loadGameEvents } from './games.js'

test('game actions send exact endpoint bodies, cookies and cancellation signals', async t => {
  const controller = new AbortController()
  const response = { game, events: [{ sequence: 1, message: 'Server event' }] }
  const cases = [
    [attack, 'attack', { fromCountryId: 'ZA', toCountryId: 'US' }],
    [rollAttack, 'attack/roll'], [stopAttack, 'attack/stop'],
    [transferConquest, 'attack/transfer', { soldiers: 2 }],
    [placeReinforcements, 'reinforcements', { countryId: 'ZA', soldiers: 3 }],
    [transfer, 'transfer', { fromCountryId: 'ZA', toCountryId: 'US', soldiers: 2 }],
    [endTurn, 'end-turn'], [playNpcTurn, 'npc-turn'],
  ]
  for (const [action, suffix, body] of cases) {
    const mock = t.mock.method(globalThis, 'fetch', async (path, options) => {
      assert.equal(path, `/api/games/game%2F1/${suffix}`)
      assert.equal(options.method, 'POST')
      assert.equal(options.credentials, 'same-origin')
      assert.equal(options.signal, controller.signal)
      assert.deepEqual(options.body ? JSON.parse(options.body) : undefined, body)
      return Response.json(response)
    })
    const options = { signal: controller.signal }
    assert.deepEqual(await (body ? action('game/1', body, options) : action('game/1', options)), response)
    mock.mock.restore()
  }
})

test('event history uses after and limit query parameters', async t => {
  const controller = new AbortController()
  const events = [{ sequence: 13, message: 'An NPC chose to attack', type: 'NpcDecision', justification: 'The target is weak' }]
  t.mock.method(globalThis, 'fetch', async (path, options) => {
    assert.equal(path, '/api/games/game-1/events?after=12&limit=50')
    assert.equal(options.signal, controller.signal)
    assert.equal(options.method, 'GET')
    assert.equal(options.credentials, 'same-origin')
    return Response.json(events)
  })
  assert.deepEqual(await getGameEvents('game-1', { after: 12, limit: 50, signal: controller.signal }), events)
})

test('history loads bare event arrays using the last sequence as its cursor', async t => {
  const first = Array.from({ length: 200 }, (_, i) => ({ sequence: i + 1, message: `Event ${i + 1}` }))
  const last = [{ sequence: 201, message: 'Last event' }]
  const paths = []
  t.mock.method(globalThis, 'fetch', async path => {
    paths.push(path)
    return Response.json(paths.length === 1 ? first : last)
  })
  assert.deepEqual(await loadGameEvents('game-1'), [...first, ...last])
  assert.deepEqual(paths, ['/api/games/game-1/events?after=0&limit=200', '/api/games/game-1/events?after=200&limit=200'])
})

test('unexpected event shapes produce an empty optional history', async t => {
  for (const data of [null, {}, { items: [] }, [null], [{ sequence: 1 }]]) {
    const mock = t.mock.method(globalThis, 'fetch', async () => Response.json(data))
    await assert.rejects(getGameEvents('game-1'), TypeError)
    assert.deepEqual(await loadGameEvents('game-1'), [])
    mock.mock.restore()
  }
})

test('event HTTP and network failures leave an empty history while the game still loads', async t => {
  for (const failure of [404, 500, 'network']) {
    const mock = t.mock.method(globalThis, 'fetch', async path => {
      if (!path.includes('/events?')) return Response.json(game)
      if (failure === 'network') throw new TypeError('Network unavailable')
      return Response.json({ message: 'History unavailable' }, { status: failure })
    })
    assert.deepEqual(await Promise.all([getGame('game-1'), loadGameEvents('game-1')]), [game, []])
    mock.mock.restore()
  }
})

test('a failed later history page discards partial history', async t => {
  let calls = 0
  t.mock.method(globalThis, 'fetch', async () => ++calls === 1
    ? Response.json(Array.from({ length: 200 }, (_, i) => ({ sequence: i + 1, message: 'Event' })))
    : Response.json({ message: 'History unavailable' }, { status: 500 }))
  assert.deepEqual(await loadGameEvents('game-1'), [])
})

test('history stops safely if the server repeats a page', async t => {
  t.mock.method(globalThis, 'fetch', async () => Response.json(Array.from({ length: 200 }, (_, i) => ({ sequence: i + 1, message: 'Event' }))))
  assert.deepEqual(await loadGameEvents('game-1'), [])
})

test('game actions preserve server rejection messages', async t => {
  for (const status of [400, 409, 404]) {
    const mock = t.mock.method(globalThis, 'fetch', async () => Response.json({ message: 'Server decides this choice is invalid' }, { status }))
    await assert.rejects(attack('game-1', { fromCountryId: 'ZA', toCountryId: 'US' }), error => error.status === status && error.message === 'Server decides this choice is invalid')
    mock.mock.restore()
  }
})
