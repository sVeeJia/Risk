// Fictional ten-territory board: three central lands and seven surrounding islands.
// Coordinates use a 960 x 500 SVG. Edit shapes here without changing region IDs.
export const regionGroups = [
  { id: 'center', name: 'Central territories', territoryCount: 3, color: '#c6aa68' },
  { id: 'outer', name: 'Surrounding territories', territoryCount: 7, color: '#729d9a' },
]

// Stable irregular coastlines keep the same board while starting order is shuffled.
// [ID, name, group, label position, color, polygon]
const territories = [
  ['C1', 'Territory 1', 'center', [369,244], '#0000FF', '330,200 370,188 410,180 430,220 410,250 425,290 390,315 353,297 325,280 310,240'],
  ['C2', 'Territory 2', 'center', [490,242], '#FF0000', '410,180 455,163 490,170 520,160 550,190 565,230 540,265 550,300 512,315 480,325 425,290 410,250 430,220'],
  ['C3', 'Territory 3', 'center', [612,247], '#00FF00', '550,190 585,175 620,180 643,191 665,205 680,250 650,290 625,298 600,315 550,300 540,265 565,230'],
  ['O1', 'Territory 4', 'outer', [226,132], '#800080', '130,110 160,87 181,91 205,67 232,80 250,70 280,98 289,125 272,153 285,170 251,185 226,174 201,187 177,169 152,160 158,140 132,133'],
  ['O2', 'Territory 5', 'outer', [475,83], '#FFC0CB', '381,74 403,51 432,58 455,38 482,48 510,40 538,60 568,55 584,81 566,102 541,100 523,124 494,117 471,132 446,116 414,121 399,98 378,93'],
  ['O3', 'Territory 6', 'outer', [737,127], '#FFFF00', '661,98 685,72 714,80 741,62 762,80 787,77 817,104 806,123 832,144 811,164 784,161 765,181 737,169 718,177 688,155 693,133 668,124'],
  ['O4', 'Territory 7', 'outer', [811,266], '#000000', '750,224 781,203 802,214 828,204 850,225 873,220 892,246 875,269 887,291 860,313 837,307 815,328 789,309 763,316 742,288 757,268 739,246'],
  ['O5', 'Territory 8', 'outer', [722,398], '#FFFFFF', '653,367 681,346 704,354 728,338 751,356 784,351 802,377 792,398 813,423 782,436 759,425 739,452 712,441 692,447 673,422 647,410 663,389'],
  ['O6', 'Territory 9', 'outer', [465,419], '#FFA500', '368,402 396,379 421,386 447,366 472,380 499,368 523,392 551,390 566,417 549,436 522,434 503,463 477,451 450,467 428,448 402,453 390,431 364,425'],
  ['O7', 'Territory 10', 'outer', [218,352], '#A52A2A', '135,323 163,301 185,309 207,287 231,302 256,294 279,320 270,345 291,366 271,391 242,384 225,414 201,401 177,410 163,386 139,379 147,354 127,344'],
]

export const worldRegions = territories.map(([id, name, group, at, color, shape]) => ({
  id, name, group, at, color, shapes: [shape],
  includes: `${name} is ${group === 'center' ? 'one of the three central territories' : 'one of the seven surrounding territories'} in this fictional world.`,
}))

// Sea paths run through open water, outside the land polygons.
// Change 'd' to reshape a dotted line; 'from' and 'to' identify its endpoints.
export const seaRoutes = [
  { id: 'northreach-center', from: 'O1', to: 'C1', d: 'M280 169Q310 172 332 203' },
  { id: 'frosthaven-center', from: 'O2', to: 'C2', d: 'M494 117Q489 142 490 170' },
  { id: 'dawn-center', from: 'O3', to: 'C3', d: 'M690 155Q658 166 643 191' },
  { id: 'storm-center', from: 'O4', to: 'C3', d: 'M744 253Q710 235 680 250' },
  { id: 'ember-center', from: 'O5', to: 'C3', d: 'M681 346Q664 318 645 292' },
  { id: 'southreach-center', from: 'O6', to: 'C2', d: 'M472 380Q464 351 480 325' },
  { id: 'dusk-center', from: 'O7', to: 'C1', d: 'M277 319Q300 290 325 280' },
  { id: 'northreach-frosthaven', from: 'O1', to: 'O2', d: 'M280 98Q326 60 381 74' },
  { id: 'frosthaven-dawn', from: 'O2', to: 'O3', d: 'M584 81Q626 57 665 95' },
  { id: 'dawn-storm', from: 'O3', to: 'O4', d: 'M811 164Q841 179 828 204' },
  { id: 'storm-ember', from: 'O4', to: 'O5', d: 'M815 328Q817 342 784 351' },
  { id: 'ember-southreach', from: 'O5', to: 'O6', d: 'M647 410Q608 443 566 417' },
  { id: 'southreach-dusk', from: 'O6', to: 'O7', d: 'M368 402Q322 427 271 391' },
  { id: 'dusk-northreach', from: 'O7', to: 'O1', d: 'M185 309Q149 240 177 169' },
]
