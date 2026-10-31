import assert from 'node:assert/strict'
import { test } from 'node:test'
import { parseDice, pairDice, battleFromEvent, liveCountsFromEvent, markersFromAttack } from './battle.js'
import { battlePresentation, emptyBattle } from './battlePresentation.js'
import { replayNpcTurns } from './npcPlayback.js'

const attack = { attackerCountryId: 'PE', defenderCountryId: 'DM' }
const started = { type: 'AttackStarted', sequence: 1, data: { fromCountryId: 'PE', toCountryId: 'DM', attackerSoldiers: '7', defenderSoldiers: '3' } }
const round = { type: 'BattleRound', sequence: 2, data: { attackerDice: '5,2,3', defenderDice: '1,6,3', attackerLosses: '1', defenderLosses: '1', attackerSoldiers: '6', defenderSoldiers: '2' } }
test('parseDice accepts comma strings, whitespace and arrays without sorting', () => {
  for (const input of ['5,3,1', '5, 3, 1', [5, 3, 1], ['5', '3', '1']]) assert.deepEqual(parseDice(input), [5, 3, 1])
})
test('parseDice handles empty, invalid, fractional and out-of-range input', () => {
  for (const value of ['', ' ', undefined, null, {}, 5]) assert.deepEqual(parseDice(value), [])
  assert.deepEqual(parseDice('0,7,1.5,no,6,2'), [6, 2])
  assert.deepEqual(parseDice([true, null, {}, [], 4, Infinity]), [4])
})
test('pairDice compares matching indices in both directions and ties; stops at shorter length', () => {
  assert.deepEqual(pairDice([5, 2, 3], [1, 6, 3]), [
    { attacker: 5, defender: 1, outcome: 'attacker' }, { attacker: 2, defender: 6, outcome: 'defender' }, { attacker: 3, defender: 3, outcome: 'tie' },
  ])
  assert.equal(pairDice([6, 2], [3]).length, 1)
  assert.equal(pairDice([6], [3, 2]).length, 1)
  assert.deepEqual(pairDice([], []), [])
})
test('battleFromEvent reads authoritative losses and AFTER-round counts', () => {
  const result = battleFromEvent(round)
  assert.equal(result.attackerLosses, 1)
  assert.equal(result.defenderLosses, 1)
  assert.equal(result.attackerSoldiers, 6)
  assert.equal(result.defenderSoldiers, 2)
  assert.equal(result.pairs[0].attacker, 5)
  const authoritative = battleFromEvent({ ...round, data: { ...round.data, attackerDice: '6', defenderDice: '1', attackerLosses: '2', defenderLosses: '0' } })
  assert.equal(authoritative.pairs[0].outcome, 'attacker')
  assert.equal(authoritative.attackerLosses, 2) // Visual comparisons must not supply the losses.
})
test('battleFromEvent rejects missing or malformed data instead of inventing results', () => {
  for (const event of [null, started, { ...round, data: null }, { ...round, data: {} }]) assert.equal(battleFromEvent(event), null)
  for (const [key, value] of [['attackerDice', '6,no'], ['defenderDice', ''], ['attackerLosses', '-1'], ['defenderLosses', null], ['attackerSoldiers', 'x'], ['defenderSoldiers', '1.5']]) {
    assert.equal(battleFromEvent({ ...round, data: { ...round.data, [key]: value } }), null)
  }
})
test('live count mapping uses pre-battle AttackStarted and post-round BattleRound values directly', () => {
  assert.deepEqual(liveCountsFromEvent(started), { PE: 7, DM: 3 })
  assert.deepEqual(liveCountsFromEvent(round, attack), { PE: 6, DM: 2 })
  assert.deepEqual(liveCountsFromEvent({ ...round, data: { ...round.data, defenderSoldiers: '0' } }, attack), { PE: 6, DM: 0 })
  assert.equal(liveCountsFromEvent(round), null)
  assert.equal(liveCountsFromEvent({ ...round, data: {} }, attack), null)
  assert.equal(liveCountsFromEvent({ type: 'Transfer', data: { soldiers: '3' } }, attack), null)
  assert.equal(liveCountsFromEvent({ type: 'CountryConquered', data: { countryId: 'DM', reward: '2' } }, attack), null)
})
test('markersFromAttack validates DTO and event endpoints and clears absent attacks', () => {
  assert.deepEqual(markersFromAttack(attack), { attackerId: 'PE', defenderId: 'DM' })
  assert.deepEqual(markersFromAttack(started.data), { attackerId: 'PE', defenderId: 'DM' })
  for (const value of [null, {}, { attackerCountryId: 'PE' }, { from: ' ', to: 'DM' }]) assert.equal(markersFromAttack(value), null)
})
test('presentation counts rounds only; retains server before/after numbers and clears replay overlays', () => {
  let state = battlePresentation(emptyBattle, { type: 'event', event: started, live: true })
  state = battlePresentation(state, { type: 'event', event: round, live: true, fastForward: true })
  assert.deepEqual(state.board.before, { attackerSoldiers: 7, defenderSoldiers: 3 })
  assert.equal(state.board.round, 1)
  assert.deepEqual(state.liveCounts, { PE: 6, DM: 2 })
  state = battlePresentation(state, { type: 'event', event: { type: 'CountryConquered', sequence: 3, data: { countryId: 'DM', reward: '2' } }, live: true })
  assert.equal(state.conquered.countryId, 'DM')
  assert.deepEqual(state.liveCounts, { PE: 6, DM: 2 }) // No reward arithmetic.
  state = battlePresentation(state, { type: 'event', event: { type: 'AttackEnded', data: { reason: 'conquest' } }, fastForward: true })
  assert.equal(state.attack, null)
  assert.equal(state.board.ended, true)
  state = battlePresentation(state, { type: 'turnEnd' })
  assert.deepEqual(state.liveCounts, {})
  assert.equal(state.hit, null)
  assert.equal(state.conquered, null)
  assert.equal(state.board.round, 1)
})
test('human rolls do not create live count overrides and a new attack resets the board', () => {
  let state = battlePresentation(emptyBattle, { type: 'event', event: started })
  state = battlePresentation(state, { type: 'event', event: round })
  state = battlePresentation(state, { type: 'event', event: { ...round, sequence: 3 } })
  assert.equal(state.board.round, 2)
  assert.deepEqual(state.liveCounts, {})
  state = battlePresentation(state, { type: 'event', event: { ...started, sequence: 10 } })
  assert.equal(state.board.round, 0)
  assert.equal(state.board.result, null)
})
test('the dock keeps a completed roll until a new attack or turn starts', () => {
  let state = battlePresentation(emptyBattle, { type: 'event', event: started })
  assert.equal(state.board.result, null)
  state = battlePresentation(state, { type: 'event', event: round })
  state = battlePresentation(state, { type: 'event', event: { type: 'AttackEnded', data: { reason: 'exhausted' } } })
  assert.equal(state.board.visible, true)
  assert.equal(state.board.result.attackerSoldiers, 6)
  assert.equal(battlePresentation(state, { type: 'event', event: { type: 'TurnStarted' } }).board.visible, false)
  assert.equal(battlePresentation(state, { type: 'turnEnd' }).board.visible, false)
  assert.equal(battlePresentation(state, { type: 'event', event: { ...started, sequence: 10 } }).board.result, null)
})
test('reload uses active attack only and never restores stale markers or count overrides', () => {
  const game = { id: 'game', activeAttack: attack, territories: [{ countryId: 'PE', soldiers: 7 }, { countryId: 'DM', soldiers: 3 }] }
  let state = battlePresentation(emptyBattle, { type: 'load', game })
  assert.deepEqual(state.attack, attack)
  assert.deepEqual(state.board.counts, { attackerSoldiers: 7, defenderSoldiers: 3 })
  state = battlePresentation(state, { type: 'load', game: { ...game, activeAttack: null } })
  assert.deepEqual(state, emptyBattle)
})
test('NPC callbacks show each battle count before committing the turn and clear overlays together', async () => {
  const players = [{ id: 'human', isHuman: true }, { id: 'npc' }]
  const game = { currentPlayerId: 'npc', phase: 'Action', players }
  let state = emptyBattle
  const shown = []
  let committed = false
  const end = { type: 'AttackEnded', sequence: 4, data: { reason: 'exhausted' } }
  const nextRound = { ...round, sequence: 3, data: { ...round.data, attackerSoldiers: '4', defenderSoldiers: '1' } }
  await replayNpcTurns({ game, isActive: () => true,
    request: async () => ({ game: { ...game, currentPlayerId: 'human' }, events: [started, round, nextRound, end] }),
    pause: async () => {},
    onEvent: event => {
      assert.equal(committed, false)
      state = battlePresentation(state, { type: 'event', event, live: true, fastForward: true })
      shown.push({ ...state.liveCounts })
    },
    onTurn: () => { committed = true; state = battlePresentation(state, { type: 'turnEnd' }) },
  })
  assert.deepEqual(shown, [{ PE: 7, DM: 3 }, { PE: 6, DM: 2 }, { PE: 4, DM: 1 }, { PE: 4, DM: 1 }])
  assert.equal(state.board.round, 2)
  assert.equal(state.board.fastForward, true)
  assert.equal(state.attack, null)
  assert.deepEqual(state.liveCounts, {})
  assert.equal(state.hit, null)
})
