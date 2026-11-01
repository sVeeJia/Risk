import test from 'node:test'
import assert from 'node:assert/strict'
import { completeGame, completeEvents } from './replay.fixture.js'
import { initialReplayState, applyReplayEvent, replayDifferences } from './replayState.js'
import { buildTimeline, stateAt, totalTime, replayMarkers } from './replayTimeline.js'
import { seekTime, seekFraction, skipTime, pacedDuration, replaySpeeds, heavyAnimation, stackFlags, timeLabel } from './replayNavigation.js'

const initial = () => initialReplayState(completeGame, completeEvents)
const timeline = () => buildTimeline(initial(), completeEvents)
const country = (state, id) => state.territories.find(item => item.countryId === id)

test('initial state restores base soldiers, starting owners, first turn and non-eliminated players', () => {
  const state = initial()
  assert.equal(country(state, 'B').ownerPlayerId, 'npc')
  assert.equal(country(state, 'B').soldiers, 4)
  assert.equal(state.currentPlayerId, 'human')
  assert.equal(state.round, 1)
  assert.equal(state.phase, 'Action')
  assert.ok(state.players.every(player => !player.isEliminated && player.pendingReinforcements === 0 && player.countriesOwned === 1))
})

test('missing initial DTO facts are reported instead of inventing a starting board', () => {
  const missing = structuredClone(completeGame)
  delete missing.territories[0].baseSoldiers
  assert.throws(() => initialReplayState(missing, completeEvents), /Missing starting state/)
})

test('full fixture reconstructs the exact final game including reward, transfer, elimination and win', () => {
  const steps = timeline()
  assert.deepEqual(stateAt(steps, steps.length - 1), completeGame)
  assert.deepEqual(replayDifferences(steps.at(-1).stateAfter, completeGame), [])
  assert.ok(steps.every(step => step.issues.length === 0))
  assert.equal(country(steps[8].stateAfter, 'A').soldiers, 8) // Post-battle six plus the recorded reward of two.
  assert.equal(country(steps[8].stateAfter, 'B').soldiers, 0)
  assert.equal(country(steps[9].stateAfter, 'A').soldiers, 5)
  assert.equal(country(steps[9].stateAfter, 'B').soldiers, 3)
})

test('first and middle steps expose server facts, and every earlier state remains immutable', () => {
  const base = initial(), original = structuredClone(base)
  const freeze = object => { Object.freeze(object); for (const value of Object.values(object)) if (value && typeof value === 'object') freeze(value) }
  freeze(base)
  const steps = buildTimeline(base, [...completeEvents].reverse())
  assert.deepEqual(base, original)
  assert.equal(steps[0].event.sequence, 1)
  assert.equal(country(stateAt(steps, 0), 'A').soldiers, 5)
  assert.equal(country(stateAt(steps, 3), 'A').soldiers, 8)
  assert.equal(country(stateAt(steps, 5), 'A').soldiers, 7)
  assert.equal(country(stateAt(steps, 5), 'B').soldiers, 2)
  assert.equal(stateAt(steps, 5).activeAttack.attackerCountryId, 'A')
  assert.equal(stateAt(steps, 4).activeAttack.attackerCountryId, 'A')
  assert.equal(steps[5].visual.battle.board.before.attackerSoldiers, 8)
})

test('turn, round, reinforcement reserves and a loss follow the events', () => {
  let state = initial()
  for (const event of [
    { type: 'TurnStarted', playerId: 'npc', data: { round: '2' } },
    { type: 'ReinforcementsReceived', playerId: 'npc', data: { soldiers: '4' } },
    { type: 'ReinforcementsPlaced', playerId: 'npc', data: { countryId: 'B', soldiers: '2', remaining: '2' } },
  ]) state = applyReplayEvent(state, event).state
  assert.equal(state.round, 2)
  assert.equal(state.currentPlayerId, 'npc')
  assert.equal(country(state, 'B').soldiers, 6)
  assert.equal(state.players[1].pendingReinforcements, 2)
  assert.equal(state.phase, 'Reinforcing')
  state = applyReplayEvent(state, { type: 'GameLost', playerId: 'human' }).state
  assert.equal(state.phase, 'Finished')
  assert.equal(state.winnerPlayerId, null)
})

