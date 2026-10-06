# Slice plan — App local session

## Strategic source

- **Active build pack:** [`docs/build-brief.md`](../../build-brief.md), [`docs/rules.md`](../../rules.md), [`docs/architecture.md`](../../architecture.md), [`docs/acceptance.md`](../../acceptance.md), [`docs/engineering/workflow.md`](../../engineering/workflow.md), [`docs/engineering/code-quality.md`](../../engineering/code-quality.md)
- **Human approval:** Approved in session on 2026-10-06. The session may read the one match tile the next placement will consume. Power commands stay later. The AI is one heuristic, with no difficulty control.
- **Final acceptance advanced:** The Session checks in [`docs/acceptance.md`](../../acceptance.md): a real seat view hides other unclaimed missions, scripted pass-and-play alternates on `LocalSession`, and the AI returns a legal placement from its own hand and the public board. Presentation, power-action, and device checks wait.

## Outcome

A `netstandard2.1` app library runs a 2–4 seat game on one in-process session. Each seat receives a view of its own secret missions and the public table. An AI seat returns a legal placement from that view. GitHub Actions runs the app tests in the existing rules job.

## Why this slice is next

Claim fixtures are on `main`. The build brief's next consumer is `ISession`, `LocalSession`, and `AiPlayer`. A Unity player bound straight to `Game` would skip the seat-scoped view acceptance requires. Rotate, stack, and bounce stay later commands on this same boundary.

## Scope

### In scope

- App library and test project, referenced from Rules by project reference, with no Unity reference and no production NuGet dependency.
- `ISession`, `IPlayer`, `LocalSession`, and `AiPlayer`.
- A seat-scoped view: that seat's unclaimed missions, every seat's claims, the public board, whose turn it is, whether the game has ended, and the tile the next placement will consume.
- A rules read of that one pending match tile, including its four cells. The rest of either deck stays hidden.
- A non-committing preview that uses `Game.Place` and discards the returned game.
- Place commands accepted only from the current seat.
- One AI policy: prefer a preview that claims one of its own missions, otherwise one deterministic legal placement.
- Scripted pass-and-play at three and four seats.
- The app Release test command added to the existing Actions job named `rules`.

### Out of scope

- A difficulty, hardness, or randomness setting. One policy is enough for the POC.
- Rotate, stack-on-top, bounce, and any open ruling in [`docs/rules.md`](../../rules.md). `UnresolvedRulingException` propagates.
- `HumanPlayer`, the Unity project, the UPM package, a desktop build, the room service, and `RemoteSession`.
- Census encoding, shuffling, and hidden-information search.
- Giving a player the `Game` object.

## Strategic traceability

| Strategic requirement or criterion | How this slice advances it |
| --- | --- |
| Offline multiplayer is local | Scripted human command sources alternate on one `LocalSession` at three and four seats. |
| Session view hides other hands | The real session view, not a test double, omits other seats' unclaimed mission ids. Claimed missions stay public. |
| AI plays legally without other missions | `AiPlayer` receives the seat view and uses the session preview. A test pins a claiming choice and the absence of other mission ids. |
| Rules stay engine-free | App references Rules. A guard rejects a Unity reference in the app project. Rules behavior is unchanged apart from the pending-tile read. |
| App definition of done | `dotnet test app/MissionSplat.App.sln --configuration Release` runs locally and in the `rules` job. |
| Powers stay later | This slice does not execute rotate, stack, or bounce. |

## Gates and dependencies

### Hard gates

- The rules library on `main` can start a game and place a tile. That work is merged.

### Sequencing recommendations

- Bootstrap the solution and CI, then the session and view, then the AI. The AI needs the view and the preview.

### Closed by the human for this slice

- The session may observe the single match tile the next `Place` will consume.
- Power commands are a later rules slice.
- The POC AI has one heuristic and no difficulty control.

### Still open

- How long a stack stays buried, bounce of the last tile, a drawn tile with no legal placement, claim by rotate, stack, or bounce, and an exhausted mission or match deck.
- Uncorrected census rows. Fixtures use a smaller named deck.

## Architecture and contracts

