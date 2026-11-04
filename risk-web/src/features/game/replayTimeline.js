import { uniqueEvents } from './commentary.js'
import { applyReplayEvent } from './replayState.js'
import { battlePresentation, emptyBattle } from './battlePresentation.js'
import { replayArrow } from '../map/arrows.js'

const durations = { GameStarted: 500, TurnStarted: 500, ReinforcementsReceived: 600, ReinforcementsPlaced: 900,
  AttackStarted: 900, BattleRound: 1400, CountryConquered: 1200, Transfer: 900, PlayerEliminated: 1000,
  AttackEnded: 500, GameWon: 1800, GameLost: 1800, NpcDecision: 1400, TurnSkipped: 500 }

export function buildTimeline(initialState, events) {
  let state = initialState, startMs = 0, battle = emptyBattle, arrow = null, decision = null
  const human = initialState.players.find(player => player.isHuman)
  return uniqueEvents(events).filter(event => event.type !== 'Commentary').map((event, index) => {
    const before = state, result = applyReplayEvent(state, event)
    state = result.state
    // Older AttackStarted records have only endpoints; the reconstructed pre-event counts supply visual context.
    const visualEvent = event.type === 'AttackStarted' ? { ...event, data: { ...event.data,
      attackerSoldiers: before.territories.find(item => item.countryId === event.data?.fromCountryId)?.soldiers,
      defenderSoldiers: before.territories.find(item => item.countryId === event.data?.toCountryId)?.soldiers,
    } } : event
    if (!result.issues.length) {
      battle = battlePresentation(battle, { type: 'event', event: visualEvent })
      arrow = replayArrow(arrow, event)
      if (event.type === 'TurnStarted') { decision = null; arrow = null; battle = { ...battle, attack: null } }
      if (event.type === 'NpcDecision') decision = { event, ended: false }
      if (state.phase === 'Finished') { arrow = null; battle = { ...battle, attack: null } }
    }
    let marker
    if (!result.issues.length && event.type === 'CountryConquered') marker = { kind: 'conquest', human: event.playerId === human?.id,
      title: `Round ${state.round} · You conquered ${state.territories.find(item => item.countryId === event.data.countryId)?.name ?? event.data.countryId}` }
    if (!result.issues.length && event.type === 'PlayerEliminated') marker = { kind: 'elimination' }
    if (!result.issues.length && ['GameWon', 'GameLost'].includes(event.type)) marker = { kind: 'gameOver', won: event.type === 'GameWon' && event.playerId === human?.id, title: event.type === 'GameWon' && event.playerId === human?.id ? 'You won' : 'You lost' }
    const durationMs = durations[event.type] ?? 600
    const step = { index, event, stateAfter: state, round: state.round, currentPlayerId: state.currentPlayerId, startMs, durationMs, marker,
      issues: result.issues.map(message => ({ sequence: event.sequence, message })), visual: { battle, arrow, decision } }
    startMs += durationMs
    return step
  })
}

export const stateAt = (steps, index, initialState) => steps[index]?.stateAfter ?? initialState
export const totalTime = steps => steps.length ? steps.at(-1).startMs + steps.at(-1).durationMs : 0
export function replayMarkers(steps) {
  const end = steps.findLast(step => step.marker?.kind === 'gameOver')
  return steps.filter(step => step === end || step.marker?.kind === 'conquest' && step.marker.human)
}
