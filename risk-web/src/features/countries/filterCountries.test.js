import assert from 'node:assert/strict'
import { test } from 'node:test'
import { getAllCountries, searchCountries } from '../../api/countries.js'
import { filterCountries } from './filterCountries.js'

const countries = Array.from({ length: 234 }, (_, index) => ({
  id: String(index), name: `Country ${index}`, population: index + 1000, rank: index + 1, soldiers: index % 9 + 1,
}))

test('country API supports search, filters, sorting and paging', async (t) => {
  let requested
  t.mock.method(globalThis, 'fetch', async (path, options) => {
    requested = new URL(path, 'http://localhost')
    assert.equal(options.credentials, 'same-origin')
    return Response.json({ items: [], total: 0, page: 2, pageSize: 8 })
  })
  await searchCountries({ search: 'South Africa', soldiers: 3, minPopulation: 100, maxPopulation: 100000000, sort: 'population', direction: 'desc', page: 2, pageSize: 8 })
  assert.equal(requested.pathname, '/api/countries')
  assert.equal(requested.searchParams.get('search'), 'South Africa')
  assert.equal(requested.searchParams.get('soldiers'), '3')
  assert.equal(requested.searchParams.get('minPopulation'), '100')
  assert.equal(requested.searchParams.get('maxPopulation'), '100000000')
  assert.equal(requested.searchParams.get('sort'), 'population')
  assert.equal(requested.searchParams.get('direction'), 'desc')
  assert.equal(requested.searchParams.get('page'), '2')
  assert.equal(requested.searchParams.get('pageSize'), '8')
})

test('local search, filtering, sorting and paging are immediate and make no requests', (t) => {
  t.mock.method(globalThis, 'fetch', () => { throw new Error('Filtering must not contact the API') })
  const sample = [
    { id: 'IN', name: 'India', rank: 1, population: 1476625576, soldiers: 9 },
    { id: 'ZA', name: 'South Africa', rank: 25, population: 64000000, soldiers: 3 },
    { id: 'VA', name: 'Holy See', rank: 234, population: 506, soldiers: 1 },
  ]
  assert.equal(filterCountries(sample, { search: ' south ' }).items[0].id, 'ZA')
  assert.equal(filterCountries(sample, { search: 'za' }).items[0].id, 'ZA')
  assert.equal(filterCountries(sample, { soldiers: '9' }).items[0].id, 'IN')
  assert.equal(filterCountries(sample, { minPopulation: '1000', maxPopulation: '100000000' }).items[0].id, 'ZA')
  assert.deepEqual(filterCountries(sample, { sort: 'population' }).items.map(c => c.id), ['VA', 'ZA', 'IN'])
  assert.deepEqual(filterCountries(sample, { sort: 'name', direction: 'desc' }).items.map(c => c.id), ['ZA', 'IN', 'VA'])
  assert.deepEqual(filterCountries(sample, { sort: 'soldiers', direction: 'desc' }).items.map(c => c.id), ['IN', 'ZA', 'VA'])
  assert.equal(filterCountries(sample, { page: 2, pageSize: 2 }).items[0].id, 'VA')
  const filtered = filterCountries(sample, { search: 'za', page: 20, pageSize: 2 })
  assert.equal(filtered.page, 1)
  assert.equal(filtered.total, 1)
  const empty = filterCountries(sample, { search: 'no match' })
  assert.deepEqual(empty.items, [])
  assert.equal(empty.totalPages, 1)
  assert.equal(sample[0].id, 'IN', 'sorting must not mutate the source list')
})

test('country roster includes all 234 countries across three API pages', async (t) => {
  const pages = []
  t.mock.method(globalThis, 'fetch', async path => {
    const query = new URL(path, 'http://localhost').searchParams
    assert.equal(query.get('pageSize'), '100')
    assert.equal(query.get('sort'), 'rank')
    assert.equal(query.has('search'), false)
    const page = Number(query.get('page'))
    pages.push(page)
    return Response.json({ items: countries.slice((page - 1) * 100, page * 100), total: 234, page, pageSize: 100 })
  })
  const result = await getAllCountries()
  assert.deepEqual(pages.sort(), [1, 2, 3])
  assert.deepEqual(result, countries)
})

test('country searches preserve cancellation and authentication errors', async (t) => {
  const controller = new AbortController()
  controller.abort()
  t.mock.method(globalThis, 'fetch', async (_path, options) => {
    options.signal?.throwIfAborted()
    return Response.json({}, { status: 401 })
  })
  await assert.rejects(searchCountries({}, { signal: controller.signal }), { name: 'AbortError' })
  await assert.rejects(searchCountries(), error => error.status === 401)
})
