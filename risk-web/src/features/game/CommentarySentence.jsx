import { eventMessage } from './eventText.js'
import { isAiCommentary } from './commentary.js'

export default function CommentarySentence({ event }) {
  return <>{eventMessage(event)}{isAiCommentary(event) && <span className="commentary-ai">AI</span>}</>
}
