import { useId } from 'react'
import { curveControl, curvedPath } from './arrows.js'
import { worldRegions } from './worldRegions.js'

export default function Arrows({ arrow, reinforcement, countryAssignments }) {
  const marker = useId()
  const point = countryId => {
    const assignment = countryAssignments.find(item => item.country.id === countryId)
    const region = worldRegions.find(item => item.id === assignment?.territoryId)
    return region ? { x: region.at[0], y: region.at[1] + 18 } : null
  }
  const from = point(arrow?.from)
  const to = point(arrow?.to)
  const pulse = point(reinforcement?.data?.countryId)
  const control = from && to ? curveControl(from, to) : null
  return <g className="map-arrows" aria-hidden="true">
    <defs><marker id={marker} viewBox="0 0 10 10" refX="9" refY="5" markerWidth="5" markerHeight="5" orient="auto-start-reverse">
      <path d="M 0 0 L 10 5 L 0 10 z" fill="context-stroke" />
    </marker></defs>
    {from && to && <g key={arrow.sequence ?? `${arrow.kind}:${arrow.from}:${arrow.to}`} className={`map-arrow map-arrow-${arrow.kind} ${arrow.preview ? 'map-arrow-preview' : ''} ${arrow.transient ? 'map-arrow-transient' : ''}`}>
      <path d={curvedPath(from, to)} markerEnd={`url(#${marker})`} />
      {arrow.kind === 'move' && <text x={(from.x + 2 * control.x + to.x) / 4} y={(from.y + 2 * control.y + to.y) / 4 - 8}>+{arrow.soldiers}</text>}
    </g>}
    {pulse && <text className="map-reinforcement" x={pulse.x} y={pulse.y - 40}>+{reinforcement.data.soldiers}</text>}
  </g>
}
