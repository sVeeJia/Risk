import assert from 'node:assert/strict'
import { test } from 'node:test'
import { findHuman, isMyTurn, isNpcTurn, ownedBy, maxMovable, panelMode, ownerSlot, quickAmounts, clampAmount, stepAmount } from './gameActions.js'

const human = { id: 'human', isHuman: true }
const npc = { id: 'npc', isHuman: false }
const territory = { ownerPlayerId: 'human', soldiers: 4 }
const game = { players: [npc, human], territories: [territory, { ownerPlayerId: 'npc' }], currentPlayerId: 'human', phase: 'Action' }
test('stepAmount increments and decrements the current amount within range', () => {
  assert.equal(stepAmount(4, 1, 10), 5)
  assert.equal(stepAmount(4, -1, 10), 3)
})
test('stepAmount stays within both limits and returns 1 for max 0', () => {
  assert.equal(stepAmount(1, -1, 10), 1)
  assert.equal(stepAmount(10, 1, 10), 10)
  assert.equal(stepAmount(4, 1, 0), 1)
})
test('stepAmount treats empty and non-numeric amounts as 1 before stepping', () => {
  for (const value of ['', 'invalid', undefined, NaN]) {
    assert.equal(stepAmount(value, 1, 10), 2)
    assert.equal(stepAmount(value, -1, 10), 1)
  }
  assert.equal(stepAmount('4', 1, 10), 5)
})
test('stepping after a quick pick uses its exact value and remains clamped', () => {
  const half = quickAmounts(8).find(choice => choice.label === 'Half').value
  assert.equal(half, 4)
  assert.equal(stepAmount(half, 1, 8), 5)
  assert.equal(stepAmount(half, -1, 8), 3)
  let value = half
  for (let i = 0; i < 20; i++) value = stepAmount(value, 1, 8)
  assert.equal(value, 8)
  for (let i = 0; i < 20; i++) value = stepAmount(value, -1, 8)
  assert.equal(value, 1)
})
for (const [max, expected] of [
  [0, []],
  [1, [{ label: 'Max', value: 1 }]],
  [2, [{ label: 'Half', value: 1 }, { label: 'Max', value: 2 }]],
  [3, [{ label: '1', value: 1 }, { label: 'Half', value: 2 }, { label: 'Max', value: 3 }]],
  [4, [{ label: '1', value: 1 }, { label: 'Half', value: 2 }, { label: '3', value: 3 }, { label: 'Max', value: 4 }]],
  [10, [{ label: '1', value: 1 }, { label: '2', value: 2 }, { label: '3', value: 3 }, { label: 'Half', value: 5 }, { label: 'Max', value: 10 }]],
  [9, [{ label: '1', value: 1 }, { label: '2', value: 2 }, { label: '3', value: 3 }, { label: 'Half', value: 5 }, { label: 'Max', value: 9 }]],
]) {
  test(`quickAmounts(${max}) returns distinct choices including Max when available`, () => assert.deepEqual(quickAmounts(max), expected))
}
test('clampAmount bounds amounts below 1, above max and within range', () => {
  assert.equal(clampAmount(-3, 10), 1)
  assert.equal(clampAmount(0, 10), 1)
  assert.equal(clampAmount(15, 10), 10)
  assert.equal(clampAmount(4, 10), 4)
  assert.equal(clampAmount(1, 10), 1)
  assert.equal(clampAmount(10, 10), 10)
})
test('clampAmount handles typed strings, cleared inputs and an unavailable maximum', () => {
  assert.equal(clampAmount('7', 5), 5)
  assert.equal(clampAmount('', 5), 1)
  assert.equal(clampAmount(NaN, 5), 1)
  assert.equal(clampAmount(4, 0), 1)
})
const colourGame = {
  players: [{ id: 'npc', startingCountryId: 'US' }, { id: 'human', startingCountryId: 'CN', isHuman: true }],
  territories: [{ countryId: 'CN', slot: 1, ownerPlayerId: 'human' }, { countryId: 'US', slot: 4, ownerPlayerId: 'npc' }, { countryId: 'ZA', slot: 8, ownerPlayerId: 'other' }],
}
test('ownerSlot keeps an unconquered country in its original colour slot', () => {
  for (const territory of colourGame.territories.slice(0, 2)) assert.equal(ownerSlot(colourGame, territory), territory.slot)
})
test('ownerSlot gives a human conquest slot 1', () => {
  assert.equal(ownerSlot(colourGame, { ...colourGame.territories[2], ownerPlayerId: 'human' }), 1)
})
test('ownerSlot gives an NPC conquest that NPC starting-country slot', () => {
  assert.equal(ownerSlot(colourGame, { ...colourGame.territories[2], ownerPlayerId: 'npc' }), 4)
})
test('ownerSlot falls back when the owner or starting country is missing', () => {
  assert.equal(ownerSlot(colourGame, colourGame.territories[2]), 8)
  assert.equal(ownerSlot(colourGame, { slot: 5, ownerPlayerId: null }), 5)
  assert.equal(ownerSlot({ ...colourGame, territories: [] }, { slot: 5, ownerPlayerId: 'npc' }), 5)
})
test('ownerSlot preserves player colours after their starting country is conquered', () => {
  const changed = { ...colourGame, territories: colourGame.territories.map(t => ({ ...t, ownerPlayerId: 'npc' })) }
  assert.equal(ownerSlot(changed, { ...changed.territories[2], ownerPlayerId: 'human' }), 1)
  assert.equal(ownerSlot(changed, changed.territories[0]), 4)
})
test('findHuman identifies the human regardless of player order', () => {
  assert.equal(findHuman(game), human)
  assert.equal(findHuman({ ...game, players: [human, npc] }), human)
})
test('findHuman returns undefined when no human is present', () => {
  assert.equal(findHuman({ ...game, players: [npc] }), undefined)
  assert.equal(findHuman({ ...game, players: [] }), undefined)
})
test('isMyTurn follows the server current player in every active phase', () => {
  for (const phase of ['Reinforcing', 'Action', 'Attacking', 'ConquestTransfer']) {
    assert.equal(isMyTurn({ ...game, phase }), true)
    assert.equal(isMyTurn({ ...game, phase, currentPlayerId: 'npc' }), false)
  }
})
test('isMyTurn is false when the game is finished', () => {
  for (const currentPlayerId of ['human', 'npc', null]) {
    assert.equal(isMyTurn({ ...game, currentPlayerId, phase: 'Finished' }), false)
  }
})
test('isNpcTurn follows the server current player in every active phase', () => {
  for (const phase of ['Reinforcing', 'Action', 'Attacking', 'ConquestTransfer']) {
    assert.equal(isNpcTurn({ ...game, phase }), false)
    assert.equal(isNpcTurn({ ...game, phase, currentPlayerId: 'npc' }), true)
  }
})
test('isNpcTurn is false when the game is finished', () => {
  for (const currentPlayerId of ['human', 'npc', null]) {
    assert.equal(isNpcTurn({ ...game, currentPlayerId, phase: 'Finished' }), false)
  }
})
test('ownedBy filters by owner without changing territories', () => {
  const original = structuredClone(game.territories)
  assert.deepEqual(ownedBy(game, 'human'), [territory])
  assert.deepEqual(ownedBy(game, 'npc'), [game.territories[1]])
  assert.deepEqual(ownedBy(game, 'missing'), [])
  assert.deepEqual(ownedBy({ ...game, territories: [] }, 'human'), [])
  assert.deepEqual(game.territories, original)
})
test('maxMovable leaves one soldier and never returns a negative value', () => {
  for (const [soldiers, expected] of [[4, 3], [1, 0], [0, 0]]) assert.equal(maxMovable({ soldiers }), expected)
})
test('panelMode covers all phases and NPC turns', () => {
  for (const [phase, mode] of Object.entries({ Reinforcing: 'reinforce', Action: 'action', Attacking: 'attacking', ConquestTransfer: 'conquest', Finished: 'finished' })) {
    assert.equal(panelMode({ ...game, phase }), mode)
    assert.equal(panelMode({ ...game, phase, currentPlayerId: 'npc' }), phase === 'Finished' ? 'finished' : 'npc')
  }
})
