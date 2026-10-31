const corners = [[9, 9], [23, 9], [9, 23], [23, 23]]
const pips = {
  1: [[16, 16]], 2: [[9, 9], [23, 23]], 3: [[9, 9], [16, 16], [23, 23]],
  4: corners, 5: [...corners, [16, 16]], 6: [...corners, [9, 16], [23, 16]],
}

export default function Die({ value, status, label }) {
  return <span className={`battle-die battle-die-${status}`}>
    <svg viewBox="0 0 32 32" role="img" aria-label={`${label}: ${value}`}>
      <rect className="battle-die-face" x="2" y="2" width="28" height="28" rx="5" />
      <g className="battle-die-pips">{(pips[value] ?? []).map(([x, y]) => <circle key={`${x}:${y}`} cx={x} cy={y} r="2.2" />)}</g>
    </svg>
    {status === 'loss' && <span className="battle-die-cross" aria-hidden="true">×</span>}
  </span>
}
