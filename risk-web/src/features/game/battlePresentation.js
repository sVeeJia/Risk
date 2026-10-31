import { battleFromEvent, liveCountsFromEvent, markersFromAttack, parseCount } from './battle.js'

export const emptyBattle = { attack: null, board: null, liveCounts: {}, hit: null, conquered: null }
function initialBoard(game) {
  const markers = markersFromAttack(game.activeAttack)
  if (!markers) return null
  const country = id => game.territories.find(item => item.countryId === id)
  return { id: `load:${game.id}`, ...markers, round: 0, result: null, ended: false,
    counts: { attackerSoldiers: parseCount(country(markers.attackerId)?.soldiers), defenderSoldiers: parseCount(country(markers.defenderId)?.soldiers) } }
}

// Presentation state only. Never changes game state or derives soldier totals.
export function battlePresentation(state, action) {
  if (action.type === 'load') return { ...emptyBattle, attack: action.game.activeAttack, board: initialBoard(action.game) }
  if (action.type === 'turnEnd') return { ...state, attack: null, liveCounts: {}, hit: null, conquered: null,
    board: state.board ? { ...state.board, ended: true, visible: false } : null }
  if (action.type !== 'event') return state
  const { event, live = false, fastForward = false } = action
  if (!event || typeof event.type !== 'string') return state
  if (event.type === 'TurnStarted') return { ...state, board: state.board ? { ...state.board, visible: false } : null }
  if (event.type === 'AttackStarted') {
    const markers = markersFromAttack(event.data)
    if (!markers) return state
    const attack = { attackerCountryId: markers.attackerId, defenderCountryId: markers.defenderId }
    const counts = { attackerSoldiers: parseCount(event.data.attackerSoldiers), defenderSoldiers: parseCount(event.data.defenderSoldiers) }
    return { ...emptyBattle, attack,
      board: { id: `attack:${event.sequence}`, ...markers, round: 0, result: null, counts, ended: false, fastForward },
      liveCounts: live ? liveCountsFromEvent(event, attack) ?? {} : {},
    }
  }
  if (event.type === 'BattleRound' && state.attack && state.board) {
    const result = battleFromEvent(event)
    if (!result) return state
    const counts = { attackerSoldiers: result.attackerSoldiers, defenderSoldiers: result.defenderSoldiers }
    return { ...state,
      board: { ...state.board, round: state.board.round + 1, result, before: state.board.counts, counts, sequence: event.sequence, fastForward, visible: true },
      hit: { sequence: event.sequence, ...markersFromAttack(state.attack), result },
      liveCounts: live ? liveCountsFromEvent(event, state.attack) ?? state.liveCounts : {},
    }
  }
  if (event.type === 'CountryConquered' && typeof event.data?.countryId === 'string') {
    return { ...state, conquered: { countryId: event.data.countryId, sequence: event.sequence } }
  }
  if (event.type === 'AttackEnded') return { ...state, attack: null,
    board: state.board ? { ...state.board, ended: true, reason: event.data?.reason, fastForward } : null }
  return state
}
