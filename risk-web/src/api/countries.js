import { api } from './client.js'

export function searchCountries(filters = {}, options) {
  const params = new URLSearchParams()
  for (const [key, value] of Object.entries(filters)) {
    if (value !== '' && value !== undefined && value !== null) params.set(key, value)
  }
  return api.get(`/api/countries?${params}`, options)
}

// The endpoint caps pages at 100, so fetch every page before drawing opponents.
// Picker filters deliberately do not apply to this complete country pool.
export async function getAllCountries(options) {
  const query = { sort: 'rank', direction: 'asc', pageSize: 100 }
  const first = await searchCountries({ ...query, page: 1 }, options)
  const remaining = await Promise.all(Array.from(
    { length: Math.max(0, Math.ceil(first.total / first.pageSize) - 1) },
    (_, index) => searchCountries({ ...query, page: index + 2 }, options),
  ))
  const countries = [first, ...remaining].flatMap(page => page.items)
  if (countries.length !== first.total || new Set(countries.map(country => country.id)).size !== countries.length) {
    throw new Error('The country list changed while loading. Please try again.')
  }
  return countries
}
