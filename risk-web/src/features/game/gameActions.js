export const findHuman = game => game.players.find(player => player.isHuman)
export const isMyTurn = game => game.phase !== 'Finished' && game.currentPlayerId === findHuman(game)?.id
export const isNpcTurn = game => game.phase !== 'Finished' && game.currentPlayerId !== findHuman(game)?.id
export const ownedBy = (game, playerId) => game.territories.filter(territory => territory.ownerPlayerId === playerId)
export const maxMovable = territory => Math.max(0, territory.soldiers - 1)
export function quickAmounts(max) {
  if (max < 1) return []
  const half = Math.max(1, Math.ceil(max / 2))
  const choices = [1, 2, 3].filter(value => value < max && value !== half).map(value => ({ label: String(value), value }))
  if (half !== max) choices.push({ label: 'Half', value: half })
  choices.sort((a, b) => a.value - b.value)
  return [...choices, { label: 'Max', value: max }]
}
export function clampAmount(value, max) {
  const amount = Number(value)
  return Math.max(1, Math.min(Number.isFinite(amount) ? amount : 1, max))
}
export function stepAmount(value, delta, max) {
  return clampAmount(clampAmount(value, max) + delta, max)
}
export function ownerSlot(game, territory) {
  const owner = game.players?.find(player => player.id === territory.ownerPlayerId)
  return (owner && game.territories.find(item => item.countryId === owner.startingCountryId)?.slot) ?? territory.slot
}
export function panelMode(game) {
  if (game.phase === 'Finished') return 'finished'
  if (isNpcTurn(game)) return 'npc'
  return { Reinforcing: 'reinforce', Action: 'action', Attacking: 'attacking', ConquestTransfer: 'conquest' }[game.phase]
}
