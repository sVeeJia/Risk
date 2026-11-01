import { mergeEvents, newerGame } from './spectatorEvents.js'
import { replayEvents } from './npcPlayback.js'

// One ordered presentation queue for both human actions and full NPC turns.
export function createSpectatorQueue({ game, lastSeenSequence, signal, pause, onEvent, onGame, onBusy, onError }) {
  const pending = []
  let shown = game
  let cursor = lastSeenSequence
  let running = false
  let arrow = null
  async function drain() {
    if (running || signal.aborted) return
    running = true
    onBusy(true)
    try {
      while (pending.length && !signal.aborted) {
        const update = pending.shift()
        arrow = await replayEvents({ events: update.events, arrow, pause,
          isActive: () => !signal.aborted, onEvent })
        if (signal.aborted) return
        const next = newerGame(shown, update.game)
        if (next !== shown) {
          const turnChanged = next.currentPlayerId !== shown.currentPlayerId || next.round !== shown.round || next.phase === 'Finished'
          shown = next
          onGame(next, turnChanged)
          if (turnChanged) { arrow = null; await pause('turnPause') }
        }
      }
    } catch (error) { if (!signal.aborted) onError(error) }
    finally { running = false; if (!signal.aborted) onBusy(false) }
  }
  return {
    get lastSeenSequence() { return cursor },
    enqueue(update) {
      if (signal.aborted) return
      newerGame(shown, update.game) // Reject an unversioned snapshot before accepting its events.
      const fresh = mergeEvents(update.events, cursor)
      cursor = fresh.lastSeenSequence
      pending.push({ game: update.game, events: fresh.events })
      return drain()
    },
  }
}
