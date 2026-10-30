import { isNpcTurn } from './gameActions.js'
import { replayArrow } from '../map/arrows.js'

export async function replayNpcTurns({ game, request, pause, isActive, onEvent, onTurn }) {
  let current = game
  while (isActive() && isNpcTurn(current)) {
    const response = await request()
    if (!isActive()) return
    let arrow = null
    for (const event of response.events) {
      if (!isActive()) return
      arrow = replayArrow(arrow, event)
      onEvent(event, arrow)
      await pause('eventPause')
    }
    if (!isActive()) return
    current = response.game
    onTurn(current)
    if (isNpcTurn(current)) await pause('turnPause')
  }
}
