import Alert from '../../components/Alert.jsx'
import Button from '../../components/Button.jsx'

export default function SpectatorStatus({ game, playback, battleBoard }) {
  const current = game.players.find(player => player.id === game.currentPlayerId)
  const text = playback.playing ? 'Replaying live actions…' : `${current?.name ?? 'Player'} is playing…`
  return <section className="action-panel" aria-label="Read-only game status">
    <div className="action-panel-shell">
      <div className="action-panel-body dock-body-group">
        <div className="action-panel-info">{battleBoard}</div>
        <div className="action-panel-details">
          <p className="action-panel-status" title={text}>{text}</p>
          <p className="action-panel-helper">Read-only spectator</p>
          {playback.error && <div className="action-panel-error" title={playback.error}><Alert>{playback.error}</Alert></div>}
        </div>
      </div>
      <footer className="action-panel-footer">
        {playback.playing && <Button type="button" variant="secondary" aria-pressed={playback.fastForward} onClick={playback.toggleSpeed}>Fast-forward: {playback.fastForward ? 'On' : 'Off'}</Button>}
        {playback.status === 'Disconnected' && <Button type="button" variant="secondary" onClick={playback.retry}>Reconnect</Button>}
      </footer>
    </div>
  </section>
}
