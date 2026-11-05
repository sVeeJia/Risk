import assert from 'node:assert/strict'
import { test } from 'node:test'
import { playerSummary } from '../playerSummary.js'

const human = { id: 'human', name: 'Alice', isHuman: true, startingCountryId: 'A', pendingReinforcements: 99 }
const npc = { id: 'npc', name: 'Brazil', isHuman: false, startingCountryId: 'B' }
const game = {
  players: [human, npc],
  territories: [
    { countryId: 'A', name: 'Argentina', slot: 1, ownerPlayerId: 'human', soldiers: 3 },
    { countryId: 'B', name: 'Brazil', slot: 2, ownerPlayerId: 'npc', soldiers: 7 },
    { countryId: 'C', name: 'Canada', slot: 3, ownerPlayerId: 'human', soldiers: 11 },
  ],
}

test('totals sum all current territories and exclude pending reinforcements', () => {
  assert.deepEqual(playerSummary(game, human), { slot: 1, name: 'Alice', soldiers: 14, countries: 2 })
  assert.deepEqual(playerSummary(game, npc), { slot: 2, name: 'NPC · Brazil', soldiers: 7, countries: 1 })
})

test('NPC labels use home country while humans keep their display name', () => {
  assert.equal(playerSummary(game, { ...npc, name: 'Computer' }).name, 'NPC · Brazil')
  assert.equal(playerSummary(game, human).name, 'Alice')
  assert.equal(playerSummary({ ...game, territories: [] }, npc).name, 'NPC · Brazil')
})

test('conquests update totals and country counts without changing player colours or home labels', () => {
  const conquered = { ...game, territories: game.territories.map(t => ({ ...t, ownerPlayerId: 'npc' })) }
  assert.deepEqual(playerSummary(conquered, human), { slot: 1, name: 'Alice', soldiers: 0, countries: 0 })
  assert.deepEqual(playerSummary(conquered, npc), { slot: 2, name: 'NPC · Brazil', soldiers: 21, countries: 3 })
  const lostHome = { ...game, territories: game.territories.map(t => t.countryId === 'B' ? { ...t, ownerPlayerId: 'human' } : t) }
  assert.deepEqual(playerSummary(lostHome, npc), { slot: 2, name: 'NPC · Brazil', soldiers: 0, countries: 0 })
  assert.deepEqual(playerSummary(lostHome, human), { slot: 1, name: 'Alice', soldiers: 21, countries: 3 })
})

test('eliminated players always show zero soldiers and countries, preserving their identity', () => {
  assert.deepEqual(playerSummary(game, { ...human, isEliminated: true }), {
    slot: 1, name: 'Alice', soldiers: 0, countries: 0,
  })
  assert.deepEqual(playerSummary(game, { ...npc, isEliminated: true }), {
    slot: 2, name: 'NPC · Brazil', soldiers: 0, countries: 0,
  })
})
