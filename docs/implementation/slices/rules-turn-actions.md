# Slice plan — Rules turn actions

## Strategic source

- **Active build pack:** [`docs/build-brief.md`](../../build-brief.md), [`docs/rules.md`](../../rules.md), [`docs/architecture.md`](../../architecture.md), [`docs/acceptance.md`](../../acceptance.md), [`docs/engineering/workflow.md`](../../engineering/workflow.md), [`docs/engineering/code-quality.md`](../../engineering/code-quality.md)
- **Human approval:** Approved in session on 2026-10-09, after the pack correction in [`pack-power-timing.md`](pack-power-timing.md) merged. The maintainer approved one `Apply(seat, action)` on `Game` with a closed action set, the turn phase held in Rules, stack folded into `Place`, the origin-only rule for an empty board, one event per power use, and the three legal-move queries, with the `Game` split and every App or Unity change left to later slices.
- **Final acceptance advanced:** The rotate, multiple-use, stack, bounce, empty-board, and power-without-claim lines in [`docs/acceptance.md`](../../acceptance.md), the omitted-power line, and the "a command after the win is rejected" and setup lines as they apply to `Apply`.

## Outcome

The rules library plays the turn `rules.md` describes. `Game.Apply(seat, action)` accepts `UseRotate`, `UseBounce`, or `Place`, rejecting an action from a seat that is not current. Rotate and bounce act before the placement on tiles already on the board, one use per cell on the drawn tile, each emitting its own event; a rejected action returns the same match. `Place` on an occupied position is the stack placement when the drawn tile shows stack; an empty board accepts only the origin. Read-only queries report the legal placements at an orientation, the legal targets for a power, and the remaining uses, and they agree with `Apply`. `LocalSession` calls `Apply` for its one placement; nothing else outside Rules changes.

## Why this slice is next

The pack now states the physical game's order; the shipped `Game` still spends rotate and bounce after the placement, rejects self-bounce, and offers four placement commands. Every later slice — the match/rules-engine split, the App session and turn driver, the power UI — builds on this command surface, and because rotate and bounce were never wired past Rules, this is the entire blast radius of the behavior correction. The queries belong here because they are the rules engine's answer to "where may this go," and the GUI and AI must stop computing it themselves in the next slice.

## Scope

### In scope

- A closed, typed action set and `Game.Apply(SeatId, action)` replacing `Place`, `Stack`, `PlaceWithRotates`, and `PlaceWithBounces`. Out-of-turn rejection moves into Rules.
- The turn phase: remaining uses per power on the drawn tile, reset by an accepted placement.
- Rotate and bounce before the placement, judged on the board as it is; no self-bounce rule; a bounce may empty the board; `Place` on an empty board only at the origin.
- Stack as `Place` on an occupied position when the drawn tile shows stack.
- Powers in play are the setup's listed symbols; an unlisted power cell is a blank.
- `TileRotated`, `TileBounced`, and `TilePlaced` noting what it covered. Closes the deferred power-event issue.
- `LegalPlacements(quarterTurns)`, `LegalTargets(symbol)`, `RemainingUses(symbol)`.
- The rotate, bounce, stack, placement, resolution, and win tests rewritten against `Apply`, with the shared helpers consolidated in `Fixtures.cs`. Closes the deferred test-helper issue.
- `LocalSession`'s two `Game` calls, and removal of its now-redundant not-your-turn check.
- `docs/architecture.md`'s Shipped-state bullet about `Game`.

### Out of scope

- Splitting `Game` into the match and a stateless rules engine. Next slice.
- Any change to `ISession`, `IPlayer`, `SeatView`, `AiPlayer`, the turn driver, deck content, or the Unity player. `LocalSession._placed` stays until the session slice, where bounce first becomes reachable through `ISession`.
- Power UI.
- The census, deck exhaustion, a drawn tile with no legal placement on a non-empty board, and a tile showing two different powers. Still open.

## Strategic traceability

| Strategic requirement or criterion | How this slice advances it |
| --- | --- |
| Rules in `rules.md` are the behavioral contract | `Apply` implements the Turn, Placement, Claim, and Powers sections as written after the correction. |
| Claim is placement-owned | Unchanged: claims read the placed tile's written cells; a fixture shows a power-formed pattern is not claimed. |
| Rules stay engine-free | Everything changes inside the Rules library; `dotnet test` judges it. |
| The match accepts one action type per ruleset | The action set is typed records; `Apply` is the one entry point. |
| The GUI never decides legality | The three queries are the source the GUI and AI will consume in the session slice. |
| A rejected action returns the same match | Fixtures assert board, charges, pending tile, seat, and decks unchanged on rejection. |

## Gates and dependencies

### Hard gates

