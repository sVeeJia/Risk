const diceValues = value => Array.isArray(value) ? value : typeof value === 'string' && value.trim() ? value.split(',') : []
const numeric = value => (typeof value === 'number' || typeof value === 'string' && value.trim() !== '') ? Number(value) : NaN
export const parseCount = value => Number.isInteger(numeric(value)) && numeric(value) >= 0 ? numeric(value) : null

export function parseDice(value) {
  return diceValues(value).map(numeric).filter(die => Number.isInteger(die) && die >= 1 && die <= 6)
}

export function pairDice(attackerDice, defenderDice) {
  if (!Array.isArray(attackerDice) || !Array.isArray(defenderDice)) return []
  return attackerDice.slice(0, defenderDice.length).map((attacker, index) => {
    const defender = defenderDice[index]
    return { attacker, defender, outcome: attacker > defender ? 'attacker' : attacker < defender ? 'defender' : 'tie' }
  })
}

export function battleFromEvent(event) {
  if (event?.type !== 'BattleRound' || !event.data) return null
  const attacker = parseDice(event.data.attackerDice)
  const defender = parseDice(event.data.defenderDice)
  if (!attacker.length || !defender.length || attacker.length !== diceValues(event.data.attackerDice).length || defender.length !== diceValues(event.data.defenderDice).length) return null
  const counts = Object.fromEntries(['attackerLosses', 'defenderLosses', 'attackerSoldiers', 'defenderSoldiers'].map(key => [key, parseCount(event.data[key])]))
  if (Object.values(counts).some(value => value === null)) return null
  return { pairs: pairDice(attacker, defender), ...counts }
}

export function markersFromAttack(attack) {
  const attackerId = attack?.attackerCountryId ?? attack?.fromCountryId ?? attack?.from
  const defenderId = attack?.defenderCountryId ?? attack?.toCountryId ?? attack?.to
  return typeof attackerId === 'string' && attackerId.trim() && typeof defenderId === 'string' && defenderId.trim()
    ? { attackerId, defenderId } : null
}

export function liveCountsFromEvent(event, attack) {
  if (!['AttackStarted', 'BattleRound'].includes(event?.type)) return null
  const markers = markersFromAttack(event.type === 'AttackStarted' ? event.data : attack)
  const attacker = parseCount(event.data?.attackerSoldiers)
  const defender = parseCount(event.data?.defenderSoldiers)
  if (!markers || attacker === null || defender === null) return null
  // AttackStarted is the initial count; BattleRound already contains AFTER-loss counts.
  return { [markers.attackerId]: attacker, [markers.defenderId]: defender }
}
