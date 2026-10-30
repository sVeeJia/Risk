import assert from 'node:assert/strict'
import { test } from 'node:test'
import { replayNpcTurns } from './npcPlayback.js'

const players = [{ id: 'human', isHuman: true }, { id: 'npc1' }, { id: 'npc2' }]
const game = currentPlayerId => ({ currentPlayerId, phase: 'Action', players })
test('NPC turns replay every event against old state before committing the server game', async () => {
  const trace = []
  let board = game('npc1')
  const next = game('npc2')
  const human = game('human')
  const responses = [
    { game: next, events: [{ type: 'NpcDecision', justification: 'Why', sequence: 1 }, { type: 'AttackStarted', sequence: 2, data: { fromCountryId: 'ZA', toCountryId: 'US' } }, { type: 'BattleRound', sequence: 3 }, { type: 'AttackEnded', sequence: 4 }] },
    { game: human, events: [{ type: 'TurnSkipped', sequence: 5 }] },
  ]
  await replayNpcTurns({ game: board, isActive: () => true,
    request: async () => { trace.push('request'); return responses.shift() },
    pause: async kind => trace.push(kind),
    onEvent: (event, arrow) => { trace.push(event.sequence); assert.equal(board.currentPlayerId, event.sequence <= 4 ? 'npc1' : 'npc2'); if (event.sequence === 3) assert.equal(arrow.kind, 'attack'); if (event.sequence === 4) assert.equal(arrow, null) },
    onTurn: nextGame => { board = nextGame; trace.push(board.currentPlayerId) },
  })
  assert.equal(board, human)
  assert.deepEqual(trace, ['request', 1, 'eventPause', 2, 'eventPause', 3, 'eventPause', 4, 'eventPause', 'npc2', 'turnPause', 'request', 5, 'eventPause', 'human'])
})
test('finished games and human turns do not request another NPC turn', async () => {
  for (const initial of [game('human'), { ...game('npc1'), phase: 'Finished' }]) {
    await replayNpcTurns({ game: initial, isActive: () => true, request: () => assert.fail('Unexpected request') })
  }
})
test('cancellation during replay stops appending events and never commits the turn', async () => {
  let active = true
  const seen = []
  await replayNpcTurns({ game: game('npc1'), isActive: () => active,
    request: async () => ({ game: game('human'), events: [{ sequence: 1 }, { sequence: 2 }] }),
    onEvent: event => seen.push(event.sequence), pause: async () => { active = false },
    onTurn: () => assert.fail('Cancelled replay committed state'),
  })
  assert.deepEqual(seen, [1])
})
test('cancellation during a request suppresses its response', async () => {
  let active = true
  await replayNpcTurns({ game: game('npc1'), isActive: () => active,
    request: async () => { active = false; return { game: game('human'), events: [{ sequence: 1 }] } },
    onEvent: () => assert.fail('Cancelled event shown'), onTurn: () => assert.fail('Cancelled state committed'),
  })
})
test('a request failure stops playback and reaches the retry handler', async () => {
  const error = new Error('Server rejected NPC turn')
  await assert.rejects(replayNpcTurns({ game: game('npc1'), isActive: () => true, request: async () => { throw error } }), error)
})
test('a server-finished turn replays its last event, commits, and makes no further requests', async () => {
  let calls = 0
  const trace = []
  const finished = { ...game('npc1'), phase: 'Finished' }
  await replayNpcTurns({ game: game('npc1'), isActive: () => true,
    request: async () => { calls++; return { game: finished, events: [{ type: 'GameWon', sequence: 1 }] } },
    onEvent: event => trace.push(event.type), pause: async kind => trace.push(kind),
    onTurn: next => { assert.equal(next, finished); trace.push('commit') },
  })
  assert.equal(calls, 1)
  assert.deepEqual(trace, ['GameWon', 'eventPause', 'commit'])
})
