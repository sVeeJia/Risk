import { useEffect, useMemo, useState } from 'react'
import { ArrowDownWideNarrow, ArrowUpWideNarrow, Check, ChevronLeft, ChevronRight, Search, Swords, X } from 'lucide-react'
import { getAllCountries } from '../../api/countries.js'
import { filterCountries } from './filterCountries.js'

const PAGE_SIZE = 12

export default function CountryPicker({ selected, selectedId, onSelect, disabled = false }) {
  const [filters, setFilters] = useState({ search: '', soldiers: '', sort: 'rank', direction: 'asc', page: 1 })
  const [countries, setCountries] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [attempt, setAttempt] = useState(0)

  useEffect(() => {
    const controller = new AbortController()
    getAllCountries({ signal: controller.signal })
      .then(items => { if (!controller.signal.aborted) setCountries(items) })
      .catch(err => { if (!controller.signal.aborted) setError(err.status === 401 ? 'Your session has expired. Sign in again to choose a country.' : err.message || 'Could not load countries.') })
      .finally(() => { if (!controller.signal.aborted) setLoading(false) })
    return () => controller.abort()
  }, [attempt])

  const data = useMemo(() => filterCountries(countries, { ...filters, pageSize: PAGE_SIZE }), [countries, filters])
  const activeId = selected?.id ?? selectedId
  const activeCountry = countries.find(country => country.id === activeId)
  const isDisabled = disabled || loading
  function update(key, value) { setFilters(current => ({ ...current, [key]: value, page: 1 })) }

  return <section className="country-picker" aria-label="Choose your starting country">
    <div className="picker-heading"><div><span className="picker-kicker">CHOOSE YOUR ALLEGIANCE</span><h3>Starting country</h3></div><span className="picker-total">{loading ? 'Loading...' : `${countries.length} countries`}</span></div>
    <fieldset className="picker-controls" disabled={isDisabled}>
      <legend className="sr-only">Find and filter countries</legend>
      <label className="sr-only" htmlFor="country-search">Search by country name or code</label>
      <div className="picker-search"><Search size={18} aria-hidden="true" /><input id="country-search" type="search" autoComplete="off" maxLength={50} placeholder="Search country name or code..." value={filters.search} onChange={event => update('search', event.target.value)} />{filters.search && <button type="button" aria-label="Clear search" onClick={() => update('search', '')}><X size={16} /></button>}</div>
      <div className="picker-strength-heading"><span><Swords size={14} aria-hidden="true" /> Starting soldiers</span><small>{filters.soldiers ? `Exactly ${filters.soldiers} soldiers` : 'Any army strength'}</small></div>
      <div className="picker-soldier-filters" role="group" aria-label="Filter by starting soldiers"><button type="button" aria-pressed={filters.soldiers === ''} onClick={() => update('soldiers', '')}>All</button>{[1, 2, 3, 4, 5, 6, 7, 8, 9].map(soldiers => <button type="button" key={soldiers} aria-label={`${soldiers} starting ${soldiers === 1 ? 'soldier' : 'soldiers'}`} aria-pressed={Number(filters.soldiers) === soldiers} onClick={() => update('soldiers', String(soldiers))}>{soldiers}</button>)}</div>
      <div className="picker-toolbar"><label htmlFor="country-sort">Sort by</label><select id="country-sort" value={filters.sort} onChange={event => update('sort', event.target.value)}><option value="rank">World rank</option><option value="name">Country name</option><option value="population">Population</option><option value="soldiers">Soldiers</option></select><button type="button" className="picker-direction" onClick={() => update('direction', filters.direction === 'asc' ? 'desc' : 'asc')} aria-label={`Sort ${filters.direction === 'asc' ? 'descending' : 'ascending'}`}>{filters.direction === 'asc' ? <ArrowUpWideNarrow size={15} /> : <ArrowDownWideNarrow size={15} />}<span>{filters.direction === 'asc' ? 'Ascending' : 'Descending'}</span></button></div>
    </fieldset>
    <div className="picker-results" aria-busy={loading}>
      {loading ? <div className="picker-empty" role="status">Loading the country roster...</div> : error ? <div className="picker-empty country-error" role="alert"><p>{error}</p><button type="button" onClick={() => { setError(''); setLoading(true); setAttempt(value => value + 1) }}>Try again</button></div> : data.items.length === 0 ? <div className="picker-empty"><Search size={24} aria-hidden="true" /><strong>No matching countries</strong><p>Try a different name, code, or soldier strength.</p><button type="button" disabled={disabled} onClick={() => setFilters(current => ({ ...current, search: '', soldiers: '', page: 1 }))}>Clear filters</button></div> : <ul className="picker-country-grid">{data.items.map(country => <li key={country.id}><button type="button" className="picker-country" aria-pressed={country.id === activeId} disabled={disabled} onClick={() => onSelect(country)}><span className="picker-country-top"><span className="picker-country-code">{country.id}</span><span className="picker-country-army" aria-label={`${country.soldiers} soldiers`}><Swords size={12} aria-hidden="true" />{country.soldiers}</span></span><span className="picker-country-name" title={country.name}>{country.name}</span><span className="picker-country-bottom"><span>#{country.rank} · {country.population.toLocaleString()}</span>{country.id === activeId && <Check size={13} aria-label="Selected" />}</span></button></li>)}</ul>}
    </div>
    <div className="picker-pagination"><span role="status">{loading ? 'Loading countries' : error ? 'Country list unavailable' : `${data.total} ${data.total === 1 ? 'match' : 'matches'}`}</span><div><button type="button" aria-label="Previous page" disabled={isDisabled || Boolean(error) || data.page <= 1} onClick={() => setFilters(current => ({ ...current, page: data.page - 1 }))}><ChevronLeft size={16} /></button><span>{data.page} / {data.totalPages}</span><button type="button" aria-label="Next page" disabled={isDisabled || Boolean(error) || data.page >= data.totalPages} onClick={() => setFilters(current => ({ ...current, page: data.page + 1 }))}><ChevronRight size={16} /></button></div></div>
    <div className="picker-selection" role="status">{activeCountry ? <><Check size={14} aria-hidden="true" /><strong title={activeCountry.name}>{activeCountry.name}</strong><span>{activeCountry.soldiers} soldiers</span></> : <span>Select a country to claim Territory 1.</span>}</div>
  </section>
}
