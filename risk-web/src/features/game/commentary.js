export function uniqueEvents(events = []) {
  const ids = new Set(), sequences = new Set()
  return events.filter(event => {
    if (!event || (event.id != null && ids.has(event.id)) || (event.sequence != null && sequences.has(String(event.sequence)))) return false
    if (event.id != null) ids.add(event.id)
    if (event.sequence != null) sequences.add(String(event.sequence))
    return true
  }).sort((a, b) => Number(a.sequence) - Number(b.sequence))
}

export function commentaryFor(events, sequence) {
  if (sequence == null) return null
  return uniqueEvents(events).find(event => event.type === 'Commentary' &&
    typeof event.data?.forSequence === 'string' && /^\d+$/.test(event.data.forSequence) &&
    event.data.forSequence === String(sequence) && typeof event.message === 'string' && event.message.trim()) ?? null
}

export const isAiCommentary = event => event?.data?.source === 'llm'

export function logRows(events, filter = 'all') {
  const unique = uniqueEvents(events)
  const rolls = new Set(unique.filter(event => event.type === 'BattleRound').map(event => String(event.sequence)))
  return unique.flatMap(event => {
    const commentary = event.type === 'BattleRound' ? commentaryFor(unique, event.sequence) : null
    if (event.type === 'Commentary' && rolls.has(event.data?.forSequence) && commentaryFor(unique, event.data.forSequence)) return []
    if (filter === 'commentary' && !commentary) return []
    return [{ event, commentary }]
  })
}
