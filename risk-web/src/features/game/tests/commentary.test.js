import test from 'node:test'
import assert from 'node:assert/strict'
import { commentaryFor, logRows, uniqueEvents, isAiCommentary } from '../commentary.js'
import { buildTimeline, totalTime, replayMarkers } from '../replayTimeline.js'
import { initialReplayState } from '../replayState.js'
import { completeGame, completeEvents } from './replay.fixture.js'
import { createSpectatorQueue } from '../spectatorQueue.js'

const roll = { id: 'roll', sequence: 6, type: 'BattleRound', message: 'Dice 6 vs 3. Defender loses 1.' }
const caption = { id: 'caption', sequence: 9, type: 'Commentary', message: 'The attack breaks through.', data: { forSequence: '6', source: 'llm' } }

test('links by forSequence across intervening events and reversed arrival order', () => {
  const conquest = { sequence: 7, type: 'CountryConquered' }
  assert.equal(commentaryFor([caption, conquest, roll], 6), caption)
  assert.deepEqual(logRows([caption, conquest]), [{ event: conquest, commentary: null }, { event: caption, commentary: null }])
  assert.deepEqual(logRows([caption, conquest, roll]), [{ event: roll, commentary: caption }, { event: conquest, commentary: null }])
})

test('missing and malformed commentary never hide a roll', () => {
  for (const event of [null, {}, { ...caption, data: null }, { ...caption, data: { forSequence: {} } }, { ...caption, message: null }]) {
    assert.equal(commentaryFor([roll, event], 6), null)
    assert.equal(logRows([roll, event])[0].event, roll)
  }
  assert.deepEqual(logRows([roll]), [{ event: roll, commentary: null }])
})

test('dedupes by id or sequence and filters only narrated rolls in roll order', () => {
  const other = { ...roll, id: 'other', sequence: 10 }
  const events = [caption, roll, { ...caption }, { ...roll, sequence: 20 }, { ...roll, id: 'duplicate' }, other]
  assert.equal(uniqueEvents(events).length, 3)
  assert.deepEqual(logRows(events, 'commentary'), [{ event: roll, commentary: caption }])
  assert.equal(logRows(events).length, 2)
})

test('AI chip is enabled only for llm', () => {
  assert.equal(isAiCommentary(caption), true)
  for (const source of ['template', 'unknown', null, undefined]) assert.equal(isAiCommentary({ data: { source } }), false)
  assert.equal(isAiCommentary({}), false)
})

test('spectator queue accepts a late roll and ignores repeated history and payloads', async () => {
  const shown = []
  const game = { version: 1 }
  const queue = createSpectatorQueue({ game, lastSeenSequence: 5, initialEvents: [{ sequence: 5 }],
    signal: new AbortController().signal, pause: async () => {}, onEvent: event => shown.push(event),
    onGame: () => {}, onBusy: () => {}, onError: error => { throw error } })
  await queue.enqueue({ game, events: [caption, { sequence: 5 }] })
  await queue.enqueue({ game, events: [roll, caption, roll] })
  assert.deepEqual(shown, [caption, roll])
  assert.equal(queue.lastSeenSequence, 9)
  assert.deepEqual(logRows(shown), [{ event: roll, commentary: caption }])
})

test('commentary creates no replay steps, duration or conquest marker changes', () => {
  const initial = initialReplayState(completeGame, completeEvents)
  const plain = buildTimeline(initial, completeEvents)
  const narrated = buildTimeline(initial, [...completeEvents, { ...caption, sequence: 100 }])
  assert.equal(narrated.length, plain.length)
  assert.equal(totalTime(narrated), totalTime(plain))
  assert.deepEqual(replayMarkers(narrated), replayMarkers(plain))
  assert.deepEqual(narrated, plain)
})
