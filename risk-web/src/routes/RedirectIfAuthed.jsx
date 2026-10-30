import { Navigate } from 'react-router-dom'
import { useAuth } from '../auth/useAuth.js'
import Splash from '../components/Splash.jsx'

// Logged-in users skip the landing, login and register pages.
export default function RedirectIfAuthed({ children }) {
  const { user, loading } = useAuth()
  if (loading) return <Splash />
  return user ? <Navigate to="/play" replace /> : children
}