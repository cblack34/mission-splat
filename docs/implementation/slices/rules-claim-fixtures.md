# Slice plan — Rules claim fixtures

## Strategic source

- **Active build pack:** [`docs/build-brief.md`](../../build-brief.md), [`docs/rules.md`](../../rules.md), [`docs/architecture.md`](../../architecture.md), [`docs/acceptance.md`](../../acceptance.md), [`docs/engineering/workflow.md`](../../engineering/workflow.md), [`docs/engineering/code-quality.md`](../../engineering/code-quality.md)
- **Human approval:** Approved in session on 2026-10-04. The wildcard stays wild. One placement may claim every aligning mission the acting seat holds. Win count and the tile vocabulary are supplied at setup.
- **Final acceptance advanced:** The Rules checks in [`docs/acceptance.md`](../../acceptance.md) for placement, lattice claims, stolen claims, setup, replacement, and win. Session, presentation, and power-action checks wait for later slices.

## Outcome

A `netstandard2.1` rules library can set up a 2–4 seat game, place a tile on the orthogonal board, and claim every secret mission of the acting seat that the placement completed on the splat grid. GitHub Actions runs that library's tests.

## Why this slice is next

Matching and placement-owned claim are the hard dependency for every later adapter. The repository has no Actions workflow, and neither definition-of-done solution exists. This unit creates the rules solution and the workflow that runs it. Power actions stay later because several of their interactions are still open rulings.

## Scope

### In scope

- Rules library and test project, callable from `dotnet test`, with no Unity reference.
- CI for `dotnet test rules/MissionSplat.Rules.sln --configuration Release`.
- Representative mission deck and match deck, ordered by the fixture, large enough that the fixtures do not exhaust either deck.
- Explicit first seat and explicit claims-to-win count. The ordinary game uses 4.
- Orthogonal placement. Corner-only contact is illegal. A non-stack tile on an occupied cell is illegal. A tile that shows stack may still be placed on a legal full side.
- Splat-grid matching for row, square, and L. A tile writes four cells. Matching reads the grid.
- The four-tile corner square: four tiles meet, each contributing one corner splat, and only the seat who places the finishing tile claims.
- Wildcard cells stay wild and count as the color of the mission being checked.
- Blank cells and power symbols do not score.
- One placement claims every completed secret mission of the acting seat, draws one replacement per claim, and then ends the turn.
- Setup composition: color catalog, non-scoring symbols, pattern list, and claims required to win. Today's ordinary catalog is four colors, blank, wild, rotate, stack, and bounce, with row, square, and L.
- Pack edits that record the wildcard ruling, the configurable win count, the grid, and same-turn multi-claim.

### Out of scope

- Executing rotate, stack-on-top, or bounce.
- A plugin loader, scripted rules, or a player screen that asks the table for the win count.
- `ISession`, `LocalSession`, `AiPlayer`, seat-scoped views, and `app/MissionSplat.App.sln`.
- Unity, the UPM package, a desktop build, the room service, and `RemoteSession`.
- A physical-box deck census.
- Any still-open ruling in [`docs/rules.md`](../../rules.md) other than the two this slice closes.

## Strategic traceability

| Strategic requirement or criterion | How this slice advances it |
| --- | --- |
| Rules stay engine-free | The rules project is `netstandard2.1` and a guard rejects a Unity reference. |
| Claim is placement-owned | Fixtures for a legal claim and for a pattern completed by another seat. |
| Corner-only placement is rejected | Fixture leaves the board unchanged. |
| Row, square, and L, including across tiles | One fixture per shape. Row covers three directions. L covers 8 orientations. The square fixture is the four-tile corner. |
| Blank and power symbols do not score | A pattern missing one scoring cell because of a blank or a power symbol does not claim. |
| Wildcard | A wild cell completes the mission color and can count as another color for another mission. |
| Claim, replacement, turn pass | Hand size is unchanged and the claim row grows by the number of missions that placement completed. |
| Win | The ordinary count of 4 wins at four and not at three. A different setup count wins at that count. Further commands after a win are rejected. |
| Setup at 2, 3, and 4 seats | Hand size 2, distinct mission ids, one starting tile, empty claim rows. |
| Representative deck | Fixtures name the deck as representative. The census gate stays open. |
| Occupied placement | A non-stack tile on an occupied cell is rejected. A stack-symbol tile may use a legal side. |
| Same-turn multi-claim | Human ruling on 2026-10-04. Both of the acting seat's completed missions are claimed. |
| CI | The workflow runs the rules test command. The app command is omitted until that solution exists. |

## Gates and dependencies

### Hard gates

- None inside the repository. The pack is on `origin/main`. The gitignore re-includes `/rules/**/*.csproj` and `/rules/**/*.sln`.

### Sequencing recommendations

