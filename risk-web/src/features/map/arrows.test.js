import assert from 'node:assert/strict'
import { test } from 'node:test'
import { arrowForEvent, arrowFromGame, curvedPath, curveControl, replayArrow } from './arrows.js'

const attack = { kind: 'attack', from: 'ZA', to: 'US' }
test('arrowForEvent uses AttackStarted endpoints and parses Transfer soldier strings', () => {
  assert.deepEqual(arrowForEvent({ type: 'AttackStarted', data: { fromCountryId: 'ZA', toCountryId: 'US' } }), attack)
  assert.deepEqual(arrowForEvent({ type: 'Transfer', data: { fromCountryId: 'ZA', toCountryId: 'US', soldiers: '3' } }), { ...attack, kind: 'move', soldiers: 3 })
})
test('arrowForEvent ignores events without endpoints and malformed transfers', () => {
  for (const event of [null, {}, { type: 'BattleRound', data: { attackerSoldiers: '5' } }, { type: 'CountryConquered', data: { countryId: 'US' } }, { type: 'ReinforcementsPlaced', data: { countryId: 'ZA', soldiers: '3' } }, { type: 'AttackStarted', data: { fromCountryId: 'ZA' } }]) assert.equal(arrowForEvent(event), null)
  for (const soldiers of [undefined, 'no', '0', '-1', '1.5']) assert.equal(arrowForEvent({ type: 'Transfer', data: { fromCountryId: 'ZA', toCountryId: 'US', soldiers } }), null)
})
test('arrowFromGame uses only the server activeAttack', () => {
  assert.deepEqual(arrowFromGame({ activeAttack: { attackerCountryId: 'ZA', defenderCountryId: 'US' } }), attack)
  assert.equal(arrowFromGame({ activeAttack: null }), null)
  assert.equal(arrowFromGame(null), null)
})
test('curvedPath makes a quadratic curve in horizontal, vertical and reversed directions', () => {
  assert.equal(curvedPath({ x: 0, y: 0 }, { x: 100, y: 0 }), 'M 0 0 Q 50 16 100 0')
  assert.equal(curvedPath({ x: 0, y: 0 }, { x: 0, y: 100 }), 'M 0 0 Q -16 50 0 100')
  assert.deepEqual(curveControl({ x: 100, y: 0 }, { x: 0, y: 0 }), { x: 50, y: -16 })
  assert.equal(curvedPath({ x: 5, y: 5 }, { x: 5, y: 5 }), 'M 5 5 Q 5 5 5 5')
})
test('replay retains attack endpoints through battle and conquest and clears at AttackEnded', () => {
  assert.equal(replayArrow(attack, { type: 'BattleRound' }), attack)
  assert.equal(replayArrow(attack, { type: 'CountryConquered' }), attack)
  assert.equal(replayArrow(attack, { type: 'AttackEnded' }), null)
  const move = replayArrow(attack, { type: 'Transfer', data: { fromCountryId: 'ZA', toCountryId: 'US', soldiers: '2' } })
  assert.equal(move.kind, 'move')
  assert.equal(replayArrow(move, { type: 'TurnStarted' }), null)
})
