import Button from '../../components/Button.jsx'
import ReplayProgress from './ReplayProgress.jsx'
import { replaySpeeds } from './replayNavigation.js'

export default function ReplayControls({ steps, clock, board }) {
  return <section className="replay-controls" aria-label="Replay controls">
    <div className="replay-display">{board}<p title={steps[clock.index]?.event.message}>{steps[clock.index]?.event.message ?? 'No events recorded for this game.'}</p></div>
    <div className="replay-transport">
      <div className="replay-buttons">
        <Button size="small" disabled={!steps.length} onClick={clock.toggle}>{clock.playing ? 'Pause' : 'Play'}</Button>
        <Button size="small" variant="secondary" disabled={!steps.length} onClick={clock.restart}>Restart</Button>
        <Button size="small" variant="secondary" disabled={!steps.length} onClick={() => clock.skip(-5000)}>Back 5s</Button>
        <Button size="small" variant="secondary" disabled={!steps.length} onClick={() => clock.skip(5000)}>Forward 5s</Button>
      </div>
      <div className="replay-speeds" role="group" aria-label="Playback speed">{replaySpeeds.map(speed => <Button key={speed} type="button" size="small" variant={speed === clock.speed ? 'primary' : 'secondary'} aria-pressed={speed === clock.speed} disabled={!steps.length} onClick={() => clock.setSpeed(speed)}>x{speed}</Button>)}</div>
    </div>
    <ReplayProgress steps={steps} clock={clock} />
  </section>
}
