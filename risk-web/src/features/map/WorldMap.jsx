import { useId } from 'react'
import { worldRegions, seaRoutes } from './worldRegions.js'
import Arrows from './Arrows.jsx'
import { ownerSlot } from '../game/gameActions.js'
import AttackMarkers from './AttackMarkers.jsx'

export default function WorldMap({ selectedIds, startId, countryAssignments = [], showSoldiers = false, onSelect, selectedFrom, selectedTo, territories = [], players = [], disabled = false, arrow, reinforcement, markers, battleHit, conquered, fastForward = false, liveCounts = {} }) {
    const gridId = useId()
  
  return <svg className="world-map fictional-map" viewBox={onSelect ? '20 4 928 482' : '0 0 960 500'} preserveAspectRatio="xMidYMid meet" role={onSelect ? 'group' : 'img'} aria-label="Fictional world of ten irregular territories: three central territories and seven surrounding islands, linked by dotted sea routes">
    <defs><pattern id={gridId} width="40" height="40" patternUnits="userSpaceOnUse"><path d="M40 0H0V40" fill="none" stroke="#779a9e" strokeOpacity=".08" /></pattern></defs>
    <rect width="960" height="500" fill={`url(#${gridId})`} />
    <ellipse cx="480" cy="250" rx="440" ry="225" className="map-orbit" />
    <g className="map-route">{seaRoutes.map(route => <path key={route.id} d={route.d}><title>{`${worldRegions.find(region => region.id === route.from).name} to ${worldRegions.find(region => region.id === route.to).name}`}</title></path>)}</g>
    {worldRegions.map(region => {
      const assignment = countryAssignments.find(item => item.territoryId === region.id)
      const countryId = assignment?.country.id
      const territory = territories.find(item => item.countryId === countryId)
      const ownerIndex = players.findIndex(player => player.id === territory?.ownerPlayerId)
      const colorSlot = territory ? ownerSlot({ territories, players }, territory) : assignment?.colorSlot
      const color = worldRegions[colorSlot - 1]?.color ?? region.color
      const soldiers = liveCounts[countryId] ?? assignment?.country.soldiers ?? 0
      const select = () => { if (!disabled && countryId) onSelect?.(countryId) }
      return <g key={region.id} role={onSelect ? 'button' : undefined} tabIndex={onSelect && countryId && !disabled ? 0 : undefined}
        aria-label={onSelect ? `${assignment?.country.name ?? region.name}, ${soldiers} soldiers, owner ${players[ownerIndex]?.name ?? 'none'}` : undefined}
        aria-disabled={onSelect ? disabled : undefined} aria-pressed={onSelect ? selectedFrom === countryId || selectedTo === countryId : undefined}
        onClick={onSelect ? select : undefined} onKeyDown={onSelect ? event => { if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); select() } } : undefined}
        className={`map-region ${onSelect ? 'map-selectable' : ''} owner-${colorSlot - 1} ${selectedFrom === countryId && countryId ? 'map-selected-from' : ''} ${selectedTo === countryId && countryId ? 'map-selected-to' : ''} ${selectedIds && !selectedIds.includes(region.id) ? 'region-inactive' : ''} ${startId === region.id ? 'region-start' : ''}`}>
      <title>{countryAssignments.find(item => item.territoryId === region.id)?.country.name ?? region.includes}</title>
      {region.shapes.map((points, i) => <polygon key={i} points={points} className="continent" fill={color} />)}
      <text className="region-label" x={region.at[0]} y={region.at[1]} textAnchor="middle">{(() => {
        const name = countryAssignments.find(item => item.territoryId === region.id)?.country.name ?? region.name
        return name.length > 18 ? `${name.slice(0, 17)}…` : name
      })()}</text>
      <text className="fictional-region-code" x={region.at[0]} y={region.at[1] + 18} textAnchor="middle">{showSoldiers ? `${soldiers} soldiers` : countryAssignments.length ? region.name : `${region.id} ${region.group === 'center' ? '/ CENTRAL' : '/ OUTER'}`}</text>
    </g>})}
    <g className="map-labels"><text x="435" y="21" className="ocean-label">THE UNCHARTED WORLD</text><text x="64" y="260" className="ocean-label" transform="rotate(-90 64 260)">WESTERN SEA</text><text x="921" y="225" className="ocean-label" transform="rotate(90 921 225)">EASTERN SEA</text></g>
    <Arrows arrow={arrow} reinforcement={reinforcement} countryAssignments={countryAssignments} />
    <AttackMarkers markers={markers} hit={battleHit} conquered={conquered} fastForward={fastForward} countryAssignments={countryAssignments} />
  </svg>
}
