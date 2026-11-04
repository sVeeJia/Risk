import { Component } from 'react'

export default class GameErrorBoundary extends Component {
  state = { failed: false }

  static getDerivedStateFromError() {
    return { failed: true }
  }

  render() {
    if (this.state.failed) return <main className="game-page"><div className="game-page-message" role="alert">
      <p>Something went wrong, reload</p>
      <button type="button" onClick={() => window.location.reload()}>Reload</button>
    </div></main>
    return this.props.children
  }
}
