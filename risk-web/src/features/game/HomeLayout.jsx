import { Link } from 'react-router-dom'
import { ArrowRight, Globe2, Shield } from 'lucide-react'
import WorldMap from '../map/WorldMap.jsx'

export default function HomeLayout({ headerActions, actions, onManual, children }) {
  return <div className="game-shell">
    <header className="game-header">
      <Link to="/" className="brand"><Shield size={27} strokeWidth={1.5} /><span>GEOPOLITICS<small>THE WORLD IS YOURS TO TAKE</small></span></Link>
      <nav aria-label="Main navigation"><span className="nav-active">HEADQUARTERS</span><button onClick={onManual}>HOW TO PLAY</button></nav>
      {headerActions}
    </header>
    <main className="command-main">
      <section className="campaign-intro"><p className="eyebrow"><span /> WORLD CONQUEST · TURN-BASED STRATEGY</p><h1>ONE WORLD.<br /><em>ONE VICTOR.</em></h1><p className="intro-copy">Build your army. Forge your alliances.<br />Put your strategy on the map.</p></section>
      <div className="atlas">
        <div className="atlas-top"><span><Globe2 size={14} /> WORLD THEATER</span><span>UNCHARTED / 01</span></div>
        <WorldMap />
        <div className="atlas-bottom"><span>10 TERRITORIES</span><span className="map-key"><b /> MAP PREVIEW</span></div>
      </div>
      <section className="command-menu" aria-label="Game menu">
        <p className="eyebrow">YOUR CAMPAIGN STARTS HERE</p>
        <div className="home-actions">{actions}</div>
        <button className="field-manual" onClick={onManual}>New to world conquest? <span>Read the field manual <ArrowRight size={13} /></span></button>
      </section>
    </main>
    {children}
  </div>
}
