import test from 'node:test'
import assert from 'node:assert/strict'
import { mergeEvents, newerGame, loadHistory, shareUrl } from '../spectatorEvents.js'
import { createSpectatorQueue } from '../spectatorQueue.js'
import { spectateSession, isGameNotFound } from '../spectatorSession.js'
import { battlePresentation, emptyBattle } from '../battlePresentation.js'

const event = sequence => ({ sequence, type: 'TurnSkipped' })
const game = version => ({ id: 'g', version, round: 1, phase: 'Action', currentPlayerId: 'human' })
const deferred = () => { let resolve; const promise = new Promise(r => { resolve = r }); return { promise, resolve } }
const settle = async () => { for (let i = 0; i < 30; i++) await Promise.resolve() }

test('merge ignores old sequences and duplicates, sorts, and preserves gaps', () => {
  const old = [event(1), event(2)]
  const result = mergeEvents([...old, event(8), event(4), event(4), null, { sequence: '5' }], 2)
  assert.deepEqual(result.events.map(e => e.sequence), [4, 8])
  assert.equal(result.lastSeenSequence, 8)
  assert.deepEqual(old.map(e => e.sequence), [1, 2])
  assert.deepEqual(mergeEvents([], 8), { events: [], lastSeenSequence: 8 })
})

test('history pages bare arrays until the first short page', async () => {
  const calls = []
  const history = await loadHistory(async (after, limit) => {
    calls.push([after, limit])
    return after === 0 ? [event(1), event(2)] : after === 2 ? [event(3), event(4)] : [event(7)]
  }, 0, 2)
  assert.deepEqual(calls, [[0, 2], [2, 2], [4, 2]])
  assert.deepEqual(history.map(e => e.sequence), [1, 2, 3, 4, 7])
})

test('history handles an empty tail, bad shapes, repeated full pages and network errors', async () => {
  assert.deepEqual(await loadHistory(async () => [], 8), [])
  await assert.rejects(loadHistory(async () => ({ items: [] })), /Unexpected/)
  await assert.rejects(loadHistory(async () => [event(1)], 1, 1), /did not advance/)
  await assert.rejects(loadHistory(async () => { throw Error('offline') }), /offline/)
})

test('snapshots only advance by numeric game.version; unversioned data is reported', () => {
  const current = game(5)
  assert.equal(newerGame(current, game(4)), current)
  assert.equal(newerGame(current, game(5)), current)
  assert.equal(newerGame(current, game(6)).version, 6)
  assert.equal(newerGame(current, null), current)
  assert.throws(() => newerGame(current, { id: 'g' }), /game.version/)
})

test('share URLs use the public watch route and encode the game id', () => {
  assert.equal(shareUrl('https://risk.example/', 'a/b ?'), 'https://risk.example/watch/a%2Fb%20%3F')
  assert.equal(shareUrl('https://risk.example/', 'a/b ?', 'Finished'), 'https://risk.example/replay/a%2Fb%20%3F')
})

test('queued actions replay serially against old snapshots, deduplicate and never roll versions back', async () => {
  const controller = new AbortController(), gate = deferred(), seen = [], versions = [], busy = []
  let first = true
  const queue = createSpectatorQueue({ game: game(1), lastSeenSequence: 0, signal: controller.signal,
    pause: async () => { if (first) { first = false; await gate.promise } },
    onEvent: e => seen.push(e.sequence), onGame: g => versions.push(g.version), onBusy: b => busy.push(b), onError: assert.fail,
  })
  const work = queue.enqueue({ game: game(2), events: [event(1), event(2)] })
  queue.enqueue({ game: game(4), events: [event(2), event(3)] })
  queue.enqueue({ game: game(3), events: [event(3)] })
  assert.deepEqual(seen, [1])
  assert.deepEqual(versions, [])
  assert.equal(queue.lastSeenSequence, 3)
  gate.resolve(); await work
  assert.deepEqual(seen, [1, 2, 3])
  assert.deepEqual(versions, [2, 4])
  assert.deepEqual(busy, [true, false])
})

test('queue retains attack arrows across separate human actions and stops when cancelled', async () => {
  const controller = new AbortController(), arrows = []
  const queue = createSpectatorQueue({ game: game(1), lastSeenSequence: 0, signal: controller.signal,
    pause: async () => {}, onBusy: () => {}, onGame: () => {}, onError: assert.fail,
    onEvent: (e, arrow) => arrows.push([e.type, arrow?.kind ?? null]),
  })
  await queue.enqueue({ game: game(2), events: [{ sequence: 1, type: 'AttackStarted', data: { fromCountryId: 'a', toCountryId: 'b' } }] })
  await queue.enqueue({ game: game(3), events: [{ sequence: 2, type: 'BattleRound' }] })
  await queue.enqueue({ game: game(4), events: [{ sequence: 3, type: 'AttackEnded' }] })
  controller.abort()
  await queue.enqueue({ game: game(5), events: [event(4)] })
  assert.deepEqual(arrows, [['AttackStarted', 'attack'], ['BattleRound', 'attack'], ['AttackEnded', null]])
})

test('cancelling mid replay never commits the pending snapshot', async () => {
  const controller = new AbortController(), gate = deferred(), committed = [], shown = []
  const queue = createSpectatorQueue({ game: game(1), lastSeenSequence: 0, signal: controller.signal,
    pause: () => gate.promise, onBusy: () => {}, onGame: g => committed.push(g), onError: assert.fail, onEvent: e => shown.push(e.sequence),
  })
  const work = queue.enqueue({ game: game(2), events: [event(1), event(2)] })
  controller.abort(); gate.resolve(); await work
  assert.deepEqual(committed, [])
  assert.deepEqual(shown, [1])
})

