import { eventMessage, safeText, isGameAction } from './eventText.js'
import { logRows, uniqueEvents } from './commentary.js'
import CommentarySentence from './CommentarySentence.jsx'
import { memo, useEffect, useRef, useState } from 'react'

function EventLog({ events, currentSequence, onSeek }) {
  const [filter, setFilter] = useState('all')
  const rows = logRows(events, filter)
  const timelineEvents = uniqueEvents(events).filter(isGameAction)
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
  }, [events, currentSequence, onSeek, filter])
  return <section className="event-log" aria-label="Game events">
    <h2>Game events</h2>
    <div className="event-log-filter" role="group" aria-label="Event filter">
      {[['all', 'All events'], ['commentary', 'Commentary']].map(([value, label]) => <button key={value} type="button" aria-pressed={filter === value} onClick={() => setFilter(value)}>{label}</button>)}
    </div>
    <ol ref={list} role="log" aria-live="polite" aria-relevant="additions">
      {rows.map(({ event, commentary }) => <li key={event.sequence} aria-current={event.sequence === currentSequence ? 'step' : undefined}
        role={onSeek ? 'button' : undefined} tabIndex={onSeek ? 0 : undefined} onClick={onSeek ? () => onSeek(Math.max(0, timelineEvents.findLastIndex(item => Number(item.sequence) <= Number(event.sequence)))) : undefined}
        onKeyDown={onSeek ? e => { if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); onSeek(Math.max(0, timelineEvents.findLastIndex(item => Number(item.sequence) <= Number(event.sequence)))) } } : undefined}
        className={`${event.type === 'NpcDecision' ? 'event-log-decision' : 'event-log-battle'} ${event.sequence === currentSequence ? 'event-log-current' : ''}`}>
        <p>{commentary ? <CommentarySentence event={commentary} /> : event.type === 'Commentary' && typeof event.message === 'string' ? <CommentarySentence event={event} /> : eventMessage(event)}</p>
        {commentary && <p className="event-log-facts">{eventMessage(event)}</p>}
        {safeText(event.justification) && <p className="event-log-why">Why: {safeText(event.justification)}</p>}
      </li>)}
    </ol>
  </section>
}

export default memo(EventLog)
