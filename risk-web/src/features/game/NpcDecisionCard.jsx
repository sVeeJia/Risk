import { ownerSlot } from './gameActions.js'
import { worldRegions } from '../map/worldRegions.js'

export default function NpcDecisionCard({ decision, game }) {
  if (decision?.event?.type !== 'NpcDecision') return null
  const { event, ended } = decision
  const player = game.players.find(item => item.id === event.playerId)
  const slot = ownerSlot(game, { ...game.territories.find(item => item.countryId === player?.startingCountryId), ownerPlayerId: player?.id })
  return <aside className={`npc-decision-card ${ended ? 'npc-decision-card-ended' : ''}`} aria-label="NPC decision">
    <header>
      <svg viewBox="0 0 10 10" aria-hidden="true"><circle cx="5" cy="5" r="5" fill={worldRegions[slot - 1]?.color ?? '#94a09e'} /></svg>
      <strong title={player?.name}>{player?.name ?? 'NPC'}</strong>
    </header>
    <p className="npc-decision-message" title={event.message}>{event.message}</p>
    {event.justification && <p className="npc-decision-why" title={`Why: ${event.justification}`}>Why: {event.justification}</p>}
  </aside>
}