test('an authoritative snapshot clears only count overrides and preserves the last dice result', () => {
  const state = { ...emptyBattle, liveCounts: { a: 3 }, board: { round: 2, visible: true } }
  const next = battlePresentation(state, { type: 'snapshot', game: { activeAttack: null } })
  assert.deepEqual(next.liveCounts, {})
  assert.equal(next.board, state.board)
  assert.equal(next.attack, null)
})

function fakeHub() {
  const hooks = {}, calls = []
  return { hooks, calls, on: (name, fn) => { hooks[name] = fn },
    onreconnecting: fn => { hooks.reconnecting = fn }, onreconnected: fn => { hooks.reconnected = fn }, onclose: fn => { hooks.close = fn },
    start: async () => { calls.push('start') }, invoke: async (name, id) => { calls.push([name, id]) },
    stop: async () => { calls.push('stop') },
  }
}

test('subscription buffers updates during catch-up and rejoins/catches up after reconnect', async () => {
  const controller = new AbortController(), hub = fakeHub(), log = [], updates = [], statuses = []
  let cursor = 1, snapshotCalls = 0, pages = 0
  const work = spectateSession({ id: 'g', signal: controller.signal,
    getGame: async () => { log.push('game'); return game(++snapshotCalls) },
    getEvents: async (id, after) => {
      log.push(['events', after]); pages++
      if (pages === 1) return [event(1)]
      if (pages === 2) { hub.hooks.GameUpdated({ game: game(3), events: [event(3)] }); return [event(2), event(3)] }
      return [event(4)]
    },
    createConnection: () => hub, onInitial: (g, events) => log.push(['initial', g.version, events.length]),
    createQueue: () => ({ get lastSeenSequence() { return cursor }, enqueue: update => { updates.push(update); cursor = mergeEvents(update.events, cursor).lastSeenSequence } }),
    onStatus: s => statuses.push(s), onError: assert.fail,
  })
  await settle()
  assert.deepEqual(log.slice(0, 3), ['game', ['events', 0], ['initial', 1, 1]])
  assert.deepEqual(hub.calls, ['start', ['Watch', 'g']])
  assert.deepEqual(updates[0].events.map(e => e.sequence), [2, 3])
  assert.equal(updates[0].game.version, 3)
  hub.hooks.reconnecting(); hub.hooks.reconnected(); await settle()
  assert.deepEqual(updates[1].events.map(e => e.sequence), [4])
  assert.ok(log.some(entry => Array.isArray(entry) && entry[0] === 'events' && entry[1] === 3))
  assert.deepEqual(statuses, ['Reconnecting…', 'Live', 'Reconnecting…', 'Live'])
  controller.abort(); await work
  assert.deepEqual(hub.calls.slice(-2), [['StopWatching', 'g'], 'stop'])
})

test('404 and hub not-found errors are recognized, and failed subscription is stopped', async () => {
  assert.equal(isGameNotFound({ status: 404 }), true)
  assert.equal(isGameNotFound(Error('HubException: Game not found.')), true)
  assert.equal(isGameNotFound(Error('offline')), false)
  const hub = fakeHub(), errors = []
  hub.invoke = async name => { if (name === 'Watch') throw Error('Game not found.') }
  await spectateSession({ id: 'missing', signal: new AbortController().signal, getGame: async () => game(1), getEvents: async () => [],
    createConnection: () => hub, createQueue: () => ({}), onInitial: () => {}, onStatus: () => {}, onError: e => errors.push(e),
  })
  assert.equal(isGameNotFound(errors[0]), true)
  assert.equal(hub.calls.at(-1), 'stop')
})

test('a cancelled initial lifetime never creates a hub connection', async () => {
  const controller = new AbortController(), gate = deferred()
  const work = spectateSession({ id: 'g', signal: controller.signal, getGame: () => gate.promise, getEvents: async () => [],
    createConnection: assert.fail, onInitial: assert.fail, onStatus: assert.fail, onError: assert.fail,
  })
  controller.abort(); gate.resolve(game(1)); await work
})

test('unmount stops an in-flight Watch immediately and only once', async () => {
  const controller = new AbortController(), gate = deferred(), hub = fakeHub()
  hub.invoke = async name => { hub.calls.push(name); if (name === 'Watch') await gate.promise }
  const work = spectateSession({ id: 'g', signal: controller.signal, getGame: async () => game(1), getEvents: async () => [],
    createConnection: () => hub, createQueue: () => ({}), onInitial: () => {}, onStatus: () => {}, onError: assert.fail,
  })
  await settle(); controller.abort()
  assert.deepEqual(hub.calls, ['start', 'Watch', 'StopWatching', 'stop'])
  gate.resolve(); await work
  assert.equal(hub.calls.filter(call => call === 'stop').length, 1)
})

test('a finished watch can hand off to replay before opening a hub connection', async () => {
  await spectateSession({ id: 'g', signal: new AbortController().signal, getGame: async () => ({ ...game(1), phase: 'Finished' }), getEvents: async () => [],
    createConnection: assert.fail, onInitial: () => false, onStatus: assert.fail, onError: assert.fail,
  })
})
