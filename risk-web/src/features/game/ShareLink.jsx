import { useEffect, useRef, useState } from 'react'
import Button from '../../components/Button.jsx'
import { shareUrl } from './spectatorEvents.js'

export default function ShareLink({ gameId }) {
  const [copied, setCopied] = useState(false)
  const [fallback, setFallback] = useState(false)
  const timer = useRef(null)
  const url = shareUrl(window.location.origin, gameId)
  useEffect(() => () => clearTimeout(timer.current), [])
  async function copy() {
    try {
      if (!navigator.clipboard?.writeText) throw new Error('Clipboard unavailable')
      await navigator.clipboard.writeText(url)
      setCopied(true)
      clearTimeout(timer.current)
      timer.current = setTimeout(() => setCopied(false), 2000)
    } catch { setFallback(true) }
  }
  return <div className="share-link">
    <Button variant="secondary" size="small" type="button" onClick={copy}>{copied ? 'Copied!' : 'Copy share link'}</Button>
    <span className="sr-only" role="status">{copied ? 'Share link copied.' : ''}</span>
    {fallback && <label>Copy this link <input aria-label="Share link" readOnly value={url} onFocus={event => event.target.select()} autoFocus /></label>}
  </div>
}
