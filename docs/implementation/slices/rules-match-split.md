# Slice plan — Rules match split

## Strategic source

- **Active build pack:** [`docs/build-brief.md`](../../build-brief.md), [`docs/rules.md`](../../rules.md), [`docs/architecture.md`](../../architecture.md), [`docs/acceptance.md`](../../acceptance.md), [`docs/engineering/workflow.md`](../../engineering/workflow.md), [`docs/engineering/code-quality.md`](../../engineering/code-quality.md)
- **Human approval:** Approved in session on 2026-10-10, after [`rules-turn-actions.md`](rules-turn-actions.md) merged. The maintainer approved extracting the stateless rules engine from `Game` as an internal type, keying the turn phase by power, grouping each power's legality and effect in its own part of the engine, and keeping the type name `Game` for the match.
- **Final acceptance advanced:** None directly. This slice changes no behavior; it makes the architecture boundary in [`docs/build-brief.md`](../../build-brief.md) ("`Rules` owns two parts with a named seam") true in code so the session slice builds on a stable `Game`.

## Outcome

`Game` is the match: seats, hands, both decks, the grid, the current seat, the turn phase keyed by power, the win, and the bookkeeping that turns an accepted action into the next match and its events. A separate internal stateless type is the rules engine: constructed from the symbols and patterns in play, it answers every legality and completion question from a board, the drawn tile, the uses spent, and a hand, and references no seat, deck, or turn. Each power's legality and effect lives in its own part of that engine. The public surface of the Rules library is unchanged, and the 176 existing tests pass without modification.

## Why this slice is next

The corrected turn is on `main`, and the merged `Game.cs` already draws the line internally: a dozen private helpers read only the grid, the setup's symbols and patterns, a tile, and a count, while the rest is bookkeeping. Naming that line before the session slice depends on `Game` means that slice sees a stable shape. It also makes the maintainer's description literal — the match knows everything, the rules engine knows portions — and closes the two design-review items deferred from the previous slice (the "not rotate means bounce" reading of the phase, and per-power grouping).

## Scope

### In scope

- An internal rules-engine type built from `GameSetup.NonScoringSymbols` and `ActivePatterns`.
- The legality and completion helpers moved into it with `Grid`, `Tile`, spent uses, and hand as arguments; the same rejection reasons and message text.
- `Game` delegating every judgment and keeping state, orchestration, resolution, dealing, queries, and reads; public signatures unchanged.
- The turn phase keyed by `SymbolId`.
- Each power's legality and effect grouped in its own part of the engine.
- Engine-level unit tests that pin the seam.
- The `docs/architecture.md` Shipped-state bullet about the engine and match being one class.

### Out of scope

- Renaming `Game` to `Match`. The docs already say "the match, `Game`"; the rename is churn without behavior.
- Any change to `ISession`, `SeatView`, `IPlayer`, App, or the Unity player; the session-boundary and Unity lags are later slices.
- A power registry, plugin loading, or a public engine API. Three powers and one caller do not justify it.
- The open rulings.

## Strategic traceability

| Strategic requirement or criterion | How this slice advances it |
| --- | --- |
| `Rules` owns two parts with a named seam | The engine is a type; the match is `Game`; the seam is the engine's argument list. |
| Rules stay engine-free; a server can judge a move | Unchanged: both parts stay in the Rules library and the server still talks to `Game`. |
| Each power's legality and effect lives in its own part of the engine | Grouped per power inside the engine, with the phase keyed by power so a fourth power touches only compiler-visible places. |
| Code quality: one home per concept, IO at the edges, testability | Judgment has one home; the engine is pure and tested without a match. |

## Gates and dependencies

### Hard gates

- The corrected turn (PR #25) is on `main`. It is.

### Sequencing recommendations

- This slice before the session slice, so `ISession` is built against a `Game` that will not move again.

## Architecture and contracts

- **Affected seams:** Rules' internal structure only. `Game` → engine is a new internal call seam.
- **Public contracts:** None change. `Game`'s members, `GameAction`, `GameEvent`, `CommandResult`, `Rejection`, `LegalPlacement`, `BoardPosition`, `VisibleTile` keep their signatures and behavior.
- **Data and migration considerations:** None.

## High-level approach

Create the engine from the setup data `Game` already carries, move the judgment helpers across one group at a time (placement, rotate, bounce, completion), and have `Game` call them with the grid, the drawn tile, the spent uses, and the acting hand. Replace the two-field phase with a structure indexed by power so each power's spent count is looked up, not branched on. Run the full suite after each group; the suite is the behavior-preservation check.

## Verification

- `dotnet test rules/MissionSplat.Rules.sln --configuration Release` with the 176 existing tests unmodified, plus the engine tests.
- `dotnet test app/MissionSplat.App.sln --configuration Release`.
- A grep showing the engine file references no seat, deck, turn, or `Game` member.
- Repository definition-of-done commands remain mandatory.

## Risks and stop conditions

- Stop if any public member must change shape, or if a test must be edited to pass; either means the split is not behavior-preserving as approved.
- Low risk otherwise: the split follows the private-helper boundary already present in the file.

## Execution issues

GitHub issues are the WIP tracker and source of task-level detail. Every issue for this slice carries the `slice:rules-match-split` label; this plan links the query, not the issues: https://github.com/cblack34/mission-splat/issues?q=label%3Aslice%3Arules-match-split

## Delivery shape

- **Topology:** Direct PRs to `main`.
- **Human merge gate:** Only the human may physically merge any PR whose base is `main`. Agents must stop when it is ready.

### Direct PRs

- **Branch:** `refactor/rules-match-split`, cut from `origin/main` at `05acb4e`.

## Amendments

None.

## Delivery record

Complete once when the final PR is ready for human merge. Do not use this section for WIP status.
