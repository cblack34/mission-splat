# Base deck census

The physical base set, counted from the maintainer's photos on 2026-10-04. Photographs stay out of the repo. Decorations on the mission cards are not rules.

This is data, not a rule. A game is built from a deck definition: a multiset of mission cards and a multiset of match tiles. The matcher does not know this list. The base definition is the one below. A later rule set, or extra tiles, is another definition. Adding tiles must not require a change to placement, matching, or claim.

The physical match deck ran out in play. What a draw does when that deck is empty is still an open rule in `rules.md`. A larger deck is a new definition, not a ruling on that edge.

## Mission cards

Twelve cards. Each is one color and one shape. The shapes are the three in `rules.md`: four in a row, four in a square, four in an L. An L includes the rotations and reflections in that document.

| Color | Row | Square | L |
|---|---|---|---|
| Blue | 0 | 1 | 2 |
| Red | 0 | 2 | 1 |
| Purple | 1 | 1 | 1 |
| Green | 0 | 1 | 2 |
| Total | 1 | 5 | 6 |

The red card whose splats read as a zigzag in the photo is counted as the red L. If that card is a different shape, the row above is wrong and the count should be corrected before the deck definition is encoded.

## Match tiles

Thirty tiles, six rows of six in the photo. A cell is a scoring color, a blank, a power, or a wildcard. No blank cell appears in this set. Gray is a power cell. The wildcard is the quartered cell.

Power glyphs in the photo: a plus is stack, a circular arrow is rotate, a power-button glyph is bounce. A few gray cells read as a vertical bar; those are counted as rotate. Confirm that glyph before encoding if it is a different symbol.

Cells are listed top-left, top-right, bottom-left, bottom-right.

| # | Cells |
|---|---|
| 1 | gray, wildcard, red, purple |
| 2 | green, blue, blue, green |
| 3 | red, purple, red, gray-rotate |
| 4 | green, blue, wildcard, gray |
| 5 | green, gray, blue, stack |
| 6 | wildcard, blue, gray, purple |
| 7 | green, gray, red, wildcard |
| 8 | gray-rotate, blue, blue, gray |
| 9 | purple, red, purple, red |
| 10 | green, blue, green, stack |
| 11 | purple, stack, gray, purple |
| 12 | stack, blue, green, red |
| 13 | red, green, green, red |
| 14 | gray, red, red, rotate |
| 15 | gray-rotate, green, green, gray |
| 16 | rotate, wildcard, gray, gray |
| 17 | purple, blue, green, red |
| 18 | gray, wildcard, rotate, wildcard |
| 19 | purple, blue, green, red |
| 20 | gray, rotate, purple, green |
| 21 | red, blue, green, gray-rotate |
| 22 | blue, purple, blue, purple |
| 23 | purple, blue, stack, red |
| 24 | green, purple, red, rotate |
| 25 | purple, gray, red, stack |
| 26 | rotate, purple, red, purple |
| 27 | blue, gray, stack, blue |
| 28 | blue, blue, bounce, green |
| 29 | purple, gray, bounce, green |
| 30 | blue, gray, red, rotate |

Two tiles in this list have the same four cells (17 and 19). That is what the photo shows, not a duplicate row in this document.
