export function mergeEvents(events, lastSeenSequence = 0) {
  const unique = new Map()
  for (const event of events ?? []) {
    if (Number.isSafeInteger(event?.sequence) && event.sequence > lastSeenSequence && !unique.has(event.sequence)) {
      unique.set(event.sequence, event)
    }
  }
  const fresh = [...unique.values()].sort((a, b) => a.sequence - b.sequence)
  return { events: fresh, lastSeenSequence: fresh.at(-1)?.sequence ?? lastSeenSequence }
}

export function newerGame(current, next) {
  if (!next) return current
  if (!Number.isSafeInteger(next.version)) throw new Error('Live viewing requires a numeric game.version from the server.')
  return !current || next.version > current.version ? next : current
}

export async function loadHistory(getPage, after = 0, limit = 200) {
  const history = []
  let cursor = after
  while (true) {
    const page = await getPage(cursor, limit)
    if (!Array.isArray(page)) throw new Error('Unexpected event history response.')
    const merged = mergeEvents(page, cursor)
    history.push(...merged.events)
    if (page.length < limit) return history
    if (merged.lastSeenSequence === cursor) throw new Error('Event history did not advance.')
    cursor = merged.lastSeenSequence
  }
}

export const shareUrl = (origin, gameId) => `${origin.replace(/\/$/, '')}/watch/${encodeURIComponent(gameId)}`
