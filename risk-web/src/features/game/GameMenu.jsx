import { useEffect, useRef, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { ArrowRight, ChevronRight, History, LogOut, Shield, Swords, X } from 'lucide-react'
import HomeLayout from './HomeLayout.jsx'
import SavedCampaigns from './SavedCampaigns.jsx'
import GameSetup from './GameSetup.jsx'

export default function GameMenu({ user, onLogout }) {
  const [searchParams] = useSearchParams()
  const [briefing, setBriefing] = useState(() => user && searchParams.get('new') === '1' ? 'World conquest' : null)
  const dialog = useRef(null)
  const opener = useRef(null)
  useEffect(() => {
    if (briefing) dialog.current?.showModal()
    else dialog.current?.close()
  }, [briefing])
  function openBriefing(event, title) { opener.current = event.currentTarget; setBriefing(title) }
  function closeBriefing() { setBriefing(null); opener.current?.focus() }
  return <HomeLayout
    onManual={event => openBriefing(event, 'The field manual')}
    headerActions={user ? <button className="commander" onClick={onLogout}><LogOut size={16} /><span>{user.displayName} · Sign out</span></button> : <Link className="commander" to="/login"><Shield size={16} /> COMMANDER LOGIN</Link>}
    actions={<>
      {user ? <button className="deploy-button" onClick={event => openBriefing(event, 'World conquest')}><Swords size={24} /><span>WORLD CONQUEST<small>Plan your next campaign</small></span><ArrowRight size={22} /></button> : <Link to="/register" className="deploy-button"><Swords size={24} /><span>PLAY NOW<small>Create your commander</small></span><ArrowRight size={22} /></Link>}
      {user ? <button className="menu-secondary" onClick={event => openBriefing(event, 'Continue campaign')}><Shield size={20} /><span>CONTINUE CAMPAIGN<small>Resume a saved game</small></span><ChevronRight size={18} /></button> : <Link to="/login" className="menu-secondary"><Shield size={20} /><span>CONTINUE CAMPAIGN<small>Sign in to headquarters</small></span><ChevronRight size={18} /></Link>}
      {user ? <button className="menu-secondary" onClick={event => openBriefing(event, 'Review')}><History size={20} /><span>REVIEW<small>Watch a finished game</small></span><ChevronRight size={18} /></button> : <div aria-hidden="true" />}
    </>}
  >
    <dialog ref={dialog} className="briefing" aria-labelledby="briefing-title" onCancel={closeBriefing} onClick={event => { if (event.target === dialog.current) closeBriefing() }}><button className="close-briefing" aria-label="Close briefing" onClick={closeBriefing}><X /></button><p className="eyebrow">COMMANDER BRIEFING</p><h2 id="briefing-title">{briefing}</h2>{briefing === 'The field manual' ? <><p>World conquest is a game of calculated risks. The planned campaign follows three phases:</p><ol><li><strong>Reinforce.</strong> Place armies to defend your borders and prepare your advance.</li><li><strong>Attack.</strong> Challenge neighboring territories and expand your control.</li><li><strong>Fortify.</strong> Move your forces and prepare for the next turn.</li></ol><p>Control the three central lands, secure the surrounding islands, and use the sea routes to expand your reach.</p><small>Gameplay is under development. These are the intended rules.</small></> : briefing === 'World conquest' ? <GameSetup /> : ['Continue campaign', 'Review'].includes(briefing) ? <SavedCampaigns key={briefing} mode={briefing === 'Review' ? 'review' : 'resume'} /> : null}</dialog>
  </HomeLayout>
}
