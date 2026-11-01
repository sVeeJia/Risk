import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { useAuth } from '../auth/useAuth.js'
import Button from '../components/Button.jsx'
import { getPublicGame, getPublicGameEvents } from '../api/spectate.js'
import { loadHistory } from '../features/game/spectatorEvents.js'
import { initialReplayState, replayDifferences } from '../features/game/replayState.js'
import { buildTimeline, stateAt } from '../features/game/replayTimeline.js'
import { emptyBattle } from '../features/game/battlePresentation.js'
import { findHuman } from '../features/game/gameActions.js'
import { markersFromAttack } from '../features/game/battle.js'
import useReplay from '../features/game/useReplay.js'
import GameLayout from '../features/game/GameLayout.jsx'
import BattleBoard from '../features/game/BattleBoard.jsx'
import ReplayControls from '../features/game/ReplayControls.jsx'

export default function Replay() {
  const { gameId } = useParams()
  return <LoadReplay key={gameId} gameId={gameId} />
}

function LoadReplay({ gameId }) {
  const { user } = useAuth()
  const [data, setData] = useState(null)
  const [error, setError] = useState('')
  const [attempt, setAttempt] = useState(0)
  useEffect(() => {
    const controller = new AbortController(), options = { signal: controller.signal }
    Promise.all([getPublicGame(gameId, options), loadHistory((after, limit) => getPublicGameEvents(gameId, after, limit, options))]).then(([game, events]) => {
      if (controller.signal.aborted) return
      const initial = initialReplayState(game, events), steps = buildTimeline(initial, events)
      const differences = replayDifferences(stateAt(steps, steps.length - 1, initial), game)
      const issues = steps.flatMap(step => step.issues)
      if (events.length && !events.some(event => event.type === 'TurnStarted')) issues.push({ message: 'Missing initial TurnStarted event' })
      if (issues.length || differences.length) console.warn('This replay may be incomplete', { issues, differences })
      setData({ game, events, initial, steps, incomplete: Boolean(issues.length || differences.length) })
    }).catch(err => { if (!controller.signal.aborted) setError(err.status === 404 ? 'Game not found' : err.message) })
    return () => controller.abort()
  }, [gameId, attempt])
  if (data) return <ReadyReplay data={data} />
  return <main className="game-page"><div className="game-page-message">
    <p role={error ? 'alert' : 'status'}>{error || 'Loading all replay events…'}</p>
    <Button as={Link} variant="secondary" to={user ? '/play' : '/'}>{user ? 'Headquarters' : 'Home'}</Button>
    {error && error !== 'Game not found' && <Button variant="secondary" onClick={() => { setError(''); setAttempt(value => value + 1) }}>Retry</Button>}
  </div></main>
}

function ReadyReplay({ data }) {
  const { game: final, events, initial, steps, incomplete } = data
  const clock = useReplay(steps)
  const step = steps[clock.index], game = stateAt(steps, clock.index, initial)
  const visual = step?.visual ?? { battle: emptyBattle, arrow: null, decision: null }
  const animate = clock.animate && clock.speed <= 4
  const battle = { ...visual.battle, hit: animate && step?.event.type === 'BattleRound' ? visual.battle.hit : null, conquered: animate && step?.event.type === 'CountryConquered' ? visual.battle.conquered : null }
  const human = findHuman(game), ended = steps.length > 0 && !clock.playing && clock.index === steps.length - 1
  return <GameLayout readOnly replay game={game} events={events} battle={battle} battleFast={clock.speed > 1}
    presentationClass={`replay-page ${animate ? '' : 'replay-static'} ${clock.playing ? '' : 'replay-paused'}`} animationSpeed={clock.speed}
    playback={{ decision: visual.decision, currentEvent: step?.event }} onSeekEvent={clock.seek}
    headerExtras={<div className="watch-banner"><span>Replay · {human?.name ?? 'Player'}</span><span className="watch-chip">read-only</span>
      {incomplete && <span className="replay-notice" role="status">This replay may be incomplete</span>}
      {final.phase !== 'Finished' && <Link to={`/watch/${encodeURIComponent(final.id)}`}>Game in progress, watch live</Link>}
    </div>}
    mapOverlay={ended && final.phase === 'Finished' ? <div className="replay-result" role="status">{final.winnerPlayerId === human?.id ? 'You won' : 'You lost'} · Round {final.round}</div> : null}
    mapProps={{ disabled: true, arrow: visual.arrow, markers: markersFromAttack(game.activeAttack), reinforcement: animate && step?.event.type === 'ReinforcementsPlaced' ? step.event : null }}
    controls={<ReplayControls steps={steps} clock={clock} board={<BattleBoard key={clock.revision} board={battle.board} territories={game.territories} players={game.players} fastForward={clock.speed > 1} animate={animate && step?.event.type === 'BattleRound'} />} />} />
}
