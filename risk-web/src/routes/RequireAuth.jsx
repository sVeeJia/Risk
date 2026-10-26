import { Navigate } from 'react-router-dom'
import { useAuth } from '../auth/useAuth.js'
import Splash from '../components/Splash.jsx'

// Logged-out visitors go to the login page.
export default function RequireAuth({ children }) {
  const { user, loading } = useAuth()
  if (loading) return <Splash />
  return user ? children : <Navigate to="/login" replace />
}