test('incomplete events skip all partial changes and mark the timeline incomplete', () => {
  const base = initial()
  for (const event of [
    { type: 'ReinforcementsPlaced', playerId: 'human', data: { countryId: 'A', soldiers: '3' } },
    { type: 'BattleRound', data: { attackerSoldiers: '3', defenderSoldiers: '2' } },
    { type: 'Transfer', data: { fromCountryId: 'A', toCountryId: 'B' } },
    { type: 'Transfer', data: { fromCountryId: 'A', toCountryId: 'B', soldiers: '999' } },
    { type: 'TurnStarted', playerId: 'unknown', data: { round: '2' } },
    { type: 'PlayerEliminated' },
  ]) {
    const result = applyReplayEvent(base, event)
    assert.equal(result.state, base)
    assert.ok(result.issues.length)
  }
  const steps = buildTimeline(base, [{ sequence: 1, type: 'Transfer', data: {} }])
  assert.equal(steps[0].issues[0].sequence, 1)
  assert.deepEqual(steps[0].stateAfter, base)
})

test('board comparison reports soldier, owner and eliminated-player differences', () => {
  const state = structuredClone(completeGame)
  state.territories[0].soldiers--
  state.territories[1].ownerPlayerId = 'npc'
  state.players[1].isEliminated = false
  assert.deepEqual(replayDifferences(state, completeGame).map(item => item.field), ['soldiers', 'ownerPlayerId', 'isEliminated'])
})

test('a missing game-over event is noticed even if the final armies already match', () => {
  const steps = buildTimeline(initial(), completeEvents.slice(0, -1))
  assert.ok(replayDifferences(steps.at(-1).stateAfter, completeGame).some(item => item.field === 'phase'))
})

test('a missing battle or conquest field never partially changes the board', () => {
  const steps = timeline()
  for (const field of ['attackerDice', 'defenderDice', 'attackerLosses', 'defenderLosses', 'attackerSoldiers', 'defenderSoldiers']) {
    const event = structuredClone(completeEvents[5])
    delete event.data[field]
    const result = applyReplayEvent(steps[4].stateAfter, event)
    assert.equal(result.state, steps[4].stateAfter)
    assert.ok(result.issues.length)
  }
  for (const field of ['reward', 'previousOwnerId', 'countryId']) {
    const event = structuredClone(completeEvents[8])
    delete event.data[field]
    const result = applyReplayEvent(steps[7].stateAfter, event)
    assert.equal(result.state, steps[7].stateAfter)
    assert.ok(result.issues.length)
  }
})

test('informational events preserve board state and NPC justification presentation', () => {
  const base = initial()
  for (const type of ['GameStarted', 'TurnSkipped', 'NpcDecision', 'Commentary']) assert.deepEqual(applyReplayEvent(base, { type }).state, base)
  const event = { sequence: 1, type: 'NpcDecision', playerId: 'npc', justification: 'An exposed border.' }
  assert.equal(buildTimeline(base, [event])[0].visual.decision.event.justification, 'An exposed border.')
})

test('timeline boundaries, durations and empty state are deterministic', () => {
  const steps = timeline()
  assert.equal(steps[0].startMs, 0)
  for (let i = 1; i < steps.length; i++) assert.equal(steps[i].startMs, steps[i - 1].startMs + steps[i - 1].durationMs)
  assert.equal(steps[5].durationMs, 1400)
  assert.equal(totalTime(steps), steps.reduce((sum, step) => sum + step.durationMs, 0))
  assert.equal(totalTime([]), 0)
  assert.deepEqual(buildTimeline(initial(), []), [])
  const base = initial()
  assert.equal(stateAt([], -1, base), base)
})

