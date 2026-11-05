import { Link } from 'react-router-dom'
import { ArrowLeft, Flag, Swords } from 'lucide-react'
import { toAssignments } from './boardAssignments.js'
import { ownerSlot, panelMode } from './gameActions.js'
import { playerSummary } from './playerSummary.js'
import EventLog from './EventLog.jsx'
import GameOver from './GameOver.jsx'
import Button from '../../components/Button.jsx'
import { worldRegions } from '../map/worldRegions.js'
import WorldMap from '../map/WorldMap.jsx'
import NpcDecisionCard from './NpcDecisionCard.jsx'
import { useAuth } from '../../auth/useAuth.js'

export default function GameLayout({ game, events, battle, playback, battleFast, controls, headerExtras, mapProps, readOnly = false, replay = false, onSeekEvent, presentationClass = '', animationSpeed = 1, mapOverlay }) {
  const { user } = useAuth()
  const mode = panelMode(game)
  const assignments = toAssignments(game, [], worldRegions)
  const current = game.players.find(player => player.id === game.currentPlayerId)
  return <main className={`game-page ${presentationClass}`} style={{ '--replay-speed': animationSpeed }}>
    <header className="game-page-header">
      <Button as={Link} variant="secondary" size="small" to={readOnly && !user ? '/' : '/play'} className="game-page-back"><ArrowLeft size={16} /> {replay ? user ? 'Headquarters' : 'Home' : readOnly ? 'Home' : 'Headquarters'}</Button>
      <h1>World conquest</h1>
      {headerExtras}
      <span className="game-page-round">Round {game.round} · {game.phase} phase</span>
    </header>
    <div className="game-page-layout">
      <section className="game-page-map-area" aria-label="Game board">
        <div className="game-page-board">
        <WorldMap countryAssignments={assignments} showSoldiers territories={game.territories} players={game.players} battleHit={battle.hit} conquered={battle.conquered} fastForward={battleFast} liveCounts={battle.liveCounts} fitted {...mapProps} />
        </div>
        <NpcDecisionCard key={playback.decision?.event.sequence ?? 'no-decision'} decision={playback.decision} game={game} />
        {mapOverlay}
      </section>
      <aside className="game-page-panel" aria-label="Game status">
        {mode === 'finished' && !replay ? <GameOver game={game} readOnly={readOnly} /> : <>
        <section className="game-page-dock-players" aria-label="Players">
        <p className="eyebrow">PLAYERS</p>
        <ol className="game-page-players">
          {game.players.map(player => {
            const summary = playerSummary(game, player)
            return <li key={player.id} className={`${player.id === game.currentPlayerId ? 'game-page-player-current' : ''} ${player.isEliminated ? 'game-page-player-eliminated' : ''}`}>
            <svg className="game-page-owner-dot" viewBox="0 0 10 10" aria-hidden="true"><circle cx="5" cy="5" r="5" fill={worldRegions[summary.slot - 1]?.color} /></svg>
            <div className="game-page-player-name">
              <strong title={summary.name}>{summary.name}</strong>{player.isHuman && !readOnly && <span className="game-page-you"> (you)</span>}
              {mode !== 'finished' && player.pendingReinforcements > 0 && <span className="game-page-badge" aria-label={`${player.pendingReinforcements} pending reinforcements`}>+{player.pendingReinforcements}</span>}
              {player.isEliminated && <span className="game-page-badge game-page-badge-eliminated" aria-label="Eliminated" title="Eliminated">×</span>}
            </div>
            <span className="game-page-player-stats">
              <span className="assignment-soldiers" aria-label={`${summary.soldiers} soldiers`} title="Total soldiers">
                <Swords size={14} aria-hidden="true" />{summary.soldiers}
              </span>
              <span className="assignment-soldiers" aria-label={`${summary.countries} countries owned`} title="Countries owned">
                <Flag size={12} aria-hidden="true" />{summary.countries}
              </span>
            </span>
          </li>})}
        </ol>
        </section>
        <section className="game-page-dock-controls" aria-label="Current turn">
        <div className="game-page-control-heading">
        <p className="eyebrow">{replay ? 'REPLAY' : 'CURRENT TURN'}</p>
        <p className="game-page-turn">
          {current && <svg className="game-page-owner-dot" viewBox="0 0 10 10" aria-hidden="true"><circle cx="5" cy="5" r="5" fill={worldRegions[ownerSlot(game, { ...game.territories.find(t => t.countryId === current.startingCountryId), ownerPlayerId: current.id }) - 1]?.color} /></svg>}
          {mode === 'finished' ? 'Finished' : current?.isHuman ? (readOnly ? `${current.name} to play` : 'Your turn') : `${current?.name ?? 'Unknown'} to play`}
        </p>
        </div>
        {controls}
        </section>
        </>}
      </aside>
      <EventLog events={events} currentSequence={playback.currentEvent?.sequence} onSeek={onSeekEvent} />
    </div>
  </main>
}
