import { ownerSlot } from './gameActions.js'

// Territory slots fix map positions; owner slots determine colours.
export function toAssignments(game, countries, regions) {
  if (game.territories.length !== regions.length) {
    throw new Error('The server returned an unexpected board.')
  }

  const countriesById = new Map(countries.map(c => [c.id, c]))
  const territories = [...game.territories].sort((a, b) => a.slot - b.slot)
  if (territories.some((territory, index) => territory.slot !== index + 1)) {
    throw new Error('The server returned invalid territory slots.')
  }

  return territories
    .map((territory, index) => {
      const listed = countriesById.get(territory.countryId)
        ?? { id: territory.countryId, name: territory.name, population: 0 }
      return {
        territoryId: regions[index].id,
        colorSlot: ownerSlot(game, territory),
        // Soldiers always come from the server's game.
        country: { ...listed, soldiers: territory.soldiers },
      }
    })
}
