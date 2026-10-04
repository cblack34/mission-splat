# Final Acceptance and Verification

The active scope is complete when every criterion below holds and every required command passes. Tactical replanning may derive additional slice checks but may not weaken or replace this contract.

## Run and verify

```bash
dotnet test rules/Barnyard.Rules.sln --configuration Release
dotnet test app/Barnyard.App.sln --configuration Release
```

Unity device evidence is human observation recorded in the completing PR: one pass-and-play game and one human-versus-AI game, each played to a win or to an agreed stop on a rules question returned to the user.

## Rules

- WHEN a tile is placed corner-only against the board, the placement is rejected and the board is unchanged. _Automated check:_ fixture asserts no tile added.
- WHEN four cells of one scoring color form a row, square, or L, and the current seat placed the completing tile, that seat claims the matching secret mission. _Automated check:_ one fixture per shape.
- WHEN the other seat's placement completes your pattern, you do not claim it. _Automated check:_ pattern exists, claim list unchanged for your seat.
- WHEN a wildcard is chosen, later matching treats it as that color for the rest of the game. _Automated check:_ fixture before and after the next placement.
- WHEN a tile is completely surrounded, rotate is rejected for that tile. _Automated check:_ four orthogonal neighbors, rotate rejected.
- The rules test project does not reference Unity.

## Session

- WHEN two human command sources alternate on `LocalSession`, the seat marker changes and both can claim. _Automated check:_ scripted pass-and-play of at least four placements.
- WHEN the AI is asked for a command, the command is a legal placement, and the AI object's inputs do not include the human mission ids. _Automated check:_ test double exposes only the public board and the AI hand.
- The Unity player can bind two human seats, or one human and one AI, without a server process. _Human evidence:_ the two device sessions above.

## Presentation

- The table shows the board, the current seat's secret missions, and both claim rows.
- Legal orthogonal neighbors highlight before a placement. Corner-only slots do not.
- Claimed missions render in a row that is not part of the board lattice.

## Trade dress

- The repository and the player content contain no source photograph, farm character, or physical-product name. _Human evidence:_ diff review on the completing PR.

## Research and decision gates

The deck census gate stays open. Acceptance allows a representative deck only if the fixtures say it is representative. Acceptance does not require the room service.

## Deliberately excluded

Network rooms, `RemoteSession`, accounts, WebGL, store listings, more than two seats, and copied physical-game art are out of scope. See [`build-brief.md`](build-brief.md). Do not implement them to satisfy this document.
