# Rules

This file is the behavioral contract for Mission Splat. Schematic drawings in [`diagrams/`](diagrams/) illustrate it. They are not a second source of truth. Source photographs of the physical game are intentionally absent.

Names below are original. They describe the same structure as the photographed instructions: secret mission cards, square match tiles, orthogonal placement, and a claim that only the placing seat may take.

## Pieces

- A **match tile** is a square 2×2 of cells. A cell is a scoring color, a non-scoring symbol, or a wildcard.
- A **scoring color** is an id in the color catalog passed at setup. The ordinary catalog is red, blue, green, and purple. A pattern check compares the mission's color id to the cell. It does not special-case those four names, so another color passed at setup matches the same way.
- A **non-scoring symbol** does not complete a pattern. The ordinary symbols are blank, rotate, stack, and bounce. Blank has no power. Rotate, stack, and bounce are the power symbols, drawn gray. Gray is not a scoring color. Another symbol passed at setup is the same kind of data and also does not score.
- A **wildcard** is a quartered cell. It is not gray. It is always wild. No command names a color for it, and nothing stores a chosen color. Each mission check counts the cell as that mission's color, so one cell can count as red for one mission and blue for another. A wildcard on the starting tile stays wild.
- A **mission card** is a secret objective: four in a row, four in a square, or four in an L, in one scoring color. The patterns in play are the set passed at setup. The ordinary set is the row, the square, and the L. A shape that was not passed in does not claim.
- A **claim** is a mission card laid face up in front of the seat that completed it. The claim row is not part of the board.

## Setup

The library does not shuffle. It takes the seats in turn order, the first seat, the color catalog, the non-scoring symbols, the patterns in play, the claims required to win, and the mission and match decks already ordered. The base deck is [`census.md`](census.md). A fixture may use a smaller deck if it names that deck. The ordinary game uses the ordinary catalog above and 4 claims. Setup can pass another positive count.

1. Deal two missions from the front of the mission deck to each seat, in the seat order given to setup.
2. Place the front match tile at the origin of the board, in the orientation it has in the deck.

A game has two, three, or four seats. Who is first: the physical rule is the youngest seat. The session takes the first seat as an explicit start input: the humans agree on it, and with an AI seat the human chooses. Rules never compute it. That choice is an assumption recorded in the brief. A starting tile is not a placement, so no claim resolves at setup, even when its cells already show a pattern.

## Turn

1. The current seat draws the top match tile.
2. The seat places the tile, choosing a quarter-turn orientation as it is set down. Quarter-turns are clockwise. That choice is part of placement, not the rotate power. Ordinary placement must share at least one full side with a tile already on the board. Corner-only contact is illegal. See [`diagrams/placement.svg`](diagrams/placement.svg). If the drawn tile has stack, the seat may instead place it on top of a tile already on the board. Stack is this placement, not a second placement after the tile is already beside the board.
3. If the drawn tile has rotate or bounce, the seat may use each such power cell once after the placement, under the power rules below.
4. Resolve claims. Then the next seat plays.

A rejected placement does not change the board, the decks, or whose turn it is. A tile that cannot be placed legally does not get discarded by a rule the photographs state. That case is an open edge: stop and return it to the user rather than inventing a discard rule.

## Placement

- The board is an irregular orthogonal cluster of tiles. It is not a filled rectangle.
- Matching reads the cell grid. Placing a tile writes that tile's four cells. The cards are the visual; the grid is the match space.
- Tiles stay axis-aligned. The rotate power turns a tile already on the board in 90-degree steps; it does not allow diagonal placement.
- Two tiles may occupy the same board position only by the stack power. Otherwise a cell of the board holds one tile.
- A square may be four corner cells from four different tiles, one from each tile, where the four tiles meet.

## Missions

A mission looks at one scoring color on the cell grid. A wildcard counts as the color of the mission being checked. It is not assigned a color first. A blank or any other non-scoring symbol does not complete the color.

- **Four in a row.** Four cells in a horizontal, vertical, or diagonal line. See [`diagrams/mission-row.svg`](diagrams/mission-row.svg).
- **Four in a square.** Four cells in a 2×2 block. See [`diagrams/mission-square.svg`](diagrams/mission-square.svg). The block may be the four cells where four tiles meet.
- **Four in an L.** Three cells in a line plus a fourth adjacent to one end, any rotation or reflection. That is eight orientations. It is not a T, a skew, or a square. See [`diagrams/mission-l.svg`](diagrams/mission-l.svg).

Only a pattern in the set passed at setup can be claimed. A seat claims a pattern only when the tile just placed completed it.

## Claim

A seat claims a mission only by placing the match tile that completes that mission's pattern, on that seat's turn. If another seat's placement completes your pattern, you do not claim it, including when you later place a tile that adds no cell to that pattern.

One placement claims every secret mission the acting seat currently holds whose pattern that placement completed. Other seats do not claim from that placement.

On those claims, as one resolution:

1. For each such mission, lay it face up in the seat's claim row and draw one replacement.
2. Do not claim a replacement drawn during this resolution, even if the board already shows its pattern.
3. The seat then ends the turn. One resolution passes the turn once.

The ordinary win count is 4. Setup can require a different positive count. The first seat whose claim row reaches that count wins. If one placement's claims cross the count together, every claim from that placement still counts, and the win is emitted after them. The winning placement is not rejected. A later command is rejected. The claim row is the score. It is not scanned as board cells.

## Powers

A power is printed on the tile that was drawn. Using it is optional unless a later ruling says otherwise. The photographs describe these effects. See [`diagrams/power-ups.svg`](diagrams/power-ups.svg).

- **Rotate.** After the drawn tile is set down, and before claims, each rotate cell on that tile may turn one tile on the board by one, two, or three clockwise quarter-turns if that tile is not completely surrounded. Completely surrounded means all four orthogonal neighbors are occupied. Only the chosen tile's visible cells turn. Claims read the four cells the placed tile wrote, after those turns. Turning a different tile does not award a claim from that tile's cells.
- **Stack.** Place the drawn tile on top of any tile on the board instead of beside it. The top tile's cells are the cells that match at that position. The covered tiles remain under that position until a bounce uncovers the top one.
- **Bounce.** Remove one tile from the board and put it on the bottom of the draw deck. If the named position has a tile buried beneath it, only the top tile is removed; the tile beneath becomes visible again, with the cells it had at the moment it was covered, not the cells its own placement orientation would otherwise show. If the named position has no buried tile, the position is removed from the board entirely. The tile the acting seat just placed this turn can never be a bounce target, so there is always at least one single-layer position left and a bounce can never empty the board.
- **Blank.** No effect.

A tile may show more than one power cell. Each rotate or bounce cell on the drawn tile may be used once on the turn it is drawn. Stack is not a repeatable action: one or more stack cells on the drawn tile grant the optional stack placement once, and extra stack cells do not grant a second placement. Powers on tiles already on the board are not reused.

## AI

The AI seat has its own two secret missions. It does not observe any other seat's missions, human or AI. It must play a legal placement. A heuristic that prefers its own missions is enough for the POC. Search with hidden information is out of scope.

## What the photographs do not decide

These are open. Do not invent a ruling in code without returning to the user.

- The base deck is [`census.md`](census.md): four colors, one line, one L, and one square each, and 30 match tiles. Uncorrected rows in that table still need the maintainer's pass before encoding.
- A drawn tile that has no legal orthogonal neighbor and no stack power.
- A pattern completed by stack or bounce rather than by the placed tile's own cells: whether the acting seat may claim it.
- Mission deck exhausted when a replacement is due, and match-tile deck exhausted on a draw: not printed. Return to the user.
