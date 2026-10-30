import { api } from './client.js'

// Creates a game on the server. The server draws the other nine countries.
export const startGame = (startingCountryId, options) => api.post('/api/games', { startingCountryId }, options)

export const getGame = (id, options) => api.get(`/api/games/${encodeURIComponent(id)}`, options)

export const listGames = () => api.get('/api/games')

const gamePath = id => `/api/games/${encodeURIComponent(id)}`
export const attack = (id, body, options) => api.post(`${gamePath(id)}/attack`, body, options)
export const rollAttack = (id, options) => api.post(`${gamePath(id)}/attack/roll`, undefined, options)
export const stopAttack = (id, options) => api.post(`${gamePath(id)}/attack/stop`, undefined, options)
export const transferConquest = (id, body, options) => api.post(`${gamePath(id)}/attack/transfer`, body, options)
export const placeReinforcements = (id, body, options) => api.post(`${gamePath(id)}/reinforcements`, body, options)
export const transfer = (id, body, options) => api.post(`${gamePath(id)}/transfer`, body, options)
export const endTurn = (id, options) => api.post(`${gamePath(id)}/end-turn`, undefined, options)
export const playNpcTurn = (id, options) => api.post(`${gamePath(id)}/npc-turn`, undefined, options)
export async function getGameEvents(id, { after = 0, limit = 200, signal } = {}) {
  const events = await api.get(`${gamePath(id)}/events?${new URLSearchParams({ after, limit })}`, { signal })
  if (!Array.isArray(events) || events.some(event => !event || !Number.isInteger(event.sequence) || typeof event.message !== 'string')) {
    throw new TypeError('The server returned an unexpected event history.')
  }
  return events
}

// History is optional: a failed or malformed page must not block the game.
export async function loadGameEvents(id, { signal } = {}) {
  const history = []
  let after = 0
  try {
    while (!signal?.aborted) {
      const events = await getGameEvents(id, { after, limit: 200, signal })
      if (events.some(event => event.sequence <= after)) return []
      history.push(...events)
      if (events.length < 200) return history
      after = Math.max(...events.map(event => event.sequence))
    }
  } catch {
    return []
  }
  return []
}