- Bootstrap the solution and CI, then implement the grid and claims on that solution. The behavior issue can be one change if that stays easier to review.

### Closed by the human for this slice

- A wildcard is never assigned a color.
- One placement may complete more than one mission the acting seat holds. Each of those missions is claimed.

### Still open

- Deck census.
- How long a stack stays buried, bounce of the last tile, a drawn tile with no legal placement, claim by rotate, stack, or bounce, and an exhausted mission or match deck.
- This slice must not invent those rulings. Fixtures must avoid them.

## Architecture and contracts

- **Affected seams:** Rules, as the command and event boundary. App and Unity are not created.
- **Public contracts:** Setup and place. A rejected placement leaves the board unchanged. A successful place can return more than one claim, then a win when the configured count is met. There is no wildcard-chosen event.
- **Extension:** Colors, non-scoring symbols, patterns, and the win count are setup inputs. Adding one later means passing it in that setup. This slice does not add a loader.
- **Data and migration considerations:** None. There is no saved game.

## High-level approach

Fixtures build a scripted representative deck and a small board. Setup places the starting tile and deals two missions. It does not claim. A placement writes four cells onto the grid. Claim detection looks only at that grid and only at missions the acting seat holds. A wildcard matches the mission color under check. Cells written by earlier tiles do not by themselves award a claim to the seat who plays next.

On one successful placement the acting seat claims every held mission that placement completed, in a single resolution. Each claim moves that mission to the claim row and draws one replacement. The turn then ends, so a replacement that already matches the board is not claimed until a later placement completes it. The win check runs after those claims. Reaching the configured count wins even when two claims cross it together. Both claims from the placement still count.

The ordinary catalog and a win count of 4 are what the standard fixtures pass in. A second fixture passes a different count. Pattern matching compares cell colors with the mission color. It does not branch on a fixed list of color names. Power symbols are stored as data and do not score. Their effects are not executed.

The behavior issue updates the strategic docs that still say the placer names a wildcard color, that the win count is fixed, and that two simultaneous missions are an open edge.

## Verification

- `dotnet test rules/MissionSplat.Rules.sln --configuration Release`
- `dotnet test app/MissionSplat.App.sln --configuration Release` is omitted. That solution does not exist. The PR records the omission.
- The rules project does not reference Unity.
- The new solution and project files are visible to git.
- No device or Unity evidence in this slice.

## Risks and stop conditions

- Cross-tile geometry is the expensive rule. Stop if a fixture can pass only by weakening an acceptance check.
- Stop if a command would exhaust a deck, discard an unplaceable tile, execute a power, or decide whether a power effect may claim.
- Stop if the rules project needs a Unity reference, a production dependency, or a command beyond setup and place.
- Stop if implementation needs a plugin loader to satisfy the extension ruling. Setup inputs are the approved seam.

## Execution issues

GitHub issues are the WIP tracker and source of task-level detail.

| Issue | Purpose | Dependencies |
| --- | --- | --- |
| [#2](https://github.com/cblack34/mission-splat/issues/2) — Bootstrap the rules solution and CI | Create the rules solution, the Unity-reference guard, and the Actions workflow. | None |
| [#3](https://github.com/cblack34/mission-splat/issues/3) — Match claims on the splat grid | Setup, placement, grid matching, wild cells, multi-claim, configurable win count, and the pack edits. | #2 |

## Delivery shape

- **Topology:** Direct PR to `main`.
- **Branch or spine:** `feat/rules-claim-fixtures`, cut from `origin/main`.
- **Final PR:** [#5](https://github.com/cblack34/mission-splat/pull/5)
- **Human merge gate:** Only the human may physically merge the final PR to `main`. Agents must stop when it is ready.

## Amendments

None.

## Delivery record

- **Outcome:** The rules library deals a 2–4 seat game, places a tile on the cell grid, and claims every secret mission the acting seat's placement completed. A wildcard stays wild. The ordinary win count is 4, and setup can pass another count. CI runs the rules tests.
- **Verification:** `dotnet test rules/MissionSplat.Rules.sln --configuration Release` passed, 75 tests, 0 failed. The `rules` check is green on `9c246d2`. This receipt update is the following commit. The app solution does not exist, so that command was omitted. Evidence is on #2, #3, and the pull request.
- **Deviations:** CI landed in this slice because neither test solution existed. The base-deck census from `main` is included. Fixtures use smaller named decks and do not encode the 30 match tiles.
- **Unresolved gates or risks:** Power effects, how long a stack stays buried, bounce of the last tile, a tile with no legal placement, and deck exhaustion. Exhaustion throws `UnresolvedRulingException` and does not change the game. Uncorrected census rows still need a maintainer pass.
- **Final PR:** https://github.com/cblack34/mission-splat/pull/5
- **Merge state:** Ready for the human to merge after CI is green and review is addressed. Agents do not merge to `main`.
