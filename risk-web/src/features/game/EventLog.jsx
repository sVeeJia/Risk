import { memo, useEffect, useRef } from 'react'

function EventLog({ events, currentSequence, onSeek }) {
  const list = useRef(null)
  useEffect(() => {
    if (!list.current) return
    if (onSeek) {
      const row = list.current.querySelector('[aria-current="step"]')
      if (row) {
        const top = row.offsetTop - list.current.offsetTop
        if (top < list.current.scrollTop || top + row.offsetHeight > list.current.scrollTop + list.current.clientHeight) list.current.scrollTop = Math.max(0, top - list.current.clientHeight / 2)
      }
    } else list.current.scrollTop = list.current.scrollHeight
  }, [events, currentSequence, onSeek])
  return <section className="event-log" aria-label="Game events">
    <h2>Game events</h2>
    <ol ref={list} role="log" aria-live="polite" aria-relevant="additions">
      {events.map((event, index) => <li key={event.sequence} aria-current={event.sequence === currentSequence ? 'step' : undefined}
        role={onSeek ? 'button' : undefined} tabIndex={onSeek ? 0 : undefined} onClick={onSeek ? () => onSeek(index) : undefined}
        onKeyDown={onSeek ? e => { if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); onSeek(index) } } : undefined}
        className={`${event.type === 'NpcDecision' ? 'event-log-decision' : 'event-log-battle'} ${event.sequence === currentSequence ? 'event-log-current' : ''}`}>
        <p>{event.message}</p>
        {event.justification && <p className="event-log-why">Why: {event.justification}</p>}
      </li>)}
    </ol>
  </section>
}

export default memo(EventLog)
