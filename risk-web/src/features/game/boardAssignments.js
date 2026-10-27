// Territory slot N (1 to 10) on the server is the map shape at index N - 1 in worldRegions.
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
        // Soldiers always come from the server's game.
        country: { ...listed, soldiers: territory.soldiers },
      }
    })
}
