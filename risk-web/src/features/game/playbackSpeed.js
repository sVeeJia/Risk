export const normal = Object.freeze({ eventPause: 800, turnPause: 1200 })
export const fast = Object.freeze({ eventPause: 120, turnPause: 150 })
export const playbackSpeed = enabled => enabled ? fast : normal

// Re-read speed while waiting so toggling also changes the current pause.
export function playbackPause(kind, readFast, signal) {
  return new Promise(resolve => {
    const started = Date.now()
    let timer
    const finish = () => { clearTimeout(timer); signal.removeEventListener('abort', finish); resolve() }
    const tick = () => {
      if (signal.aborted || Date.now() - started >= playbackSpeed(readFast())[kind]) finish()
      else timer = setTimeout(tick, 20)
    }
    signal.addEventListener('abort', finish, { once: true })
    tick()
  })
}
