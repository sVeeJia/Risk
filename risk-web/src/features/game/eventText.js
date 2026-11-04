export const safeText = (value, fallback = '') => typeof value === 'string' ? value : fallback
export const eventMessage = (event, fallback = '') => safeText(event?.message, fallback)
export const isGameAction = event => Boolean(event && event.type !== 'Commentary')
export const latestAction = (events = []) => events.findLast(isGameAction) ?? null
