import Die from './Die.jsx'
import { ownerSlot } from './gameActions.js'
import { worldRegions } from '../map/worldRegions.js'

export default function BattleBoard({ board, territories, players = [], fastForward = false, commentary = null }) {
  const ended = Boolean(board?.ended)
  if (!board?.result || board.visible === false) return null
  const name = id => territories.find(country => country.countryId === id)?.name ?? id
  const colour = id => {
    const territory = territories.find(country => country.countryId === id)
    return territory ? worldRegions[ownerSlot({ territories, players }, territory) - 1]?.color ?? '#94a09e' : '#94a09e'
  }
  const attacker = name(board.attackerId)
  const defender = name(board.defenderId)
  const { result } = board
  const diceWidth = result.pairs.length * (result.pairs.length > 6 ? 34 : 48)
  const diceHeight = result.pairs.length > 6 ? 74 : 98
  return <aside className={`battle-board ${fastForward ? 'battle-visuals-fast' : ''} ${ended ? 'battle-board-ended' : ''}`} style={{ '--battle-dice-width': `${diceWidth}px` }} aria-label="Battle board">
    <header><strong title={`${attacker} vs ${defender}`}>
      <svg className="battle-country-dot" viewBox="0 0 10 10" aria-hidden="true"><circle cx="5" cy="5" r="5" fill={colour(board.attackerId)} /></svg>
      <span className="battle-country-name">{attacker}</span>{' vs '}
      <svg className="battle-country-dot" viewBox="0 0 10 10" aria-hidden="true"><circle cx="5" cy="5" r="5" fill={colour(board.defenderId)} /></svg>
      <span className="battle-country-name">{defender}</span>
    </strong><span>Round {board.round}</span></header>
    {result ? <>
      <div className="battle-dice-block" key={board.sequence}>
      <div className="battle-row-labels" aria-hidden="true"><span>ATK</span><span>DEF</span></div>
      <svg className="battle-dice-sheet" viewBox={`0 0 ${diceWidth} ${diceHeight}`} preserveAspectRatio="xMinYMid meet" aria-label="Dice pairs">
      <foreignObject width={diceWidth} height={diceHeight}>
      <div className="battle-dice-pairs" role="list" aria-label="Dice pairs">
        {result.pairs.map((pair, index) => <div className="battle-dice-pair" role="listitem" key={index}>
          <Die value={pair.attacker} status={pair.outcome === 'tie' ? 'tie' : pair.outcome === 'attacker' ? 'win' : 'loss'} label={`Attacker die ${index + 1}`} />
          <span aria-hidden="true">{pair.outcome === 'tie' ? '=' : 'vs'}</span>
          <Die value={pair.defender} status={pair.outcome === 'tie' ? 'tie' : pair.outcome === 'defender' ? 'win' : 'loss'} label={`Defender die ${index + 1}`} />
        </div>)}
      </div>
      </foreignObject>
      </svg>
      </div>
      <footer>
        <p title={`Attacker loses ${result.attackerLosses} | Defender loses ${result.defenderLosses}`}><span className={result.attackerLosses > 0 ? 'battle-loss' : 'battle-neutral'}>Attacker loses {result.attackerLosses}</span><span aria-hidden="true"> | </span><span className={result.defenderLosses > 0 ? 'battle-loss' : 'battle-neutral'}>Defender loses {result.defenderLosses}</span></p>
        <p title={`${attacker} ${board.before?.attackerSoldiers ?? '—'} → ${result.attackerSoldiers} | ${defender} ${board.before?.defenderSoldiers ?? '—'} → ${result.defenderSoldiers}`}><span>{attacker} {board.before?.attackerSoldiers ?? '—'} → {result.attackerSoldiers}</span><span aria-hidden="true"> | </span><span>{defender} {board.before?.defenderSoldiers ?? '—'} → {result.defenderSoldiers}</span></p>
      </footer>
    </> : <p className="battle-board-waiting">Waiting for a roll</p>}
    {ended && <p className="sr-only">Attack ended{board.reason ? `: ${board.reason}` : ''}</p>}
    {commentary && <p className="battle-board-commentary" title={commentary}>{commentary}</p>}
    <p className="sr-only" aria-live="polite" aria-atomic="true">{result ? `Round ${board.round}: ${attacker} loses ${result.attackerLosses}, ${defender} loses ${result.defenderLosses}` : `${attacker} attacks ${defender}`}</p>
  </aside>
}
