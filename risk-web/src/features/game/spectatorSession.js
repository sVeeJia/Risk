import { loadHistory, mergeEvents, newerGame } from './spectatorEvents.js'

export const isGameNotFound = error => error?.status === 404 || /Game not found\./i.test(error?.message ?? '')

// Dependencies are injected so subscription races and cleanup can be tested without a server.
export async function spectateSession({ id, signal, getGame, getEvents, createConnection, createQueue, onInitial, onStatus, onError }) {
  let connection
  let buffering = true
  let buffered = []
  let epoch = 0
  let stopped = false
  const stop = () => {
    if (!connection || stopped) return
    stopped = true
    connection.invoke('StopWatching', id).catch(() => {})
    return connection.stop().catch(() => {})
  }
  const active = () => !signal.aborted
  const page = (after, limit) => getEvents(id, after, limit, { signal })
  const report = error => { if (active()) { onStatus('Disconnected'); onError(error) } }
  try {
    const [game, history] = await Promise.all([getGame(id, { signal }), loadHistory(page)])
    newerGame(null, game)
    if (!active()) return
    onInitial(game, history)
    const queue = createQueue(game, mergeEvents(history).lastSeenSequence)
    connection = createConnection()
    signal.addEventListener('abort', stop, { once: true })
    const receive = update => {
      if (!active()) return
      try {
        if (buffering) buffered.push(update)
        else queue.enqueue(update)
      } catch (error) { report(error) }
    }
    async function catchUp() {
      const attempt = ++epoch
      buffering = true
      await connection.invoke('Watch', id)
      if (!active() || attempt !== epoch) return
      const events = await loadHistory(page, queue.lastSeenSequence)
      const snapshot = await getGame(id, { signal })
      if (!active() || attempt !== epoch) return
      const next = buffered.reduce((latest, update) => newerGame(latest, update.game), newerGame(game, snapshot))
      const combined = [...events, ...buffered.flatMap(update => update.events ?? [])]
      queue.enqueue({ game: next, events: mergeEvents(combined, queue.lastSeenSequence).events })
      buffered = []
      buffering = false
      onStatus('Live')
    }
    connection.on('GameUpdated', receive)
    connection.onreconnecting(() => { if (active()) { ++epoch; buffering = true; onStatus('Reconnecting…') } })
    connection.onreconnected(() => { if (active()) catchUp().catch(report) })
    connection.onclose(() => { if (active()) onStatus('Disconnected') })
    onStatus('Reconnecting…')
    await connection.start()
    if (!active()) return
    await catchUp()
    if (active()) await new Promise(resolve => signal.addEventListener('abort', resolve, { once: true }))
  } catch (error) { report(error) }
  finally {
    signal.removeEventListener('abort', stop)
    await stop()
  }
}
