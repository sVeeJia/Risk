import { useCallback, useEffect, useReducer, useRef, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { ArrowLeft, Swords } from 'lucide-react'
import * as games from '../api/games.js'
import { toAssignments } from '../features/game/boardAssignments.js'
import { findHuman, ownerSlot, panelMode } from '../features/game/gameActions.js'
import ActionPanel from '../features/game/ActionPanel.jsx'
import EventLog from '../features/game/EventLog.jsx'
import GameOver from '../features/game/GameOver.jsx'
import Button from '../components/Button.jsx'
import { worldRegions } from '../features/map/worldRegions.js'
import WorldMap from '../features/map/WorldMap.jsx'
import useNpcPlayback from '../features/game/useNpcPlayback.js'
import { arrowForEvent, arrowFromGame } from '../features/map/arrows.js'
import { markersFromAttack } from '../features/game/battle.js'
import { battlePresentation, emptyBattle } from '../features/game/battlePresentation.js'
import BattleBoard from '../features/game/BattleBoard.jsx'
import NpcDecisionCard from '../features/game/NpcDecisionCard.jsx'

const actions = {
  attack: games.attack, roll: games.rollAttack, stop: games.stopAttack,
  conquest: games.transferConquest, reinforce: games.placeReinforcements,
  transfer: games.transfer, end: games.endTurn,
}
const noBody = new Set(['roll', 'stop', 'end'])
const emptySelection = { from: null, to: null }

// A new route gets an independent request lifetime and game state.
export default function Game() {
  const { gameId } = useParams()
  return <GameScreen key={gameId} gameId={gameId} />
}

function GameScreen({ gameId }) {
  const [game, setGame] = useState(null)
  const [events, setEvents] = useState([])
  const [selection, setSelection] = useState(emptySelection)
  const [actionMode, setActionMode] = useState('attack')
  const [busy, setBusy] = useState(true)
  const [error, setError] = useState('')
  const request = useRef(null)
  const [moveArrow, setMoveArrow] = useState(null)
  const [historyReady, setHistoryReady] = useState(false)
  const [battle, dispatchBattle] = useReducer(battlePresentation, emptyBattle)
  const onBattleEvent = useCallback((event, fastForward) => dispatchBattle({ type: 'event', event, live: true, fastForward }), [])
  const onBattleTurn = useCallback(() => dispatchBattle({ type: 'turnEnd' }), [])
  const playback = useNpcPlayback({ game, gameId, busy: busy || !historyReady || Boolean(moveArrow), setGame, setEvents, onBattleEvent, onBattleTurn })
  const locked = busy || Boolean(game && ['npc', 'finished'].includes(panelMode(game)))
  useEffect(() => {
    if (!moveArrow) return
    const timer = setTimeout(() => setMoveArrow(null), 2000)
    return () => clearTimeout(timer)
  }, [moveArrow])

  useEffect(() => {
    const controller = new AbortController()
    request.current = controller
    games.loadGameEvents(gameId, { signal: controller.signal }).then(history => {
      if (controller.signal.aborted) return
      // An action may finish before history loads; preserve its new events.
      setEvents(previous => [...new Map([...history, ...previous].map(event => [event.sequence, event])).values()]
        .sort((a, b) => a.sequence - b.sequence))
      setHistoryReady(true)
    })
    games.getGame(gameId, { signal: controller.signal }).then(data => {
      if (controller.signal.aborted) return
      setGame(data)
      dispatchBattle({ type: 'load', game: data })
    }).catch(err => {
      if (!controller.signal.aborted) setError(err.status === 404 ? 'This game was not found.' : err.message)
    }).finally(() => {
      if (!controller.signal.aborted) { request.current = null; setBusy(false) }
    })
    return () => { controller.abort(); request.current?.abort() }
  }, [gameId])

  async function perform(action, body) {
    if (request.current || locked) return
    const controller = new AbortController()
    request.current = controller
    setBusy(true)
    setError('')
    try {
      const options = { signal: controller.signal }
      const response = await (noBody.has(action) ? actions[action](gameId, options) : actions[action](gameId, body, options))
      if (controller.signal.aborted) return
      setGame(response.game)
      setEvents(previous => [...previous, ...response.events])
      for (const event of response.events) dispatchBattle({ type: 'event', event })
      setSelection(emptySelection)
      const move = response.events.map(arrowForEvent).find(arrow => arrow?.kind === 'move')
      setMoveArrow(move ? { ...move, transient: true, sequence: response.events.at(-1)?.sequence } : null)
    } catch (err) {
      if (!controller.signal.aborted) setError(err.status === 404 ? 'This game was not found.' : err.message)
    } finally {
      if (!controller.signal.aborted) { request.current = null; setBusy(false) }
    }
  }

  if (!game) return <main className="game-page"><div className="game-page-message">
    {error ? <p role="alert">{error}</p> : <p role="status">Loading your game...</p>}
    <Button as={Link} variant="secondary" to="/play">Back to headquarters</Button>
  </div></main>

  const human = findHuman(game)
  const mode = panelMode(game)
  const assignments = toAssignments(game, [], worldRegions)
  const current = game.players.find(player => player.id === game.currentPlayerId)
  const preview = mode === 'action' && actionMode === 'attack' && selection.from && selection.to
    ? { kind: 'attack', from: selection.from, to: selection.to, preview: true } : null
  const arrow = mode === 'finished' ? null : moveArrow ?? (mode === 'npc' ? playback.arrow : arrowFromGame(game) ?? preview)
  const markers = markersFromAttack(mode === 'npc' ? battle.attack : game.activeAttack)
  const battleFast = playback.playing ? playback.fastForward : battle.board?.fastForward ?? false
  function select(countryId) {
    if (locked) return
    const territory = game.territories.find(t => t.countryId === countryId)
    const own = territory.ownerPlayerId === human.id
    if (mode === 'reinforce' && own) setSelection({ from: countryId, to: null })
    if (mode !== 'action') return
    if (actionMode === 'attack') {
      if (own && territory.soldiers >= 2) setSelection({ from: countryId, to: null })
      else if (!own && selection.from) setSelection(previous => ({ ...previous, to: countryId }))
    } else if (own) {
      if (!selection.from || selection.to || selection.from === countryId) setSelection({ from: countryId, to: null })
      else setSelection(previous => ({ ...previous, to: countryId }))
    }
  }

  return <main className="game-page">
    <header className="game-page-header">
      <Button as={Link} variant="secondary" size="small" to="/play" className="game-page-back"><ArrowLeft size={16} /> Headquarters</Button>
      <h1>World conquest</h1>
      <span className="game-page-round">Round {game.round} · {game.phase} phase</span>
    </header>
    <div className="game-page-layout">
      <section className="game-page-map-area" aria-label="Game board">
        <div className="game-page-board">
        <WorldMap countryAssignments={assignments} showSoldiers onSelect={select} selectedFrom={selection.from} selectedTo={selection.to} territories={game.territories} players={game.players} disabled={locked} arrow={arrow} reinforcement={playback.currentEvent?.type === 'ReinforcementsPlaced' ? playback.currentEvent : null} markers={markers} battleHit={battle.hit} conquered={battle.conquered} fastForward={battleFast} liveCounts={battle.liveCounts} />
        </div>
        <NpcDecisionCard key={playback.decision?.event.sequence ?? 'no-decision'} decision={playback.decision} game={game} />
      </section>
      <aside className="game-page-panel" aria-label="Game status">
        {mode === 'finished' ? <GameOver game={game} /> : <>
        <section className="game-page-dock-players" aria-label="Players">
        <p className="eyebrow">PLAYERS</p>
        <ol className="game-page-players">
          {game.players.map(player => <li key={player.id} className={`${player.id === game.currentPlayerId ? 'game-page-player-current' : ''} ${player.isEliminated ? 'game-page-player-eliminated' : ''}`}>
            <svg className="game-page-owner-dot" viewBox="0 0 10 10" aria-hidden="true"><circle cx="5" cy="5" r="5" fill={worldRegions[ownerSlot(game, { ...game.territories.find(t => t.countryId === player.startingCountryId), ownerPlayerId: player.id }) - 1]?.color} /></svg>
            <div className="game-page-player-name">
              <strong title={player.name}>{player.name}</strong>{player.isHuman && <span className="game-page-you"> (you)</span>}
              {player.pendingReinforcements > 0 && <span className="game-page-badge" aria-label={`${player.pendingReinforcements} pending reinforcements`}>+{player.pendingReinforcements}</span>}
              {player.isEliminated && <span className="game-page-badge game-page-badge-eliminated" aria-label="Eliminated" title="Eliminated">×</span>}
            </div>
            <span className="assignment-soldiers" aria-label={`${game.territories.find(t => t.countryId === player.startingCountryId)?.soldiers ?? 0} soldiers in starting country`}>
              <Swords size={14} aria-hidden="true" />{game.territories.find(t => t.countryId === player.startingCountryId)?.soldiers ?? 0}
            </span>
          </li>)}
        </ol>
        </section>
        <section className="game-page-dock-controls" aria-label="Current turn">
        <div className="game-page-control-heading">
        <p className="eyebrow">CURRENT TURN</p>
        <p className="game-page-turn">
          {current && <svg className="game-page-owner-dot" viewBox="0 0 10 10" aria-hidden="true"><circle cx="5" cy="5" r="5" fill={worldRegions[ownerSlot(game, { ...game.territories.find(t => t.countryId === current.startingCountryId), ownerPlayerId: current.id }) - 1]?.color} /></svg>}
          {current?.isHuman ? 'Your turn' : `${current?.name ?? 'Unknown'} to play`}
        </p>
        </div>
        <ActionPanel key={`${mode}:${actionMode}:${game.round}:${game.activeAttack?.attackerCountryId}`} game={game} selection={selection} actionMode={actionMode} busy={locked} error={playback.error || error} playback={playback} onAction={perform} onMode={value => { setActionMode(value); setSelection(emptySelection) }} battleBoard={<BattleBoard board={battle.board} territories={game.territories} players={game.players} fastForward={battleFast} />} />
        </section>
        </>}
      </aside>
      <EventLog events={events} currentSequence={playback.currentEvent?.sequence} />
    </div>
  </main>
}
