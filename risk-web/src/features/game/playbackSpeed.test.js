import assert from 'node:assert/strict'
import { test } from 'node:test'
import { normal, fast, playbackSpeed, playbackPause } from './playbackSpeed.js'

test('playbackSpeed supplies the exact normal and fast delays', () => {
  assert.deepEqual(normal, { eventPause: 800, turnPause: 1200 })
  assert.deepEqual(fast, { eventPause: 120, turnPause: 150 })
  assert.equal(playbackSpeed(false), normal)
  assert.equal(playbackSpeed(true), fast)
})
test('a speed change shortens the pause already running', async t => {
  t.mock.timers.enable({ apis: ['Date', 'setTimeout'] })
  let enabled = false
  let done = false
  const pending = playbackPause('eventPause', () => enabled, new AbortController().signal).then(() => { done = true })
  t.mock.timers.tick(100)
  await Promise.resolve()
  assert.equal(done, false)
  enabled = true
  t.mock.timers.tick(40)
  await pending
  assert.equal(done, true)
})
test('aborting cancels a pause immediately', async () => {
  const controller = new AbortController()
  const pending = playbackPause('turnPause', () => false, controller.signal)
  controller.abort()
  await pending
  await playbackPause('eventPause', () => false, controller.signal)
})
