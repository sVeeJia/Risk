export function assignCountries(countries, selectedId, territories, random = Math.random) {
  const selected = countries.find(country => country.id === selectedId)
  if (!selected) throw new Error('Your selected country is unavailable. Please choose another country.')
  const others = countries.filter(country => country.id !== selectedId)
  if (others.length < territories.length - 1) throw new Error('There are not enough countries to prepare the board.')
  for (let i = others.length - 1; i > 0; i--) {
    const j = Math.floor(random() * (i + 1))
    ;[others[i], others[j]] = [others[j], others[i]]
  }
  return territories.map((territory, index) => ({
    territoryId: territory.id,
    country: index === 0 ? selected : others[index - 1],
  }))
}
