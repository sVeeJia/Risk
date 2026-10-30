import { Link } from 'react-router-dom'
import Button from '../../components/Button.jsx'
import { findHuman } from './gameActions.js'

export default function GameOver({ game }) {
  const won = game.winnerPlayerId === findHuman(game)?.id
  return <section className="game-over" aria-labelledby="game-over-title">
    <div className="game-over-info">
    <p className="eyebrow">GAME OVER</p>
    <h2 id="game-over-title">{won ? 'You won' : 'You lost'}</h2>
    <p>Ended in round {game.round}</p>
    <h3>Final countries held</h3>
    <ul>{game.players.map(player => <li key={player.id}>
      <span>{player.name}{player.isHuman ? ' (you)' : ''}</span>
      <strong>{player.countriesOwned}</strong>
    </li>)}</ul>
    </div>
    <div className="game-over-links">
      <Button as={Link} to="/play?new=1">New game</Button>
      <Button as={Link} variant="secondary" to="/play">Back to headquarters</Button>
    </div>
  </section>
}
