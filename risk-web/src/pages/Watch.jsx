import { Link, Navigate, useParams } from 'react-router-dom'
import Button from '../components/Button.jsx'
import GameLayout from '../features/game/GameLayout.jsx'
import BattleBoard from '../features/game/BattleBoard.jsx'
import SpectatorStatus from '../features/game/SpectatorStatus.jsx'
import useSpectator from '../features/game/useSpectator.js'
import { findHuman } from '../features/game/gameActions.js'
import { markersFromAttack } from '../features/game/battle.js'
import { arrowFromGame } from '../features/map/arrows.js'

export default function Watch() {
  const { gameId } = useParams()
  return <WatchScreen key={gameId} gameId={gameId} />
}

function WatchScreen({ gameId }) {
  const playback = useSpectator(gameId)
  const { game, events, battle, notFound, error } = playback
  if (game?.phase === 'Finished') return <Navigate to={`/replay/${encodeURIComponent(game.id)}`} replace />
  if (notFound || !game) return <main className="game-page"><div className="game-page-message">
    <p role={error ? 'alert' : 'status'}>{notFound ? 'Game not found' : error || 'Loading game…'}</p>
    <Button as={Link} variant="secondary" to="/">Home</Button>
    {!notFound && error && <Button variant="secondary" onClick={playback.retry}>Reconnect</Button>}
  </div></main>
  return <GameLayout readOnly game={game} events={events} battle={battle} playback={playback} battleFast={playback.fastForward}
    headerExtras={<div className="watch-banner"><span title={`You are watching ${findHuman(game)?.name ?? 'a player'}'s game`}>You are watching {findHuman(game)?.name ?? 'a player'}'s game</span><span className="watch-chip">read-only</span><span className={`watch-chip watch-${playback.status === 'Live' ? 'live' : 'offline'}`} role="status">{playback.status}</span></div>}
    mapProps={{ disabled: true, arrow: playback.arrow ?? arrowFromGame(game), markers: markersFromAttack(battle.attack),
      reinforcement: playback.currentEvent?.type === 'ReinforcementsPlaced' ? playback.currentEvent : null }}
    controls={<SpectatorStatus game={game} playback={playback} battleBoard={<BattleBoard board={battle.board} territories={game.territories} players={game.players} fastForward={playback.fastForward} />} />} />
}