- The corrected pack is on `main` (PR #22). It is.
- `Grid.TurnClockwise`, `Grid.Bounce`, and `Grid.Cover` exist and are order-independent; only their caller's order changes.

### Sequencing recommendations

- `Apply`, the phase, the events, and the test rewrite first; the queries and helper consolidation second, on the same branch. The split follows in its own slice.

## Architecture and contracts

- **Affected seams:** Rules' public command surface. `Game` gains the action set, two events, one event field, three queries, and the phase; loses four methods.
- **Public contracts:** `CommandResult`, `Rejection`, `GameSetup`, `GameEvent` base, `Tile`, `Cell`, ids: unchanged. `ISession`, `IPlayer`, `SeatView`: unchanged. `LocalSession` is an adapter whose two `Game` calls change.
- **Data and migration considerations:** None. There is no saved game.

## High-level approach

Replace the four placement commands with one `Apply` that dispatches on the action type. Power uses validate the target against the current grid and the remaining charge, apply one `Grid` operation, decrement the charge, and return the same game advanced by that one step with one event. `Place` validates against the empty-board, side-sharing, or cover rule the position calls for, writes the cells, resolves claims from the written cells as today, advances the deck and the turn, and resets the phase. The queries reuse the same validation so they cannot disagree with `Apply`. Tests move to the shared helpers as they are rewritten.

## Verification

- `dotnet test rules/MissionSplat.Rules.sln --configuration Release`
- `dotnet test app/MissionSplat.App.sln --configuration Release`
- A fixture per amended acceptance line, named for it, as listed in the issues.
- No remaining call site for the deleted commands anywhere in the repository.
- Repository definition-of-done commands remain mandatory.

## Risks and stop conditions

- Stop if the phase forces an `ISession` or `SeatView` change; those belong to the session slice.
- Stop if a fixture needs a ruling `rules.md` does not give. Return it to the maintainer.
- Stop if `Grid` needs a new operation to support the pre-placement order; the approved change is to the caller's order only.
- Mid-turn state is new. Mitigation: the rejection fixtures assert every component of the match is unchanged, and the accepted-use-survives-rejected-placement fixture pins the phase.

## Execution issues

GitHub issues are the WIP tracker and source of task-level detail. Every issue for this slice carries the `slice:rules-turn-actions` label; this plan links the query, not the issues: https://github.com/cblack34/mission-splat/issues?q=label%3Aslice%3Arules-turn-actions

## Delivery shape

- **Topology:** Direct PRs to `main`.
- **Human merge gate:** Only the human may physically merge any PR whose base is `main`. Agents must stop when it is ready.

### Direct PRs

- **Branch:** `feat/rules-turn-actions`, cut from `origin/main` at `91e7347`.

## Amendments

None.

## Delivery record

Complete once when the final PR is ready for human merge. Do not use this section for WIP status.

- **Outcome:** `Game.Apply(seat, action)` over the closed set `UseRotate`, `UseBounce`, `Place` replaces the four placement commands. Rotate and bounce act before the placement, one in-play cell per use, on board tiles only, each with its own event; the drawn tile is never a target; a bounce may empty the board and the origin is then the only placement; a rejected action returns the same game. Stack is `Place` on an occupied position and `TilePlaced` records what it covered. An unlisted power symbol is a blank. `LegalPlacements`, `LegalTargets`, and `RemainingUses` share the legality decisions with `Apply`. `LocalSession` calls `Apply`; nothing else outside Rules changed.
- **Verification:** On `a24bd45` (after the first PR review round), `dotnet test rules/MissionSplat.Rules.sln --configuration Release` passed 176 (was 135) and `dotnet test app/MissionSplat.App.sln --configuration Release` passed 16 (was 15), both run by the implementation lead independently of the execution agents' receipts. No call site of the removed commands remains; Rules and App reference no Unity type; the Unity player references none of the changed types. A fresh-context design review on `c14e4d2` covered all nine production files; four of seven suggestions were applied, one recorded as a deviation, two rejected with evidence in the PR. The PR review then found two defects, both fixed: a stacked placement duplicated the session's board view, and a tile showing two different powers silently resolved an open ruling.
- **Deviations:** `LocalSession` keeps its not-your-turn check because an app test pins a session-level rejection; Rules now rejects it too, and the session slice decides which stays. `SetupCheck` accepts an unlisted rotate, stack, or bounce symbol as a blank so the omitted-power acceptance line is testable. `Grid.LayerCountAt` and `Grid.Overlaps` were deleted as dead, and `Grid.Cover`/`Bounce` now return the covered or revealed id. `RejectionReason` was consolidated. The `LocalSession._placed` fix listed out of scope was pulled in: once `Apply(Place)` accepts an occupied position, stack is reachable through `ISession.Place` and `_placed` duplicated the covered tile, so the board view now comes from `Game.Tiles` as the target architecture requires. A drawn tile showing two different in-play powers now throws `UnresolvedRulingException` from `Apply`, the repository's convention for an open ruling, and the queries return nothing for it; the earlier fixtures that let both powers be used were replaced. `GameAction` is closed by an `internal abstract` member, since an internal constructor does not stop another assembly deriving a record.
- **Unresolved gates or risks:** The match/rules-engine split is the next slice. Whether an omitted power's cell should render as a blank in the player, or keep its printed face as it does now, is the maintainer's call for the GUI slice; the rules behavior is identical either way. The census, deck exhaustion, a drawn tile with no legal placement on a non-empty board, and a tile showing two different powers stay open.
- **Refactor and handoff receipt:** In the PR body, https://github.com/cblack34/mission-splat/pull/25, and on issues #23 and #24.
- **Final PR:** https://github.com/cblack34/mission-splat/pull/25