- **Affected seams:** App owns `ISession` and `IPlayer`. `LocalSession` and `AiPlayer` are the adapters. Rules remains the command and event authority.
- **Public contracts:** `Game` gains a read of the pending match tile and its four cells. Events stay `TilePlaced`, `MissionClaimed`, and `GameWon`. A preview reports acceptance and the acting seat's claims without advancing the session.
- **Data and migration considerations:** None. There is no saved game.

## High-level approach

`LocalSession` owns the current `Game`. Start and place delegate to the rules library. A place from any seat other than the current seat is rejected and leaves the session unchanged. The seat view is the only state a player receives. The pending match tile is part of that public view because the table can see the tile about to be placed. Later deck tiles are not part of the view.

The preview calls `Game.Place` and drops the returned game, so the AI can ask which placements the rules accept and which of its own missions they would claim. The AI searches orthogonal neighbors and quarter-turns 0 through 3. It takes a placement that claims one of its own missions when one exists, and otherwise one deterministic accepted placement. It does not reimplement matching.

## Verification

- `dotnet test rules/MissionSplat.Rules.sln --configuration Release`
- `dotnet test app/MissionSplat.App.sln --configuration Release`
- The secrecy check uses the real session view.
- The three-seat and four-seat scripts each complete a full round, and each seat claims.
- The AI command is accepted, and a fixture shows it choosing a placement that claims its own mission.
- The app project does not reference Unity.
- No device or Unity evidence in this slice.

## Risks and stop conditions

- Stop if a correct view or preview has to carry another seat's unclaimed mission.
- Stop if showing the pending tile requires revealing later deck tiles.
- Stop if the AI needs its own copy of pattern matching or placement legality.
- Stop if a test needs a power command or a deck-exhaustion ruling.
- Do not rename the Actions job. The required status check is `rules`.

## Execution issues

GitHub issues are the WIP tracker and source of task-level detail.

| Issue | Purpose | Dependencies |
| --- | --- | --- |
| [#6](https://github.com/cblack34/mission-splat/issues/6) — Bootstrap the app solution and CI | Create the app solution, the Unity-reference guard, and the app test step in the existing Actions job. | None |
| [#7](https://github.com/cblack34/mission-splat/issues/7) — Add the local session and seat-scoped view | `ISession`, `IPlayer`, `LocalSession`, the pending-tile read, the preview, and the pass-and-play fixtures. | #6 |
| [#8](https://github.com/cblack34/mission-splat/issues/8) — Add the AI player heuristic | One legal-placement policy over the seat view, plus the secrecy and claim-preference tests. | #7 |

## Delivery shape

- **Topology:** Direct PR to `main`.
- **Branch or spine:** `feat/app-local-session`, cut from `origin/main`.
- **Final PR:** Opened from `feat/app-local-session`. The link is recorded in the delivery record.
- **Human merge gate:** Only the human may physically merge the final PR to `main`. Agents must stop when it is ready.

## Amendments

None.

## Delivery record

Complete once when the final PR is ready for human merge. Do not use this section for WIP status.

- **Outcome:** `LocalSession` runs a 2–4 seat game in process. A seat view shows that seat's unclaimed missions and every seat's claims. `AiPlayer` returns a legal placement and prefers one that claims its own mission. The `rules` job also runs the app tests.
- **Verification:** `dotnet test rules/MissionSplat.Rules.sln --configuration Release` passed, 77 tests, 0 failed. `dotnet test app/MissionSplat.App.sln --configuration Release` passed, 13 tests, 0 failed. Evidence is on #6, #7, #8, and the pull request.
- **Deviations:** `Game.PendingMatchTile` reads the one tile the next placement will consume. `Tile.Local` is public so that tile's four cells can be shown. `LocalSession` remembers accepted placement coordinates because `Game` does not enumerate placed tiles; cell values are read back from the grid. No difficulty control.
- **Unresolved gates or risks:** Power effects, how long a stack stays buried, bounce of the last tile, a tile with no legal placement, and deck exhaustion. Exhaustion still throws `UnresolvedRulingException` and does not change the game. Uncorrected census rows still need a maintainer pass. Unity, the UPM package, and device play are a later slice.
- **Final PR:** Recorded when the pull request is opened.
- **Merge state:** Ready for review. Agents do not merge to `main`.
