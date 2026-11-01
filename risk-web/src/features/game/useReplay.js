import { useCallback, useEffect, useRef, useState } from 'react'
import { heavyAnimation, pacedDuration, skipTime } from './replayNavigation.js'
import { totalTime } from './replayTimeline.js'

export default function useReplay(steps) {
  const generation = useRef(0)
  const [clock, setClock] = useState({ index: steps.length ? 0 : -1, timeMs: 0, playing: Boolean(steps.length), speed: 1, animate: true, revision: 0 })
  const totalMs = totalTime(steps)
  const seek = useCallback((index, end = false) => { generation.current++; setClock(previous => ({ ...previous,
    index, timeMs: end ? totalTime(steps) : steps[index]?.startMs ?? 0, playing: false, animate: false, revision: previous.revision + 1,
  })) }, [steps])
  const skip = useCallback(delta => { generation.current++; setClock(previous => {
    const index = skipTime(steps, previous.index, delta, previous.timeMs)
    const end = delta > 0 && previous.timeMs + delta >= totalTime(steps)
    return { ...previous, index, timeMs: end ? totalTime(steps) : steps[index]?.startMs ?? 0, playing: false, animate: false, revision: previous.revision + 1 }
  }) }, [steps])
  const toggle = useCallback(() => { generation.current++; setClock(previous => {
    if (!steps.length) return previous
    if (previous.timeMs >= totalTime(steps)) return { ...previous, index: 0, timeMs: 0, playing: true, animate: true, revision: previous.revision + 1 }
    return { ...previous, playing: !previous.playing }
  }) }, [steps])
  useEffect(() => {
    if (!clock.playing || !steps.length) return
    let frame, cancelled = false
    const token = ++generation.current
    const started = performance.now(), fromMs = clock.timeMs
    const boundary = steps[clock.index].startMs + steps[clock.index].durationMs
    const tick = now => {
      if (cancelled || generation.current !== token) return
      const timeMs = Math.min(boundary, fromMs + (now - started) * clock.speed)
      if (timeMs >= boundary) {
        const last = clock.index === steps.length - 1
        setClock(previous => ({ ...previous, timeMs, index: last ? previous.index : previous.index + 1,
          playing: !last, animate: heavyAnimation(previous.speed) }))
      } else {
        setClock(previous => ({ ...previous, timeMs }))
        frame = setTimeout(() => tick(performance.now()), Math.min(20, pacedDuration(boundary - timeMs, clock.speed)))
      }
    }
    frame = setTimeout(() => tick(performance.now()), Math.min(20, pacedDuration(boundary - fromMs, clock.speed)))
    return () => { cancelled = true; clearTimeout(frame) }
    // The running frame owns timeMs until a boundary; seeking changes index/playing/revision.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [clock.playing, clock.index, clock.speed, clock.revision, steps])
  useEffect(() => {
    const keydown = event => {
      if (!steps.length || event.defaultPrevented) return
      if (event.altKey || event.ctrlKey || event.metaKey || event.target.closest('input,textarea,select,[contenteditable="true"]')) return
      if (event.key === ' ' && event.target.closest('button,a,[role="button"]')) return
      if (![' ', 'ArrowLeft', 'ArrowRight', 'Home', 'End'].includes(event.key)) return
      event.preventDefault()
      if (event.key === ' ') toggle()
      if (event.key === 'ArrowLeft') skip(-5000)
      if (event.key === 'ArrowRight') skip(5000)
      if (event.key === 'Home') seek(0)
      if (event.key === 'End') seek(steps.length - 1, true)
    }
    window.addEventListener('keydown', keydown)
    return () => window.removeEventListener('keydown', keydown)
  }, [seek, skip, toggle, steps.length])
  return { ...clock, totalMs, seek, skip, toggle,
    restart: () => { generation.current++; setClock(previous => ({ ...previous, index: steps.length ? 0 : -1, timeMs: 0, playing: Boolean(steps.length), animate: false, revision: previous.revision + 1 })) },
    setSpeed: speed => { if (speed === clock.speed) return; generation.current++; setClock(previous => ({ ...previous, speed, animate: previous.animate && heavyAnimation(speed) })) },
  }
}
