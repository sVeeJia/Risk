import { isNpcTurn } from './gameActions.js'
import { replayArrow } from '../map/arrows.js'

export async function replayEvents({ events, arrow = null, pause, isActive, onEvent }) {
  for (const event of events) {
    if (!isActive()) return arrow
    arrow = replayArrow(arrow, event)
    onEvent(event, arrow)
    await pause('eventPause')
  }
  return arrow
}

export async function replayNpcTurns({ game, request, pause, isActive, onEvent, onTurn }) {
  let current = game
  while (isActive() && isNpcTurn(current)) {
    const response = await request()
    if (!isActive()) return
    await replayEvents({ events: response.events, pause, isActive, onEvent })
    if (!isActive()) return
    current = response.game
    onTurn(current)
    if (isNpcTurn(current)) await pause('turnPause')
  }
}
