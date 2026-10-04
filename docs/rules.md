# Rules

This file is the behavioral contract for Mission Splat. Schematic drawings in [`diagrams/`](diagrams/) illustrate it. They are not a second source of truth. Source photographs of the physical game are intentionally absent.

Names below are original. They describe the same structure as the photographed instructions: secret mission cards, square match tiles, orthogonal placement, and a claim that only the placing seat may take.

## Pieces

- A **match tile** is a square 2×2 of cells. A cell is a scoring color, a blank, a power cell, or a wildcard.
- Scoring colors are red, blue, green, and purple.
- A **blank** does not score and has no power.
- A **power cell** is drawn gray and carries one symbol: rotate, stack, or bounce. Gray is not a scoring color.
- A **wildcard** is a quartered red/blue/green/purple cell. It is not gray. When it enters play, the placing seat names one scoring color. That choice lasts for the rest of the game.
- A **mission card** is a secret objective: four in a row, four in a square, or four in an L, in any one scoring color.
- A **claim** is a mission card laid face up in front of the seat that completed it. The claim row is not part of the board.

## Setup

1. Shuffle mission cards. Deal two, face down, to each seat. Seats may look; opponents may not.
2. Shuffle match tiles face down. This is the draw deck.
3. Flip the top match tile and place it as the starting board.

A game has two, three, or four seats. Who is first: the physical rule is the youngest seat. The session takes the first seat as an explicit start input: the humans agree on it, and with an AI seat the human chooses. Rules never compute it. That choice is an assumption recorded in the brief. A starting tile is not a placement, so no claim resolves at setup.

## Turn

1. The current seat draws the top match tile.
2. The seat places the tile. Ordinary placement must share at least one full side with a tile already on the board. Corner-only contact is illegal. See [`diagrams/placement.svg`](diagrams/placement.svg). If the drawn tile has stack, the seat may instead place it on top of a tile already on the board. Stack is this placement, not a second placement after the tile is already beside the board.
3. If the drawn tile has rotate or bounce, the seat may use each once after the placement, under the power rules below.
4. If the drawn tile has a wildcard, the seat names its color before matching.
5. Resolve claims. Then the next seat plays.

A tile that cannot be placed legally does not get discarded by a rule the photographs state. That case is an open edge: stop and return it to the user rather than inventing a discard rule.

## Placement

- The board is an irregular orthogonal cluster. It is not a filled rectangle.
- Tiles stay axis-aligned. The rotate power turns a tile in 90-degree steps; it does not allow diagonal placement.
- Two tiles may occupy the same board position only by the stack power. Otherwise a cell of the board holds one tile.

## Missions

Matching is on the splat lattice. Each placed tile contributes four cells. Adjacent tiles align those cells into a larger grid. A mission looks at scoring colors on that grid, after wildcard substitution and after stack replacement.

- **Four in a row.** Four cells of one scoring color in a horizontal, vertical, or diagonal line. See [`diagrams/mission-row.svg`](diagrams/mission-row.svg).
- **Four in a square.** Four cells of one scoring color in a 2×2 block. See [`diagrams/mission-square.svg`](diagrams/mission-square.svg).
- **Four in an L.** Four cells of one scoring color in an L, any rotation or reflection. See [`diagrams/mission-l.svg`](diagrams/mission-l.svg).

A blank, a gray power cell, and an unchosen wildcard do not complete a color. A chosen wildcard counts as the named color.

## Claim

A seat claims a mission only by placing the match tile that completes that mission's pattern, on that seat's turn. If another seat's placement completes your pattern, you do not claim it.

On a legal claim:

1. Lay that mission card face up in the seat's claim row.
2. Draw a replacement mission card.
3. The seat still ends the turn. The photographs do not grant an extra placement.

First seat to four claims wins. The claim row is the score. It is not scanned as board cells.

## Powers

A power is printed on the tile that was drawn. Using it is optional unless a later ruling says otherwise. The photographs describe these effects. See [`diagrams/power-ups.svg`](diagrams/power-ups.svg).

- **Rotate.** Turn any tile on the board 90 degrees, any number of quarter-turns, if that tile is not completely surrounded. Assumption: completely surrounded means all four orthogonal neighbors are occupied.
- **Stack.** Place the drawn tile on top of any tile on the board instead of beside it. Assumption: the top tile's cells are the cells that match at that position.
- **Bounce.** Remove one tile from the board and put it on the bottom of the draw deck. Do not remove the starting tile if it is the only tile, unless the user later rules otherwise. That edge is open.
- **Blank.** No effect.

A tile may show more than one power cell. Each power cell on the drawn tile may be used once on the turn it is drawn. Powers on tiles already on the board are not reused.

## AI

The AI seat has its own two secret missions. It does not observe any other seat's missions, human or AI. It must play a legal placement. A heuristic that prefers its own missions is enough for the POC. Search with hidden information is out of scope.

## What the photographs do not decide

These are open. Do not invent a ruling in code without returning to the user.

- Full match-tile and mission-card census.
- Whether a stack buries the covered tile for the rest of the game or only while covered.
- Bounce of the last remaining tile.
- A drawn tile that has no legal orthogonal neighbor and no stack power.
- Simultaneous completion of two missions by one placement. Preferred holding assumption: the seat claims one and the other stays secret, but this is not printed.
- A wildcard on the starting tile: who names its color, or whether that tile is redrawn. No claim resolves at setup.
- A pattern completed by rotate, stack, or bounce rather than by the placed tile's own cells: whether the acting seat may claim it.
- Mission deck exhausted when a replacement is due, and match-tile deck exhausted on a draw: not printed. Return to the user.
