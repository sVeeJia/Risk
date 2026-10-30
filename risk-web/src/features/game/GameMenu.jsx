import { useEffect, useRef, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { ArrowRight, ChevronRight, Flag, Globe2, LogOut, Shield, Swords, Users, X } from 'lucide-react'
import WorldMap from '../map/WorldMap.jsx'
import GameSetup from './GameSetup.jsx'
import { regionGroups, worldRegions } from '../map/worldRegions.js'

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
  return <div className="game-shell">
    <header className="game-header">
      <Link to="/" className="brand"><Shield size={27} strokeWidth={1.5} /><span>GEOPOLITICS<small>THE WORLD IS YOURS TO TAKE</small></span></Link>
      <nav aria-label="Main navigation"><span className="nav-active">HEADQUARTERS</span><button onClick={event => openBriefing(event, 'The field manual')}>HOW TO PLAY</button></nav>
      {user ? <button className="commander" onClick={onLogout}><LogOut size={16} /><span>{user.displayName} · Sign out</span></button> : <Link className="commander" to="/login"><Shield size={16} /> COMMANDER LOGIN</Link>}
    </header>
    <main className="command-main">
      <section className="campaign-intro"><p className="eyebrow"><span /> WORLD CONQUEST · TURN-BASED STRATEGY</p><h1>ONE WORLD.<br /><em>ONE VICTOR.</em></h1><p className="intro-copy">Build your army. Forge your alliances.<br />Put your strategy on the map.</p></section>
      <div className="atlas"><div className="atlas-top"><span><Globe2 size={14} /> WORLD THEATER</span><span>UNCHARTED / 01</span></div><WorldMap /><div className="atlas-bottom"><span>3 CENTRAL + 7 OUTER <i /> {worldRegions.length} TERRITORIES</span><span className="map-key"><b /> MAP PREVIEW</span></div><ul className="continent-legend" aria-label="Territory layout">{regionGroups.map(continent => <li key={continent.id}><span style={{ backgroundColor: continent.color }} /><span>{continent.name}</span><b>{continent.territoryCount}</b></li>)}</ul><p className="sea-route-note">DASHED LINES CONNECT TERRITORIES ACROSS WATER</p></div>
      <section className="command-menu" aria-label="Game menu"><p className="eyebrow">{user ? `WELCOME, ${user.displayName}` : 'YOUR CAMPAIGN STARTS HERE'}</p>
        {user ? <button className="deploy-button" onClick={event => openBriefing(event, 'World conquest')}><Swords size={24} /><span>WORLD CONQUEST<small>Plan your next campaign</small></span><ArrowRight size={22} /></button> : <Link to="/register" className="deploy-button"><Swords size={24} /><span>PLAY NOW<small>Create your commander</small></span><ArrowRight size={22} /></Link>}
        {user ? <button className="menu-secondary" onClick={event => openBriefing(event, 'Private game')}><Users size={20} /><span>PLAY WITH FRIENDS<small>Gather your rivals</small></span><ChevronRight size={18} /></button> : <Link to="/login" className="menu-secondary"><Shield size={20} /><span>CONTINUE CAMPAIGN<small>Sign in to headquarters</small></span><ChevronRight size={18} /></Link>}
        <button className="field-manual" onClick={event => openBriefing(event, 'The field manual')}>New to world conquest? <span>Read the field manual <ArrowRight size={13} /></span></button>
      </section>
      <section className="strategy-strip" aria-label="Game principles"><div><span className="step-number">01</span><Flag /><p>CLAIM YOUR GROUND<small>Every territory is a foothold.</small></p></div><div><span className="step-number">02</span><Users /><p>CHOOSE YOUR ALLIES<small>Trust is a tactical decision.</small></p></div><div><span className="step-number">03</span><Swords /><p>CONQUER THE WORLD<small>Only one commander remains.</small></p></div></section>
    </main>
    <footer className="game-footer"><span>GEOPOLITICS <b> / </b> STRATEGY WITHOUT BORDERS</span><span>THE WAR ROOM IS WAITING.</span></footer>
    <dialog ref={dialog} className="briefing" aria-labelledby="briefing-title" onCancel={closeBriefing} onClick={event => { if (event.target === dialog.current) closeBriefing() }}><button className="close-briefing" aria-label="Close briefing" onClick={closeBriefing}><X /></button><p className="eyebrow">COMMANDER BRIEFING</p><h2 id="briefing-title">{briefing}</h2>{briefing === 'The field manual' ? <><p>World conquest is a game of calculated risks. The planned campaign follows three phases:</p><ol><li><strong>Reinforce.</strong> Place armies to defend your borders and prepare your advance.</li><li><strong>Attack.</strong> Challenge neighboring territories and expand your control.</li><li><strong>Fortify.</strong> Move your forces and prepare for the next turn.</li></ol><p>Control the three central lands, secure the surrounding islands, and use the sea routes to expand your reach.</p><small>Gameplay is under development. These are the intended rules.</small></> : briefing === 'World conquest' ? <GameSetup /> : <p>Headquarters is being built. Game setup and multiplayer gameplay are not available yet. Your commander account is ready for future campaigns.</p>}</dialog>
  </div>
}
