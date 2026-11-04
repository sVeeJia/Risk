import { totalTime } from './replayTimeline.js'

export const replaySpeeds = [1, 2, 4, 8, 16]
export const pacedDuration = (durationMs, speed) => durationMs / (replaySpeeds.includes(speed) ? speed : 1)
export const heavyAnimation = speed => speed <= 4

export function seekTime(steps, targetMs) {
  if (!steps.length) return -1
  const target = Math.max(0, Math.min(totalTime(steps), Number(targetMs) || 0))
  let low = 0, high = steps.length - 1
  while (low < high) {
    const mid = Math.ceil((low + high) / 2)
    if (steps[mid].startMs <= target) low = mid
    else high = mid - 1
  }
  return low
}

export const seekFraction = (steps, fraction) => seekTime(steps, Math.max(0, Math.min(1, Number(fraction) || 0)) * totalTime(steps))
export function skipTime(steps, index, deltaMs, fromMs = steps[index]?.startMs ?? 0) {
  if (!steps.length) return -1
  const target = seekTime(steps, fromMs + deltaMs)
  if (target === index && deltaMs) return Math.max(0, Math.min(steps.length - 1, index + Math.sign(deltaMs)))
  return target
}

export const timeLabel = ms => `${Math.floor(ms / 60000).toString().padStart(2, '0')}:${Math.floor(ms / 1000 % 60).toString().padStart(2, '0')}`

// Three lanes; close flags move to the nearest free slot, with a connector to their exact timeline position.
export function stackFlags(markers, width, totalMs) {
  if (!markers.length || width <= 0) return []
  const columns = Math.max(1, Math.floor(width / 18), Math.ceil(markers.length / 3))
  const cell = width / columns, occupied = new Set()
  return markers.map(step => {
    const anchor = totalMs ? step.startMs / totalMs * width : 0
    const preferred = Math.min(columns - 1, Math.floor(anchor / cell))
    let slot, lane
    outer: for (let distance = 0; distance < columns; distance++) {
      for (const candidate of [...new Set([preferred - distance, preferred + distance])]) {
        if (candidate < 0 || candidate >= columns) continue
        for (let row = 0; row < 3; row++) if (!occupied.has(`${candidate}:${row}`)) {
          slot = candidate; lane = row; break outer
        }
      }
    }
    occupied.add(`${slot}:${lane}`)
    return { ...step, anchor, x: (slot + .5) * cell, lane, size: Math.min(14, cell * .8) }
  })
}
