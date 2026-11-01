import { Navigate, Route, Routes } from 'react-router-dom'
import { lazy, Suspense } from 'react'
import { RedirectIfAuthed, RequireAuth } from './routes/guards.jsx'
import Landing from './pages/Landing.jsx'
import Login from './pages/Login.jsx'
import Register from './pages/Register.jsx'
import Lobby from './pages/Lobby.jsx'
import Game from './pages/Game.jsx'
const Watch = lazy(() => import('./pages/Watch.jsx'))
const Replay = lazy(() => import('./pages/Replay.jsx'))

export default function App() {
  return (
    <Routes>
      <Route path="/" element={<RedirectIfAuthed><Landing /></RedirectIfAuthed>} />
      <Route path="/login" element={<RedirectIfAuthed><Login /></RedirectIfAuthed>} />
      <Route path="/register" element={<RedirectIfAuthed><Register /></RedirectIfAuthed>} />
      <Route path="/play" element={<RequireAuth><Lobby /></RequireAuth>} />
      <Route path="/watch/:gameId" element={<Suspense fallback={<p role="status">Loading game…</p>}><Watch /></Suspense>} />
      <Route path="/replay/:gameId" element={<Suspense fallback={<p role="status">Loading replay…</p>}><Replay /></Suspense>} />
      <Route path="/game/:gameId" element={<RequireAuth><Game /></RequireAuth>} />
      <Route path="/play/:gameId" element={<RequireAuth><Game /></RequireAuth>} />
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}