test('conquests and eliminations are detected, but flags exclude NPC conquests', () => {
  const human = timeline()
  assert.equal(human[8].marker.kind, 'conquest')
  assert.equal(human[10].marker.kind, 'elimination')
  assert.deepEqual(replayMarkers(human).map(step => step.marker.kind), ['conquest', 'gameOver'])
  assert.match(replayMarkers(human)[0].marker.title, /Round 1 · You conquered Brunei/)
  const npcEvents = [
    { sequence: 1, type: 'AttackStarted', playerId: 'npc', data: { fromCountryId: 'B', toCountryId: 'A' } },
    { sequence: 2, type: 'CountryConquered', playerId: 'npc', data: { countryId: 'A', reward: '0', previousOwnerId: 'human' } },
    { sequence: 3, type: 'GameWon', playerId: 'npc' },
  ]
  const npc = buildTimeline(initial(), npcEvents)
  assert.equal(npc[1].marker.human, false)
  assert.equal(replayMarkers(npc).length, 1)
  assert.equal(replayMarkers(npc)[0].marker.won, false)
  assert.equal(buildTimeline(initial(), [{ sequence: 1, type: 'GameLost', playerId: 'human' }])[0].marker.won, false)
})

test('seek by time and fraction lands on the preceding boundary and clamps', () => {
  const steps = timeline()
  assert.equal(seekTime(steps, -1), 0)
  assert.equal(seekTime(steps, steps[5].startMs + 5), 5)
  assert.equal(seekTime(steps, steps[5].startMs - 1), 4)
  assert.equal(seekFraction(steps, 0), 0)
  assert.equal(seekFraction(steps, 1), steps.length - 1)
  assert.equal(seekFraction(steps, 99), steps.length - 1)
  assert.equal(seekFraction(steps, .5), seekTime(steps, totalTime(steps) / 2))
  assert.equal(seekTime([], 400), -1)
})

test('older histories with more than one terminal event still have one end marker', () => {
  const steps = buildTimeline(initial(), [{ sequence: 1, type: 'GameLost', playerId: 'human' }, { sequence: 2, type: 'GameWon', playerId: 'npc' }])
  assert.deepEqual(replayMarkers(steps).map(step => step.index), [1])
})

test('five-second skips use timeline time, cross at least one event and clamp at ends', () => {
  const steps = timeline()
  assert.equal(skipTime(steps, 0, 5000), seekTime(steps, 5000))
  assert.equal(skipTime(steps, 8, -5000), seekTime(steps, steps[8].startMs - 5000))
  assert.equal(skipTime(steps, 5, 1), 6)
  assert.equal(skipTime(steps, 5, -1), 4)
  assert.equal(skipTime(steps, 0, -5000), 0)
  assert.equal(skipTime(steps, steps.length - 1, 5000), steps.length - 1)
  assert.equal(skipTime([], -1, 5000), -1)
})

test('all speeds scale pacing exactly and x8/x16 disable heavy animation', () => {
  assert.deepEqual(replaySpeeds.map(speed => pacedDuration(1600, speed)), [1600, 800, 400, 200, 100])
  assert.deepEqual(replaySpeeds.map(heavyAnimation), [true, true, true, false, false])
  assert.equal(timeLabel(72000), '01:12')
  assert.equal(timeLabel(270000), '04:30')
})

test('dense flags remain distinct, inside the rail, and retain exact timeline anchors', () => {
  const markers = Array.from({ length: 30 }, (_, index) => ({ index, startMs: 500, marker: { kind: 'conquest' } }))
  const flags = stackFlags(markers, 240, 1000)
  assert.equal(flags.length, markers.length)
  assert.equal(new Set(flags.map(flag => `${flag.x}:${flag.lane}`)).size, 30)
  assert.ok(flags.every(flag => flag.anchor === 120 && flag.x > 0 && flag.x < 240 && flag.lane < 3))
  for (let i = 0; i < flags.length; i++) for (let j = i + 1; j < flags.length; j++) if (flags[i].lane === flags[j].lane) {
    assert.ok(Math.abs(flags[i].x - flags[j].x) >= (flags[i].size + flags[j].size) / 2)
  }
  assert.deepEqual(stackFlags([], 240, 1000), [])
})
