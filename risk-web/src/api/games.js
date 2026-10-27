import { api } from './client.js'

// Creates a game on the server. The server draws the other nine countries.
export const startGame = (startingCountryId, options) => api.post('/api/games', { startingCountryId }, options)

export const getGame = id => api.get(`/api/games/${id}`)

export const listGames = () => api.get('/api/games')
