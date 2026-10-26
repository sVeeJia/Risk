// Match the API's search/filter behavior without a round trip for each keystroke.
export function filterCountries(countries, filters = {}) {
  const term = (filters.search ?? '').trim().toLowerCase()
  const hasValue = value => value !== '' && value !== undefined && value !== null
  const items = countries.filter(country =>
    (!term || country.name.toLowerCase().includes(term) || country.id.toLowerCase() === term) &&
    (!hasValue(filters.soldiers) || country.soldiers === Number(filters.soldiers)) &&
    (!hasValue(filters.minPopulation) || country.population >= Number(filters.minPopulation)) &&
    (!hasValue(filters.maxPopulation) || country.population <= Number(filters.maxPopulation)),
  )
  const field = ['name', 'population', 'soldiers'].includes(filters.sort) ? filters.sort : 'rank'
  const direction = filters.direction === 'desc' ? -1 : 1
  items.sort((a, b) => {
    const left = field === 'name' ? a.name.toLowerCase() : a[field]
    const right = field === 'name' ? b.name.toLowerCase() : b[field]
    return (left < right ? -1 : left > right ? 1 : 0) * direction
  })
  const pageSize = filters.pageSize ?? 12
  const totalPages = Math.max(1, Math.ceil(items.length / pageSize))
  const page = Math.min(totalPages, Math.max(1, filters.page ?? 1))
  return { items: items.slice((page - 1) * pageSize, page * pageSize), total: items.length, page, totalPages }
}
