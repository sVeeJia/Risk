import { useEffect, useRef, useState } from 'react'
import { Swords } from 'lucide-react'
import { getAllCountries } from '../../api/countries.js'
import { assignCountries } from './assignCountries.js'
import { worldRegions } from '../map/worldRegions.js'
import CountryPicker from '../countries/CountryPicker.jsx'
import WorldMap from '../map/WorldMap.jsx'

export default function GameSetup() {
  const [country, setCountry] = useState(null)
  const [assignments, setAssignments] = useState(null)
  const [preparing, setPreparing] = useState(false)
  const [error, setError] = useState('')
  const request = useRef(null)
  useEffect(() => () => request.current?.abort(), [])

  function selectCountry(selected) {
    setCountry(selected)
    setAssignments(null)
    setError('')
  }
  async function prepareBoard() {
    if (!country || preparing) return
    const controller = new AbortController()
    request.current?.abort()
    request.current = controller
    setPreparing(true)
    setError('')
    try {
      const countries = await getAllCountries({ signal: controller.signal })
      if (!controller.signal.aborted) setAssignments(assignCountries(countries, country.id, worldRegions))
    } catch (err) {
      if (!controller.signal.aborted) setError(err.message)
    } finally {
      if (!controller.signal.aborted) setPreparing(false)
    }
  }
  
  return <div className="game-setup">
    <p>Search for your starting country. It will occupy Territory 1 on the fictional board. Nine other countries are drawn randomly from the full country list, with no duplicates.</p>
    <div className="setup-workspace">
      <div className="setup-picker-column">
        <CountryPicker selected={country} onSelect={selectCountry} disabled={preparing} />
        <div className="setup-actions"><button type="button" className="game-button game-button-primary setup-draw" disabled={!country || preparing} onClick={prepareBoard}>{preparing ? 'Preparing your board...' : assignments ? 'Draw nine other countries again' : 'Prepare the 10-territory board'}</button>
          <p className="country-error setup-error-slot" role="alert">{error}</p>
        </div>
      </div>
      <aside className="setup-board-column" aria-label="Board preview">
        <p className="eyebrow">YOUR WORLD / 10 TERRITORIES</p>
        <WorldMap startId={assignments ? worldRegions[0].id : undefined} countryAssignments={assignments ?? []} />
        <div className="setup-assignment-results" aria-live="polite">{assignments ? <>
          <p><strong>Your country: {assignments[0].country.name}</strong><br />Territory 1 is yours. The other nine countries fill Territories 2–10.</p>
          <ol className="assigned-countries">{assignments.map((assignment, i) => <li key={assignment.territoryId} title={`${worldRegions[i].name} · Population ${assignment.country.population.toLocaleString()}`}><span className="assignment-color" style={{ backgroundColor: worldRegions[i].color }} aria-hidden="true" /><strong className="assignment-name" title={assignment.country.name}>{assignment.country.name}{i === 0 ? ' (you)' : ''}</strong><span className="assignment-soldiers" aria-label={`${assignment.country.soldiers} soldiers`}><Swords size={14} aria-hidden="true" />{assignment.country.soldiers}</span><span className="sr-only">{worldRegions[i].name} · Population {assignment.country.population.toLocaleString()}</span></li>)}</ol>
        </> : <p className="region-description">Choose your country and prepare the board to see all ten countries here.</p>}</div>
        <small>This previews the country selection and board. Saving a game and combat are still under development.</small>
      </aside>
    </div>
  </div>
}
