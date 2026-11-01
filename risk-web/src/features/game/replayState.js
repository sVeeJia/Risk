const number = value => {
  const parsed = typeof value === 'string' || typeof value === 'number' ? parseInt(value, 10) : NaN
  return Number.isSafeInteger(parsed) && parsed >= 0 ? parsed : null
}

export function initialReplayState(game, events) {
  return { ...game, round: 1, phase: 'Action', winnerPlayerId: null, activeAttack: null, attackedThisTurn: false,
    currentPlayerId: [...events].sort((a, b) => a.sequence - b.sequence).find(event => event.type === 'TurnStarted')?.playerId ?? null,
    territories: game.territories.map(territory => {
      const soldiers = number(territory.baseSoldiers)
      const owner = game.players.find(player => player.startingCountryId === territory.countryId)
      if (soldiers == null || !owner) throw new Error(`Missing starting state for ${territory.name ?? territory.countryId}.`)
      return { ...territory, soldiers, ownerPlayerId: owner.id }
    }),
    players: game.players.map(player => ({ ...player, countriesOwned: 1, isEliminated: false, pendingReinforcements: 0 })),
  }
}

// Historical bookkeeping only: apply the supplied event facts, never roll dice or choose a turn.
export function applyReplayEvent(previous, event) {
  const state = { ...previous, territories: previous.territories.map(item => ({ ...item })), players: previous.players.map(item => ({ ...item })) }
  const data = event.data ?? {}
  const issues = []
  const require = (value, field) => {
    if (value == null || value === '') throw new Error(`Missing or invalid ${field}`)
    return value
  }
  const country = id => require(state.territories.find(item => item.countryId === id), 'country reference')
  const player = id => require(state.players.find(item => item.id === id), 'playerId')
  const count = key => require(number(data[key]), `data.${key}`)
  const attack = () => require(state.activeAttack, 'active attack')
  try {
    switch (event.type) {
      case 'GameStarted': case 'TurnSkipped': case 'NpcDecision': case 'Commentary': break
      case 'TurnStarted':
        player(event.playerId)
        state.round = count('round')
        state.currentPlayerId = event.playerId
        state.activeAttack = null
        state.attackedThisTurn = false
        state.phase = 'Action'
        break
      case 'ReinforcementsReceived':
        player(event.playerId).pendingReinforcements = count('soldiers')
        state.phase = 'Reinforcing'
        break
      case 'ReinforcementsPlaced': {
        const target = country(data.countryId), owner = player(event.playerId), soldiers = count('soldiers'), remaining = count('remaining')
        target.soldiers += soldiers
        owner.pendingReinforcements = remaining
        state.phase = remaining ? 'Reinforcing' : 'Action'
        break
      }
      case 'Transfer': {
        const from = country(data.fromCountryId), to = country(data.toCountryId), soldiers = count('soldiers')
        if (from.soldiers < soldiers) throw new Error('Transfer exceeds recorded source soldiers')
        from.soldiers -= soldiers
        to.soldiers += soldiers
        state.phase = 'Action'
        break
      }
      case 'AttackStarted':
        country(data.fromCountryId); country(data.toCountryId)
        state.activeAttack = { attackerCountryId: data.fromCountryId, defenderCountryId: data.toCountryId }
        state.phase = 'Attacking'
        state.attackedThisTurn = true
        break
      case 'BattleRound': {
        const active = attack(), from = country(active.attackerCountryId), to = country(active.defenderCountryId)
        const attacker = count('attackerSoldiers'), defender = count('defenderSoldiers')
        count('attackerLosses'); count('defenderLosses')
        for (const key of ['attackerDice', 'defenderDice']) {
          const dice = typeof data[key] === 'string' ? data[key].split(',').map(value => parseInt(value, 10)) : []
          if (!dice.length || dice.some(die => !Number.isInteger(die) || die < 1 || die > 6)) throw new Error(`Missing or invalid data.${key}`)
        }
        from.soldiers = attacker
        to.soldiers = defender
        break
      }
      case 'CountryConquered': {
        const target = country(data.countryId), conqueror = player(event.playerId), from = country(attack().attackerCountryId), reward = count('reward')
        require(data.previousOwnerId, 'data.previousOwnerId')
        target.ownerPlayerId = conqueror.id
        from.soldiers += reward
        state.phase = 'ConquestTransfer'
        break
      }
      case 'PlayerEliminated': player(event.playerId).isEliminated = true; break
      case 'AttackEnded':
        require(data.reason, 'data.reason')
        state.activeAttack = null
        if (data.reason !== 'conquest') state.phase = 'Action'
        break
      case 'GameWon': case 'GameLost':
        player(event.playerId)
        state.phase = 'Finished'
        state.winnerPlayerId = event.type === 'GameWon' ? event.playerId : null
        state.activeAttack = null
        break
      default: issues.push(`Unknown event type ${event.type}`)
    }
    for (const item of state.players) item.countriesOwned = state.territories.filter(territory => territory.ownerPlayerId === item.id).length
    return { state, issues }
  } catch (error) {
    // Discard the entire transaction, including partial writes, when an old event is incomplete.
    return { state: previous, issues: [error.message] }
  }
}

export function replayDifferences(computed, actual) {
  const differences = []
  for (const field of ['phase', 'round', 'currentPlayerId', 'winnerPlayerId']) if (computed[field] !== actual[field]) {
    differences.push({ field, computed: computed[field], actual: actual[field] })
  }
  for (const country of actual.territories) {
    const recorded = computed.territories.find(item => item.countryId === country.countryId)
    for (const field of ['ownerPlayerId', 'soldiers']) if (recorded?.[field] !== country[field]) {
      differences.push({ countryId: country.countryId, field, computed: recorded?.[field], actual: country[field] })
    }
  }
  for (const player of actual.players) {
    const recorded = computed.players.find(item => item.id === player.id)
    if (Boolean(recorded?.isEliminated) !== Boolean(player.isEliminated)) differences.push({ playerId: player.id, field: 'isEliminated', computed: recorded?.isEliminated, actual: player.isEliminated })
  }
  return differences
}
