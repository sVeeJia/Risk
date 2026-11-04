import { worldRegions } from './worldRegions.js'

function Badge({ defending }) {
  return <g className="attack-badge">
    <circle r="15" />
    <svg x="-13" y="-13" width="26" height="26" viewBox="0 0 24 24" aria-hidden="true">
      {defending ? <path d="M12 2 21 6v6c0 5-9 10-9 10S3 17 3 12V6z M12 5v13" /> : <path d="m3 2 13 13-2 2L2 4V2z m18 0L8 15l2 2L22 4V2z M13 18l5-5 M6 13l5 5 M16 16l5 5 M8 16l-5 5" />}
    </svg>
  </g>
}

export default function AttackMarkers({ markers, hit, conquered, countryAssignments, fastForward = false }) {
  const regionFor = id => worldRegions.find(region => region.id === countryAssignments.find(item => item.country.id === id)?.territoryId)
  return <g className={`attack-markers ${fastForward ? 'battle-visuals-fast' : ''}`} aria-hidden="true">
    {markers && [[markers.attackerId, false], [markers.defenderId, true]].map(([id, defending]) => {
      const region = regionFor(id)
      if (!region) return null
      return <g key={id} className={defending ? 'attack-defender' : 'attack-attacker'}>
        {region.shapes.map((points, index) => <polygon key={index} points={points} className="attack-glow" />)}
        <g transform={`translate(${region.at[0]} ${region.at[1] - 28})`}><Badge defending={defending} /></g>
      </g>
    })}
    {hit && [[hit.attackerId, hit.result.attackerLosses], [hit.defenderId, hit.result.defenderLosses]].map(([id, losses]) => {
      const region = regionFor(id)
      if (!region) return null
      return <g key={`${hit.sequence}:${id}`} className="attack-hit">
        {region.shapes.map((points, index) => <polygon key={index} points={points} className={`attack-hit-shape ${losses ? 'attack-hit-loss' : 'attack-hit-safe'}`} />)}
        {losses > 0 && <text className="attack-loss-label" x={region.at[0]} y={region.at[1] + 18}>-{losses}</text>}
      </g>
    })}
    {conquered && (() => {
      const region = regionFor(conquered.countryId)
      if (!region) return null
      return <g key={conquered.sequence} className="attack-conquered">
        {region.shapes.map((points, index) => <polygon key={index} points={points} className="attack-conquered-flash" />)}
        <g transform={`translate(${region.at[0]} ${region.at[1] - 48})`}><rect x="-55" y="-13" width="110" height="24" rx="5" /><text textAnchor="middle" y="3">Conquered!</text></g>
      </g>
    })()}
  </g>
}
