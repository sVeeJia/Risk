import { useEffect, useRef, useState } from 'react'
import * as api from '../../api/spectate.js'
import { emptyBattle, battlePresentation } from './battlePresentation.js'
import { createSpectatorQueue } from './spectatorQueue.js'
import { spectateSession, isGameNotFound } from './spectatorSession.js'
import { playbackPause } from './playbackSpeed.js'

const initial = { game: null, events: [], battle: emptyBattle, playing: false, arrow: null, currentEvent: null, decision: null, status: 'Reconnecting…', error: '', notFound: false }

export default function useSpectator(gameId) {
  const [state, setState] = useState(initial)
  const [fastForward, setFastForward] = useState(false)
  const [attempt, setAttempt] = useState(0)
  const speed = useRef(false)
  useEffect(() => {
    const controller = new AbortController()
    const { signal } = controller
    const update = fn => { if (!signal.aborted) setState(fn) }
    const onError = error => {
      update(previous => ({ ...previous, error: error.message, notFound: isGameNotFound(error) }))
      if (isGameNotFound(error)) controller.abort()
    }
    // The first StrictMode lifetime is cancelled before it can open a connection.
    Promise.resolve().then(() => {
      if (signal.aborted) return
      return spectateSession({ id: gameId, signal, getGame: api.getPublicGame, getEvents: api.getPublicGameEvents,
        createConnection: api.createGameConnection, onError,
        onStatus: status => update(previous => ({ ...previous, status })),
        onInitial: (game, events) => update(() => ({ ...initial, game, events, battle: battlePresentation(emptyBattle, { type: 'load', game }) })),
        createQueue: (game, lastSeenSequence) => createSpectatorQueue({ game, lastSeenSequence, signal,
          pause: kind => playbackPause(kind, () => speed.current, signal),
          onBusy: playing => update(previous => ({ ...previous, playing, currentEvent: playing ? previous.currentEvent : null, arrow: playing ? previous.arrow : null })),
          onError,
          onEvent: (event, arrow) => update(previous => ({ ...previous,
            events: [...previous.events, event], arrow, currentEvent: event,
            decision: event.type === 'NpcDecision' ? { event, ended: false } : event.type === 'TurnStarted' ? null : previous.decision,
            battle: battlePresentation(previous.battle, { type: 'event', event, live: true, fastForward: speed.current }),
          })),
          onGame: (next, turnChanged) => update(previous => ({ ...previous, game: next,
            decision: turnChanged && previous.decision ? { ...previous.decision, ended: true } : previous.decision,
            battle: battlePresentation(previous.battle, { type: 'snapshot', game: next }),
          })),
        }),
      })
    }).catch(onError)
    return () => controller.abort()
  }, [gameId, attempt])
  return { ...state, fastForward,
    toggleSpeed: () => { speed.current = !speed.current; setFastForward(speed.current) },
    retry: () => setAttempt(value => value + 1),
  }
}
