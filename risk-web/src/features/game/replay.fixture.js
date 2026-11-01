const event = (sequence, type, playerId, data = {}) => ({ sequence, type, playerId, data, message: `${type} ${sequence}`, justification: null, timestamp: '2026-11-01T12:00:00Z' })
export const completeEvents = [
  event(1, 'GameStarted', 'human'), event(2, 'TurnStarted', 'human', { round: '1' }),
  event(3, 'ReinforcementsReceived', 'human', { soldiers: '3' }),
  event(4, 'ReinforcementsPlaced', 'human', { countryId: 'A', soldiers: '3', remaining: '0' }),
  event(5, 'AttackStarted', 'human', { fromCountryId: 'A', toCountryId: 'B' }),
  event(6, 'BattleRound', 'human', { attackerDice: '6,2,4', defenderDice: '5,3,1', attackerLosses: '1', defenderLosses: '2', attackerSoldiers: '7', defenderSoldiers: '2' }),
  event(7, 'BattleRound', 'human', { attackerDice: '5,2', defenderDice: '4,3', attackerLosses: '1', defenderLosses: '1', attackerSoldiers: '6', defenderSoldiers: '1' }),
  event(8, 'BattleRound', 'human', { attackerDice: '6,4', defenderDice: '2', attackerLosses: '0', defenderLosses: '1', attackerSoldiers: '6', defenderSoldiers: '0' }),
  event(9, 'CountryConquered', 'human', { countryId: 'B', reward: '2', previousOwnerId: 'npc' }),
  event(10, 'Transfer', 'human', { fromCountryId: 'A', toCountryId: 'B', soldiers: '3' }),
  event(11, 'PlayerEliminated', 'npc'), event(12, 'AttackEnded', 'human', { reason: 'conquest' }), event(13, 'GameWon', 'human'),
]

export const completeGame = {
  id: 'complete', version: 7, createdAt: '2026-11-01T12:00:00Z', round: 1, currentPlayerId: 'human', phase: 'Finished', winnerPlayerId: 'human', activeAttack: null, attackedThisTurn: true,
  territories: [
    { countryId: 'A', name: 'Peru', slot: 1, baseSoldiers: 5, soldiers: 5, ownerPlayerId: 'human' },
    { countryId: 'B', name: 'Brunei', slot: 2, baseSoldiers: 4, soldiers: 3, ownerPlayerId: 'human' },
  ],
  players: [
    { id: 'human', name: 'Alice', isHuman: true, startingCountryId: 'A', countriesOwned: 2, pendingReinforcements: 0, isEliminated: false },
    { id: 'npc', name: 'Bob', isHuman: false, startingCountryId: 'B', countriesOwned: 0, pendingReinforcements: 0, isEliminated: true },
  ],
}
