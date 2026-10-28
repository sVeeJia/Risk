import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { ArrowLeft, Swords } from 'lucide-react'
import { getGame } from '../api/games.js'
import { toAssignments } from '../features/game/boardAssignments.js'
import { worldRegions } from '../features/map/worldRegions.js'
import WorldMap from '../features/map/WorldMap.jsx'

export default function Game() {
  const { gameId } = useParams()
  const [result, setResult] = useState({ gameId: null, game: null, assignments: [], error: '' })

  useEffect(() => {
    const controller = new AbortController()
    getGame(gameId, { signal: controller.signal })
      .then(data => {
        if (!controller.signal.aborted) setResult({ gameId, game: data, assignments: toAssignments(data, [], worldRegions), error: '' })
      })
      .catch(err => {
        if (controller.signal.aborted) return
        setResult({ gameId, game: null, assignments: [], error: err.status === 404 ? 'This game was not found.' : err.status === 401 ? 'Your session has expired. Sign in again to view this game.' : err.message })
      })
    return () => controller.abort()
  }, [gameId])

  const { game, assignments, error } = result
  if (result.gameId !== gameId) return <main className="game-page"><p role="status" className="game-page-message">Loading your game...</p></main>
  if (error) return <main className="game-page"><div role="alert" className="game-page-message"><p>{error}</p><Link to="/play">Back to headquarters</Link></div></main>

  // Population isn't needed here, so no country list is loaded.
  const territoryByCountry = new Map(game.territories.map(t => [t.countryId, t]))
  const currentPlayer = game.players.find(p => p.id === game.currentPlayerId)

  return <main className="game-page">
    <header className="game-page-header">
      <Link to="/play" className="game-page-back"><ArrowLeft size={16} /> Headquarters</Link>
      <h1>World conquest</h1>
      <span className="game-page-round">Round {game.round} · {game.phase} phase</span>
    </header>

    <div className="game-page-layout">
      <section className="game-page-board" aria-label="Game board">
        <WorldMap startId={worldRegions[0].id} countryAssignments={assignments} showSoldiers />
      </section>

      <aside className="game-page-panel" aria-label="Game status">
        <p className="eyebrow">CURRENT TURN</p>
        <p className="game-page-turn">
          {currentPlayer?.isHuman ? 'Your turn' : `${currentPlayer?.name ?? 'Unknown'} to play`}
        </p>
        <p className="game-page-note">Attacks and NPC turns are under development.</p>

        <p className="eyebrow">PLAYERS</p>
        <ol className="game-page-players">
          {game.players.map((player, i) => {
            const territory = territoryByCountry.get(player.startingCountryId)
            return <li key={player.id} className={player.id === game.currentPlayerId ? 'game-page-player-current' : ''}>
              <span className="assignment-color" style={{ backgroundColor: worldRegions[(territory?.slot ?? i + 1) - 1]?.color }} aria-hidden="true" />
              <strong title={player.name}>{player.name}{player.isHuman ? ' (you)' : ''}</strong>
              <span className="assignment-soldiers" aria-label={`${territory?.soldiers ?? 0} soldiers`}><Swords size={14} aria-hidden="true" />{territory?.soldiers ?? 0}</span>
            </li>
          })}
        </ol>
      </aside>
    </div>
  </main>
}
