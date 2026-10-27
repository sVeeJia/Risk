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
  t.mock.method(globalThis, 'fetch', async (path, options) => {
    paths.push(path)
    assert.equal(options.method, 'GET')
    assert.equal(options.credentials, 'same-origin')
    return Response.json(path === '/api/games' ? [game] : game)
  })
  assert.deepEqual(await getGame('game-1'), game)
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
