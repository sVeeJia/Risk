import { useEffect, useRef, useState } from 'react'
import { playNpcTurn } from '../../api/games.js'
import { isNpcTurn } from './gameActions.js'
import { playbackPause } from './playbackSpeed.js'
import { replayNpcTurns } from './npcPlayback.js'

export default function useNpcPlayback({ game, gameId, busy, setGame, setEvents }) {
  const [playing, setPlaying] = useState(false)
  const [fastForward, setFastForward] = useState(false)
  const [error, setError] = useState('')
  const [retry, setRetry] = useState(0)
  const [currentEvent, setCurrentEvent] = useState(null)
  const [arrow, setArrow] = useState(null)
  const speed = useRef(false)
  const latest = useRef(game)
  const token = useRef(null)
  const enabled = Boolean(game && isNpcTurn(game) && !busy)
  useEffect(() => { latest.current = game }, [game])

  useEffect(() => {
    if (!enabled) return
    const controller = new AbortController()
    const runToken = {}
    token.current = runToken
    let cancelled = false
    const active = () => !cancelled && !controller.signal.aborted && token.current === runToken
    async function run() {
      // StrictMode cleanup invalidates the first effect before a POST starts.
      await Promise.resolve()
      if (!active()) return
      setPlaying(true)
      setError('')
      speed.current = false
      setFastForward(false)
      try {
        await replayNpcTurns({
          game: latest.current,
          request: () => playNpcTurn(gameId, { signal: controller.signal }),
          pause: kind => playbackPause(kind, () => speed.current, controller.signal),
          isActive: active,
          onEvent: (event, displayed) => {
            setArrow(displayed)
            setCurrentEvent(event)
            setEvents(previous => [...previous.filter(item => item.sequence !== event.sequence), event])
          },
          onTurn: nextGame => { setArrow(null); setCurrentEvent(null); setGame(nextGame) },
        })
      } catch (err) {
        if (active()) setError(err.message)
      } finally {
        if (active()) {
          setPlaying(false)
          setCurrentEvent(null)
          setArrow(null)
          speed.current = false
          setFastForward(false)
        }
      }
    }
    run()
    return () => { cancelled = true; controller.abort(); token.current = null }
  }, [enabled, gameId, retry, setGame, setEvents])

  return { playing: enabled && playing, fastForward: enabled && fastForward, error: enabled ? error : '', currentEvent: enabled ? currentEvent : null, arrow: enabled ? arrow : null,
    toggleSpeed: () => { speed.current = !speed.current; setFastForward(speed.current) },
    retry: () => setRetry(value => value + 1),
  }
}
