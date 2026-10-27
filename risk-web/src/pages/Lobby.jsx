import GameMenu from '../features/game/GameMenu.jsx'
import { useAuth } from '../auth/useAuth.js'

export default function Lobby() {
  const { user, logout } = useAuth()
  return <GameMenu user={user} onLogout={logout} />
}
