import test from 'node:test'
import assert from 'node:assert/strict'
import { getPublicGame, getPublicGameEvents } from './spectate.js'

test('public game/history GETs omit cookies and preserve bare arrays and cancellation', async t => {
  const calls = [], controller = new AbortController()
  t.mock.method(globalThis, 'fetch', async (url, options) => {
    calls.push({ url, options })
    return new Response(JSON.stringify(url.includes('/events') ? [{ sequence: 4 }] : { id: 'g', version: 1 }))
  })
  assert.equal((await getPublicGame('a/b', { signal: controller.signal })).version, 1)
  assert.deepEqual(await getPublicGameEvents('a/b', 3, 500, { signal: controller.signal }), [{ sequence: 4 }])
  assert.deepEqual(calls.map(call => call.url), ['/api/public/games/a%2Fb', '/api/public/games/a%2Fb/events?after=3&limit=500'])
  assert.ok(calls.every(call => call.options.credentials === 'omit' && call.options.signal === controller.signal && !call.options.body))
})

test('public fetch preserves a 404 for the not-found screen', async t => {
  t.mock.method(globalThis, 'fetch', async () => new Response('{}', { status: 404 }))
  await assert.rejects(getPublicGame('missing'), error => error.status === 404)
})
