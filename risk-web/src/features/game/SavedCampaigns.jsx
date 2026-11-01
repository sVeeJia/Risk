import { useEffect, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { getGame, listGames } from '../../api/games.js'
import Alert from '../../components/Alert.jsx'
import Button from '../../components/Button.jsx'
import { campaignEmptyMessage, campaignListState, campaignRow, campaignsForMode, loadSavedCampaigns } from './savedCampaigns.js'

export default function SavedCampaigns({ mode = 'resume' }) {
  const navigate = useNavigate()
  const [state, setState] = useState({ games: [], loading: true, error: '' })
  const [attempt, setAttempt] = useState(0)
  useEffect(() => {
    const controller = new AbortController()
    loadSavedCampaigns(listGames, getGame, { signal: controller.signal }).then(games => {
      if (!controller.signal.aborted) setState({ games, loading: false, error: '' })
    }).catch(error => {
      if (!controller.signal.aborted) setState({ games: [], loading: false, error: error.message })
    })
    return () => controller.abort()
  }, [attempt])
  const games = campaignsForMode(state.games, mode)
  const status = campaignListState({ ...state, games })
  return <div className="saved-campaigns">
    {status === 'loading' && <p role="status">Loading saved campaigns…</p>}
    {status === 'empty' && <p role="status">{campaignEmptyMessage(mode)}</p>}
    {status === 'error' && <><Alert>{state.error}</Alert><Button variant="secondary" onClick={() => { setState({ games: [], loading: true, error: '' }); setAttempt(value => value + 1) }}>Retry</Button></>}
    {status === 'ready' && <ul aria-label={mode === 'review' ? 'Finished games' : 'Campaigns in progress'}>{games.map(game => {
      const row = campaignRow(game)
      return <li key={row.id} className="saved-campaign-row" role="link" tabIndex={0}
        aria-label={`${row.action} campaign created ${row.date}, round ${row.round}`}
        onClick={() => navigate(row.href)}
        onKeyDown={event => {
          if (event.target === event.currentTarget && (event.key === 'Enter' || event.key === ' ')) {
            event.preventDefault()
            navigate(row.href)
          }
        }}>
        <div className="saved-campaign-text">
          <time dateTime={game.createdAt}>{row.date}</time>
          <span>{row.summary}</span>
          <small>{row.countryLabel}</small>
        </div>
        <Button as={Link} to={row.href} className="saved-campaign-action" onClick={event => event.stopPropagation()}>{row.action}</Button>
      </li>
    })}</ul>}
  </div>
}
