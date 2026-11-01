import { HubConnectionBuilder } from '@microsoft/signalr'
import { ApiError } from './client.js'

async function publicGet(path, { signal } = {}) {
  const response = await fetch(path, { credentials: 'omit', signal })
  const data = await response.json().catch(() => null)
  if (!response.ok) throw new ApiError(response.status, data?.message ?? 'Unable to load this game.')
  return data
}

export const getPublicGame = (id, options) => publicGet(`/api/public/games/${encodeURIComponent(id)}`, options)
export const getPublicGameEvents = (id, after = 0, limit = 200, options) =>
  publicGet(`/api/public/games/${encodeURIComponent(id)}/events?after=${after}&limit=${limit}`, options)

export const createGameConnection = () => new HubConnectionBuilder()
  .withUrl('/hubs/game')
  .withAutomaticReconnect()
  .build()
