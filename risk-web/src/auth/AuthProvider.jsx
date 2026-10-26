import { useEffect, useMemo, useState } from 'react'
import { api } from '../api/client.js'
import { AuthContext } from './authContext.js'

export default function AuthProvider({ children }) {
  const [user, setUser] = useState(null)
  const [loading, setLoading] = useState(true)

  // Ask the server who we are. The token lives in an HttpOnly cookie, so JavaScript never touches it.
  useEffect(() => {
    api.get('/api/auth/me')
      .then(setUser)
      .catch(() => setUser(null))
      .finally(() => setLoading(false))
  }, [])

  const value = useMemo(() => ({
    user,
    loading,
    login: async (email, password) => setUser(await api.post('/api/auth/login', { email, password })),
    register: async (email, password, displayName) =>
      setUser(await api.post('/api/auth/register', { email, password, displayName })),
    logout: async () => {
      await api.post('/api/auth/logout')
      setUser(null)
    },
  }), [user, loading])

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}