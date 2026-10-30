import { useEffect, useRef } from 'react'

export default function EventLog({ events, currentSequence }) {
  const list = useRef(null)
  useEffect(() => {
    if (list.current) list.current.scrollTop = list.current.scrollHeight
  }, [events])
  return <section className="event-log" aria-label="Game events">
    <h2>Game events</h2>
    <ol ref={list} role="log" aria-live="polite" aria-relevant="additions">
      {events.map(event => <li key={event.sequence} aria-current={event.sequence === currentSequence ? 'step' : undefined} className={`${event.type === 'NpcDecision' ? 'event-log-decision' : 'event-log-battle'} ${event.sequence === currentSequence ? 'event-log-current' : ''}`}>
        <p>{event.message}</p>
        {event.justification && <p className="event-log-why">Why: {event.justification}</p>}
      </li>)}
    </ol>
  </section>
}
