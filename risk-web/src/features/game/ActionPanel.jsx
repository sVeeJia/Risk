import { useCallback, useEffect, useId, useRef, useState } from 'react'
import Alert from '../../components/Alert.jsx'
import Button from '../../components/Button.jsx'
import { worldRegions } from '../map/worldRegions.js'
import { clampAmount, findHuman, maxMovable, ownerSlot, panelMode, quickAmounts, stepAmount } from './gameActions.js'

export default function ActionPanel({ game, selection, actionMode, onMode, onAction, busy, error, playback }) {
  const mode = panelMode(game)
  const human = findHuman(game)
  const country = id => game.territories.find(t => t.countryId === id)
  const source = country(selection.from)
  const target = country(selection.to)
  const attacker = country(game.activeAttack?.attackerCountryId)
  const defender = country(game.activeAttack?.defenderCountryId)
  const current = game.players.find(p => p.id === game.currentPlayerId)
  const winner = game.players.find(p => p.id === game.winnerPlayerId)
  const maximum = mode === 'reinforce' ? human.pendingReinforcements : maxMovable(attacker ?? source ?? { soldiers: 1 })
  const amountContext = JSON.stringify([selection.from, selection.to, maximum])
  const [amount, setAmount] = useState(() => ({ value: String(Math.max(1, maximum)), context: amountContext }))
  if (amount.context !== amountContext) {
    setAmount({ value: String(clampAmount(amount.value, maximum)), context: amountContext })
  }
  const chooseAmount = value => setAmount({ value: String(value), context: amountContext })
  const inputId = useId()
  const hold = useRef({ delay: null, repeat: null, repeated: false })
  const stopHold = useCallback(() => {
    clearTimeout(hold.current.delay)
    clearInterval(hold.current.repeat)
  }, [])
  useEffect(() => {
    window.addEventListener('pointerup', stopHold)
    window.addEventListener('blur', stopHold)
    return () => {
      stopHold()
      window.removeEventListener('pointerup', stopHold)
      window.removeEventListener('blur', stopHold)
    }
  }, [amountContext, busy, stopHold])
  const step = delta => setAmount(previous => ({ value: String(stepAmount(previous.value, delta, maximum)), context: amountContext }))
  const stepButton = (delta, label, disabled) => <Button variant="secondary" size="icon" type="button" aria-label={label} disabled={disabled}
    onPointerDown={event => {
      if (event.button !== 0 || busy) return
      stopHold()
      hold.current.repeated = false
      hold.current.delay = setTimeout(() => {
        hold.current.repeated = true
        step(delta)
        hold.current.repeat = setInterval(() => step(delta), 80)
      }, 400)
    }}
    onPointerUp={stopHold} onPointerLeave={stopHold} onPointerCancel={stopHold} onBlur={stopHold} onTouchEnd={stopHold}
    onClick={event => { stopHold(); if (event.detail === 0 || !hold.current.repeated) step(delta); hold.current.repeated = false }}>
    {delta < 0 ? '-' : '+'}
  </Button>
  const soldiers = Number(amount.value)
  const validAmount = Number.isInteger(soldiers) && soldiers >= 1 && soldiers <= maximum
  const submit = (event, action, body) => {
    event.preventDefault()
    onAction(action, body)
  }
  const amountInput = <div className="action-panel-amount">
    <label htmlFor={inputId}>Soldiers</label>
    <div className="action-panel-stepper">
      {stepButton(-1, 'Decrease soldiers', maximum < 1 || soldiers <= 1)}
      <input id={inputId} type="number" min="1" max={maximum} step="1" required value={amount.value}
        onChange={e => chooseAmount(e.target.value === '' ? '' : clampAmount(e.target.value, maximum))}
        onBlur={() => chooseAmount(clampAmount(amount.value, maximum))} />
      {stepButton(1, 'Increase soldiers', maximum < 1 || soldiers >= maximum)}
    </div>
    <div className="action-panel-quick-amounts" role="group" aria-label="Quick soldier amounts">
      {quickAmounts(maximum).map(({ label, value }) => <Button key={label} variant={soldiers === value ? 'primary' : 'secondary'} size="small" type="button" aria-pressed={soldiers === value}
        aria-label={`Set amount to ${label === 'Half' ? 'half' : label === 'Max' ? 'maximum' : `${label} soldiers`}`}
        onClick={() => chooseAmount(value)}>{label}</Button>)}
    </div>
  </div>
  const chip = (label, territory) => <span className={`action-panel-chip ${territory ? '' : 'action-panel-chip-empty'}`}>
    <span className="action-panel-chip-label">{label}:</span>
    <span className="action-panel-chip-country">
      {territory && <svg viewBox="0 0 10 10" aria-hidden="true"><circle cx="5" cy="5" r="5" fill={worldRegions[ownerSlot(game, territory) - 1]?.color} /></svg>}
      <span title={territory?.name ?? 'Choose a country'}>{territory?.name ?? 'Choose a country'}</span>
    </span>
  </span>

  return <section className="action-panel" aria-label="Turn controls" aria-busy={busy}>
    <Alert>{error}</Alert>
    {mode === 'npc' && <div className="npc-playback action-panel-mode">
      <div className="action-panel-info">
      <p title={`${current?.name} is playing...`}>{current?.name} is playing...</p>
      {playback?.currentEvent?.justification && <p className="npc-playback-why">Why: {playback.currentEvent.justification}</p>}
      </div>
      <div className="action-panel-choices">
      {playback?.playing && <Button variant="secondary" type="button" aria-pressed={playback.fastForward} onClick={playback.toggleSpeed}>Fast-forward: {playback.fastForward ? 'On' : 'Off'}</Button>}
      {playback?.error && <Button variant="secondary" type="button" onClick={playback.retry}>Retry</Button>}
      </div>
    </div>}
    <fieldset disabled={busy}>
      {mode === 'reinforce' && <form className="action-panel-mode" onSubmit={e => submit(e, 'reinforce', { countryId: selection.from, soldiers })}>
        <div className="action-panel-info">
        <h2>Place {human.pendingReinforcements} reinforcements</h2>
        <p>Choose one of your countries on the map.</p>
        <p className="action-panel-country" title={`Country: ${source?.name ?? 'None selected'}`}>Country: {source?.name ?? 'None selected'}</p>
        </div>
        <div className="action-panel-choices">
        {amountInput}<Button className="action-panel-submit" disabled={!source || !validAmount}>Place</Button>
        </div>
      </form>}
      {mode === 'action' && <div className="action-panel-mode">
        <div className="action-panel-info">
        <div className="action-panel-tabs">
          <Button variant={actionMode === 'attack' ? 'primary' : 'secondary'} type="button" aria-pressed={actionMode === 'attack'} onClick={() => onMode('attack')}>Attack</Button>
          <Button variant={actionMode === 'move' ? 'primary' : 'secondary'} type="button" aria-pressed={actionMode === 'move'} onClick={() => onMode('move')}>Move soldiers</Button>
        </div>
        <p className="action-panel-helper" title={actionMode === 'attack' ? 'Select your source country (2+ soldiers), then an enemy country.' : 'Select your source country, then another of your countries.'}>{actionMode === 'attack' ? 'Select your source country (2+ soldiers), then an enemy country.' : 'Select your source country, then another of your countries.'}</p>
        <div className="action-panel-selection">{chip('From', source)}<span aria-hidden="true">→</span>{chip('To', target)}</div>
        {actionMode === 'attack' && game.attackedThisTurn && <p className="action-panel-notice" title="You have already attacked this turn"><span aria-hidden="true">!</span>You have already attacked this turn</p>}
        </div>
        <div className="action-panel-choices">
        <div className={`action-panel-actions ${actionMode === 'move' ? 'action-panel-actions-move' : ''}`}>
        {actionMode === 'attack' ? <>
          <Button disabled={!source || !target || game.attackedThisTurn} onClick={() => onAction('attack', { fromCountryId: selection.from, toCountryId: selection.to })}>Attack</Button>
        </> : <form onSubmit={e => submit(e, 'transfer', { fromCountryId: selection.from, toCountryId: selection.to, soldiers })}>
          {amountInput}<Button className="action-panel-submit" disabled={!source || !target || !validAmount}>Move</Button>
        </form>}
        <Button variant="secondary" type="button" onClick={() => onAction('end')}>End turn</Button>
        </div>
        </div>
      </div>}
      {mode === 'attacking' && <div className="action-panel-mode">
        <div className="action-panel-info">
        <h2 title={`${attacker?.name} is attacking ${defender?.name}`}>{attacker?.name} is attacking {defender?.name}</h2>
        <p><small>Stopping ends your turn.</small></p>
        </div>
        <div className="action-panel-choices">
        <div className="action-panel-button-row">
        <Button onClick={() => onAction('roll')}>Roll</Button>
        <Button variant="danger" onClick={() => onAction('stop')}>Stop attack</Button>
        </div>
        </div>
      </div>}
      {mode === 'conquest' && <form className="action-panel-mode" onSubmit={e => submit(e, 'conquest', { soldiers })}>
        <div className="action-panel-info">
        <h2 title={`You conquered ${defender?.name}. Move soldiers in`}>You conquered {defender?.name}. Move soldiers in</h2>
        </div>
        <div className="action-panel-choices">
        {amountInput}<Button className="action-panel-submit" disabled={!validAmount}>Move</Button>
        </div>
      </form>}
      {mode === 'finished' && <p role="status" className="action-panel-finished">{!game.winnerPlayerId ? 'You were eliminated. Game over.' : winner?.id === human.id ? 'You conquered the world!' : `${winner?.name} conquered the world`}</p>}
    </fieldset>
  </section>
}
