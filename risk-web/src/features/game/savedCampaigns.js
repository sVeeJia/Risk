const timestamp = game => {
  const value = Date.parse(game.createdAt)
  return Number.isFinite(value) ? value : -Infinity
}

export const newestCampaigns = games => [...games].sort((a, b) => {
  const left = timestamp(a), right = timestamp(b)
  return left === right ? 0 : left > right ? -1 : 1
})

export function campaignRow(game, locale) {
  const human = game.players?.find(player => player.isHuman)
  const current = game.players?.find(player => player.id === game.currentPlayerId)
  const finished = game.phase === 'Finished'
  const result = finished && human ? game.winnerPlayerId === human.id ? 'Won'
    : game.winnerPlayerId || human.isEliminated ? 'Lost' : null : null
  return {
    id: game.id,
    date: Number.isFinite(timestamp(game)) ? new Intl.DateTimeFormat(locale, { year: 'numeric', month: 'short', day: 'numeric' }).format(new Date(game.createdAt)) : 'Unknown date',
    round: game.round,
    turn: finished ? `Finished${result ? ` · ${result}` : ''}` : current ? current.isHuman ? 'Your turn' : `${current.name} to play` : 'Turn unavailable',
    countries: human?.countriesOwned ?? null,
    countryLabel: human?.countriesOwned == null ? 'Countries unavailable' : `${human.countriesOwned} ${human.countriesOwned === 1 ? 'country' : 'countries'} owned`,
    action: finished ? 'Review' : 'Resume',
    href: `/play/${encodeURIComponent(game.id)}`,
  }
}

export const campaignListState = ({ loading, error, games }) => loading ? 'loading' : error ? 'error' : games.length ? 'ready' : 'empty'

export async function loadSavedCampaigns(list, get, options) {
  const games = await list(options)
  if (!Array.isArray(games)) throw new Error('The server returned an unexpected campaign list.')
  // A summary-only list can be hydrated through the already-existing game GET.
  const full = await Promise.all(games.map(game => Array.isArray(game.players) ? game : get(game.id, options)))
  return newestCampaigns(full)
}
