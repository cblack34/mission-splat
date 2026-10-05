# Final Acceptance and Verification

The active scope is complete when every criterion below holds and every required command passes. Tactical replanning may derive additional slice checks but may not weaken or replace this contract.

## Run and verify

```bash
dotnet test rules/MissionSplat.Rules.sln --configuration Release
dotnet test app/MissionSplat.App.sln --configuration Release
```

Agent-run Unity CLI evidence (`unity test` results and play-mode captures that show the game advancing) is recorded in the completing PR. Device evidence stays human observation, also recorded in the PR: one pass-and-play game and one human-versus-AI game, each played to a win or to an agreed stop on a rules question returned to the user.

## Rules

- WHEN a tile is placed corner-only against the board, the placement is rejected and the board is unchanged. _Automated check:_ fixture asserts no tile added.
- WHEN four cells of one scoring color form a row, square, or L, and the current seat placed the completing tile, that seat claims the matching secret mission. _Automated check:_ one fixture per shape; the row fixture covers horizontal, vertical, and diagonal; the L covers all 8 orientations; at least one row, square, and L spans a tile boundary; a pattern with one blank, gray, or unnamed-wildcard cell does not claim.
- WHEN a claim resolves, that mission moves to the claim row, a replacement is drawn, and the turn passes. _Automated check:_ hand size unchanged, claim row grows by one, current seat changes.
- WHEN a seat holds its fourth claim, a won event is emitted and further commands are rejected. _Automated check:_ three claims do not win; four claims win, and a command after the win is rejected.
- WHEN a game is set up at two, three, and four seats, each seat holds exactly two distinct secret missions, the board holds exactly one starting tile, and no claim resolves at setup. _Automated check:_ one fixture per seat count asserts hand sizes of 2, no duplicate mission ids across seats, board count of 1, and empty claim rows.
- WHEN a session view is handed to a seat, it contains that seat's unclaimed missions and no other seat's unclaimed mission identities. Claimed missions are present for every seat. _Automated check:_ the real session view, not a test double, hides the other hands.
- WHEN another seat's placement completes your pattern, you do not claim it, and you still do not claim it after you later place a tile that contributes no cell to that pattern. _Automated check:_ pattern exists, claim list unchanged for your seat, at two seats and at three or more seats.
- WHEN a wildcard is chosen, later matching treats it as that color for the rest of the game. _Automated check:_ fixture before and after the next placement.
- WHEN a tile that is not completely surrounded is rotated, only that tile's cells turn by the requested quarter-turns. WHEN a tile is completely surrounded, rotate is rejected for that tile. _Automated check:_ a rotate on a tile with a free side changes only that tile's cells; four orthogonal neighbors, rotate rejected.
- WHEN the drawn tile shows more than one power cell, each cell can be used once on that turn and no more. _Automated check:_ a tile with two rotate cells allows two rotates and rejects a third; a power printed on a tile already on the board cannot be used.
- WHEN the drawn tile has stack and is placed on an occupied position, that position matches the top tile's cells. _Automated check:_ covered color no longer completes a pattern; top color does.
- WHEN bounce removes a tile, that tile leaves the board and is the bottom of the draw deck. _Automated check:_ board count drops by one, the removed tile is the last element of the draw deck, and the order of the other deck tiles is unchanged.
- WHEN a tile without stack is placed on an occupied position, the placement is rejected. A tile with stack may still use ordinary orthogonal placement. _Automated check:_ non-stack on an occupied position leaves the board unchanged; a stack tile that shares a full side is accepted.
- The rules test project does not reference Unity.

## Session

- WHEN two to four human command sources alternate on `LocalSession`, the seat marker changes and each can claim. _Automated check:_ scripted pass-and-play of at least one full round at three seats and at four seats.
- WHEN the AI is asked for a command, the command is a legal placement, and the AI object's inputs do not include the other seats' mission ids. _Automated check:_ test double exposes only the public board and that AI hand.
- The Unity player can bind 2–4 seats, any mix of human and AI, without a server process. _Human evidence:_ one pass-and-play game of at least three seats and one game with an AI seat.

## Presentation

- The table shows the board, the current seat's secret missions, and every seat's claim row. _Human evidence:_ screenshot or play-mode capture in the completing PR.
- WHEN the turn passes between human seats on one device, the secret missions are hidden until the incoming seat confirms. _Human evidence:_ observed in the 3-seat pass-and-play game.
- Legal orthogonal neighbors highlight before a placement. Corner-only slots do not. _Human evidence:_ the same play-mode capture.
- Claimed missions render in a row that is not part of the board lattice. _Human evidence:_ the same play-mode capture.

## Trade dress

- The repository and the player content contain no source photograph, farm character, or physical-product name. _Human evidence:_ diff review on the completing PR.

## Research and decision gates

The deck census gate stays open. Acceptance allows a representative deck only if the fixtures say it is representative. Acceptance does not require the room service.

## Deliberately excluded

Network rooms, `RemoteSession`, accounts, WebGL, store listings, more than four seats, and copied physical-game art are out of scope. See [`build-brief.md`](build-brief.md). Do not implement them to satisfy this document.
