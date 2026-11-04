import test from 'node:test'
import assert from 'node:assert/strict'
import { createElement } from 'react'
import { renderToStaticMarkup } from 'react-dom/server'
import { createServer } from 'vite'
import { eventMessage, latestAction, safeText } from '../eventText.js'
import { battlePresentation, emptyBattle } from '../battlePresentation.js'
import { completeGame, completeEvents } from './replay.fixture.js'

test('latest action text stays on the roll when commentary arrives', () => {
  const roll = { sequence: 1, type: 'BattleRound', message: 'Attacker loses 1.' }
  const commentary = { sequence: 2, type: 'Commentary', message: 'A fierce exchange.' }
  assert.equal(eventMessage(latestAction([roll, commentary])), eventMessage(latestAction([roll])))
  assert.equal(latestAction([commentary]), null)
  for (const value of [null, undefined, {}, roll, 42]) {
    assert.equal(eventMessage({ message: value }, 'Unavailable'), 'Unavailable')
    assert.equal(safeText(value), '')
  }
})

test('real battle board and log render commentary safely; replay action text is unchanged', async () => {
  const server = await createServer({ server: { middlewareMode: true }, appType: 'custom' })
  try {
    const load = async name => (await server.ssrLoadModule(`/src/features/game/${name}.jsx`)).default
    const BattleBoard = await load('BattleBoard')
    const EventLog = await load('EventLog')
    const ReplayControls = await load('ReplayControls')
    const CommentarySentence = await load('CommentarySentence')
    const Boundary = await load('GameErrorBoundary')
    const throughRoll = completeEvents.slice(0, completeEvents.findIndex(event => event.type === 'BattleRound') + 1)
    const roll = throughRoll.at(-1)
    const caption = { sequence: 100, type: 'Commentary', message: '<b>A fierce exchange.</b>', data: { forSequence: String(roll.sequence), source: 'llm' } }
    const battle = throughRoll.reduce((state, event) => battlePresentation(state, { type: 'event', event }), emptyBattle)
    const boardProps = { board: battle.board, territories: completeGame.territories, players: completeGame.players }
    const plain = renderToStaticMarkup(createElement(BattleBoard, { ...boardProps, events: throughRoll }))
    const narrated = renderToStaticMarkup(createElement(BattleBoard, { ...boardProps, events: [...throughRoll, caption] }))
    assert.ok(plain.includes('Attacker loses'))
    assert.ok(narrated.includes('Attacker loses'))
    assert.ok(narrated.includes('&lt;b&gt;A fierce exchange.&lt;/b&gt;'))
    assert.equal(narrated.includes('battle-board-commentary'), false)
    assert.doesNotThrow(() => renderToStaticMarkup(createElement(EventLog, { events: [...throughRoll, caption, { sequence: 101, message: caption, justification: caption }] })))
    assert.doesNotThrow(() => renderToStaticMarkup(createElement(CommentarySentence, { event: { message: caption } })))
    const controls = events => renderToStaticMarkup(createElement(ReplayControls, {
      steps: events.map(event => ({ event, startMs: 0, durationMs: 100 })),
      clock: { index: events.length - 1, totalMs: 100, timeMs: 0 },
    }))
    const actionText = events => controls(events).match(/<div class="replay-display">(.*?)<\/div>/)[1]
    assert.equal(actionText([roll, caption]), actionText([roll]))
    const boundary = new Boundary({ children: createElement('p', null, 'Game') })
    boundary.state = Boundary.getDerivedStateFromError(new Error('Render failure'))
    assert.match(renderToStaticMarkup(boundary.render()), /Something went wrong, reload/)
  } finally {
    await server.close()
  }
})
