export function playerSummary(game, player) {
  const home = game.territories.find(territory => territory.countryId === player.startingCountryId)
  const owned = player.isEliminated ? [] : game.territories.filter(territory => territory.ownerPlayerId === player.id)
  return {
    slot: home?.slot,
    name: player.isHuman ? player.name : `NPC · ${home?.name ?? player.name}`,
    soldiers: owned.reduce((total, territory) => total + territory.soldiers, 0),
    countries: owned.length,
  }
}
