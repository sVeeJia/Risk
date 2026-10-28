import assert from 'node:assert/strict'
import { describe, it } from 'node:test'
import { toAssignments } from './boardAssignments.js'

const regions = Array.from({ length: 10 }, (_, i) => ({ id: `R${i + 1}` }))
const countries = Array.from({ length: 10 }, (_, i) => ({
  id: `C${i + 1}`, name: `Country ${i + 1}`, population: (i + 1) * 1000, soldiers: 99,
}))
const territories = Array.from({ length: 10 }, (_, i) => ({
  countryId: `C${i + 1}`, name: `Country ${i + 1}`, slot: i + 1, baseSoldiers: 99, soldiers: i + 1,
}))

describe('toAssignments', () => {
  it('maps slot 1 to the first map shape and slot 10 to the last', () => {
    const result = toAssignments({ territories }, countries, regions)
    assert.equal(result.length, 10)
    assert.equal(result[0].territoryId, 'R1')
    assert.equal(result[0].country.id, 'C1')
    assert.equal(result[9].territoryId, 'R10')
    assert.equal(result[9].country.id, 'C10')
  })

  it('uses current soldiers from the server game, not roster or base soldiers', () => {
    const result = toAssignments({ territories }, countries, regions)
    assert.equal(result[4].country.soldiers, 5)
    assert.equal(countries[4].soldiers, 99)
  })

  it('keeps the population from the country list', () => {
    assert.equal(toAssignments({ territories }, countries, regions)[2].country.population, 3000)
  })

  it('orders server territories by slot without mutating either input', () => {
    const reversed = [...territories].reverse()
    const result = toAssignments({ territories: reversed }, countries, regions)
    assert.deepEqual(result.map(r => r.country.id), territories.map(t => t.countryId))
    assert.equal(reversed[0].slot, 10)
    assert.equal(regions[0].id, 'R1')
  })

  it('uses the server ID/name and population zero if the country is missing', () => {
    const result = toAssignments({ territories }, countries.slice(1), regions)
    assert.deepEqual(result[0], { territoryId: 'R1', country: { id: 'C1', name: 'Country 1', population: 0, soldiers: 1 } })
  })

  it('throws when the server returns too few or too many territories', () => {
    assert.throws(() => toAssignments({ territories: territories.slice(0, 9) }, countries, regions), /unexpected board/)
    assert.throws(() => toAssignments({ territories: [...territories, territories[0]] }, countries, regions), /unexpected board/)
  })

  it('rejects duplicate, missing, out-of-range and fractional slots', () => {
    for (const slot of [0, 2, 11, 1.5, undefined]) {
      const invalid = [{ ...territories[0], slot }, ...territories.slice(1)]
      assert.throws(() => toAssignments({ territories: invalid }, countries, regions), /invalid territory slots/)
    }
  })
})
