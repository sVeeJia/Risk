export function arrowForEvent(event) {
  const data = event?.data
  if (!data?.fromCountryId || !data?.toCountryId) return null
  const base = { from: data.fromCountryId, to: data.toCountryId }
  if (event.type === 'AttackStarted') return { kind: 'attack', ...base }
  if (event.type === 'Transfer' && Number.isInteger(Number(data.soldiers)) && Number(data.soldiers) > 0) {
    return { kind: 'move', ...base, soldiers: Number(data.soldiers) }
  }
  return null
}

export const arrowFromGame = game => game?.activeAttack ? {
  kind: 'attack', from: game.activeAttack.attackerCountryId, to: game.activeAttack.defenderCountryId,
} : null

export function curveControl(from, to) {
  return { x: (from.x + to.x) / 2 - (to.y - from.y) * .16, y: (from.y + to.y) / 2 + (to.x - from.x) * .16 }
}
export function curvedPath(from, to) {
  const control = curveControl(from, to)
  return `M ${from.x} ${from.y} Q ${control.x} ${control.y} ${to.x} ${to.y}`
}

// BattleRound has no endpoints; retain the preceding AttackStarted arrow.
export function replayArrow(previous, event) {
  if (event.type === 'AttackEnded') return null
  return arrowForEvent(event) ?? (previous?.kind === 'attack' ? previous : null)
}
