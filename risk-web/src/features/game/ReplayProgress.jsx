import { useEffect, useMemo, useRef, useState } from 'react'
import { Flag, Trophy, X } from 'lucide-react'
import { replayMarkers } from './replayTimeline.js'
import { seekFraction, stackFlags, timeLabel } from './replayNavigation.js'

export default function ReplayProgress({ steps, clock }) {
  const rail = useRef(null)
  const [width, setWidth] = useState(300)
  useEffect(() => {
    const observer = new ResizeObserver(entries => setWidth(entries[0].contentRect.width))
    observer.observe(rail.current)
    return () => observer.disconnect()
  }, [])
  const flags = useMemo(() => stackFlags(replayMarkers(steps), width, clock.totalMs), [steps, width, clock.totalMs])
  const step = steps[clock.index]
  return <div className="replay-progress">
    <div className="replay-time"><span>{timeLabel(clock.timeMs)} / {timeLabel(clock.totalMs)}</span><span>Event {clock.index + 1} / {steps.length} · Round {step?.round ?? 1}</span></div>
    <div className="replay-progress-rail" ref={rail}>
      <svg className="replay-marker-lines" width="100%" height="48" aria-hidden="true">{flags.map(flag => <line key={flag.index} x1={flag.x} y1={flag.lane * 14 + 12} x2={flag.anchor} y2="47" />)}</svg>
      {flags.map(flag => <button key={flag.index} type="button" className={`replay-marker replay-marker-${flag.marker.kind}`}
        style={{ left: flag.x, top: flag.lane * 14, width: flag.size }} title={flag.marker.title} aria-label={flag.marker.title}
        onClick={() => clock.seek(flag.index, flag.marker.kind === 'gameOver' && flag.index === steps.length - 1)}>
        {flag.marker.kind === 'conquest' ? <Flag /> : flag.marker.won ? <Trophy /> : <X />}
      </button>)}
      <input type="range" aria-label="Replay progress" min="0" max={clock.totalMs || 1} step="1" value={clock.timeMs}
        aria-valuenow={Math.round(clock.timeMs)}
        aria-valuetext={`${timeLabel(clock.timeMs)} of ${timeLabel(clock.totalMs)}, event ${clock.index + 1} of ${steps.length}, round ${step?.round ?? 1}`}
        disabled={!steps.length} onChange={event => clock.seek(seekFraction(steps, Number(event.target.value) / clock.totalMs), Number(event.target.value) >= clock.totalMs)}
        onKeyDown={event => {
          if (['ArrowLeft', 'ArrowRight', 'Home', 'End', ' '].includes(event.key)) event.preventDefault()
          if (event.key === 'ArrowLeft') clock.skip(-5000)
          if (event.key === 'ArrowRight') clock.skip(5000)
          if (event.key === 'Home') clock.seek(0)
          if (event.key === 'End') clock.seek(steps.length - 1, true)
          if (event.key === ' ') clock.toggle()
        }} />
    </div>
  </div>
}
