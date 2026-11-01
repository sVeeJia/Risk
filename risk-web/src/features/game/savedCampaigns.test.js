import test from 'node:test'
import assert from 'node:assert/strict'
import { campaignListState, campaignRow, loadSavedCampaigns, newestCampaigns } from './savedCampaigns.js'

const campaign = overrides => ({ id: 'old', createdAt: '2026-01-15T12:00:00Z', round: 4, phase: 'Action', currentPlayerId: 'npc',
  players: [{ id: 'human', name: 'Alice', isHuman: true, countriesOwned: 3 }, { id: 'npc', name: 'China', isHuman: false }], ...overrides })

test('saved campaigns sort newest first, leave invalid dates last, and do not mutate the list', () => {
  const list = [campaign(), campaign({ id: 'unknown', createdAt: null }), campaign({ id: 'new', createdAt: '2026-11-01T12:00:00Z' })]
  assert.deepEqual(newestCampaigns(list).map(game => game.id), ['new', 'old', 'unknown'])
  assert.deepEqual(list.map(game => game.id), ['old', 'unknown', 'new'])
  assert.deepEqual(newestCampaigns([]), [])
})

test('row formatting shows the server round, current player and human countries owned', () => {
  const row = campaignRow(campaign({ id: 'a/b' }), 'en-US')
  assert.equal(row.round, 4)
  assert.equal(row.turn, 'China to play')
  assert.equal(row.countries, 3)
  assert.match(row.date, /2026/)
  assert.equal(row.href, '/play/a%2Fb')
  assert.equal(campaignRow(campaign({ currentPlayerId: 'human' })).turn, 'Your turn')
})

test('finished rows show known Won/Lost results without showing a current turn', () => {
  assert.equal(campaignRow(campaign({ phase: 'Finished', winnerPlayerId: 'human' })).turn, 'Finished · Won')
  assert.equal(campaignRow(campaign({ phase: 'Finished', winnerPlayerId: 'npc' })).turn, 'Finished · Lost')
  assert.equal(campaignRow(campaign({ phase: 'Finished', winnerPlayerId: null, players: [{ id: 'human', isHuman: true, countriesOwned: 0, isEliminated: true }] })).turn, 'Finished · Lost')
  assert.equal(campaignRow(campaign({ phase: 'Finished', winnerPlayerId: null })).turn, 'Finished')
})

test('missing display fields are explicit instead of invented', () => {
  const row = campaignRow(campaign({ createdAt: 'invalid', players: [] }))
  assert.equal(row.date, 'Unknown date')
  assert.equal(row.turn, 'Turn unavailable')
  assert.equal(row.countries, null)
  assert.equal(row.countryLabel, 'Countries unavailable')
})

test('Resume and Review always use the owner route, never the watch route', () => {
  for (const phase of ['Reinforcing', 'Action', 'Attacking', 'ConquestTransfer', 'Finished']) {
    const row = campaignRow(campaign({ id: 'a/b', phase }))
    assert.equal(row.action, phase === 'Finished' ? 'Review' : 'Resume')
    assert.equal(row.href, '/play/a%2Fb')
  }
})

test('country counts use singular only for one country', () => {
  for (const count of [0, 1, 2]) {
    const row = campaignRow(campaign({ players: [{ id: 'human', isHuman: true, countriesOwned: count }] }))
    assert.equal(row.countryLabel, `${count} ${count === 1 ? 'country' : 'countries'} owned`)
  }
})

test('list states distinguish loading, errors, empty and ready', () => {
  assert.equal(campaignListState({ loading: true, error: '', games: [] }), 'loading')
  assert.equal(campaignListState({ loading: false, error: 'offline', games: [] }), 'error')
  assert.equal(campaignListState({ loading: false, error: '', games: [] }), 'empty')
  assert.equal(campaignListState({ loading: false, error: '', games: [campaign()] }), 'ready')
})

test('saved games reuse the list endpoint, hydrate summaries with the existing GET, and sort', async () => {
  const options = { signal: new AbortController().signal }, calls = []
  const full = campaign()
  const games = await loadSavedCampaigns(async received => { assert.equal(received, options); return [full, { id: 'new' }] }, async (id, received) => {
    calls.push(id); assert.equal(received, options)
    return campaign({ id, createdAt: '2026-11-01T12:00:00Z' })
  }, options)
  assert.deepEqual(calls, ['new'])
  assert.deepEqual(games.map(game => game.id), ['new', 'old'])
  assert.equal(games[1], full)
})

test('empty lists do not fetch games; malformed lists and failures surface to the panel', async () => {
  assert.deepEqual(await loadSavedCampaigns(async () => [], assert.fail), [])
  await assert.rejects(loadSavedCampaigns(async () => ({ items: [] }), assert.fail), /unexpected campaign list/)
  await assert.rejects(loadSavedCampaigns(async () => { throw Error('offline') }, assert.fail), /offline/)
  await assert.rejects(loadSavedCampaigns(async () => [{ id: 'g' }], async () => { throw Error('Game not found') }), /Game not found/)
})
