# Slice plan — Rules stack

## Strategic source

- **Active build pack:** [`docs/build-brief.md`](../../build-brief.md), [`docs/rules.md`](../../rules.md), [`docs/architecture.md`](../../architecture.md), [`docs/acceptance.md`](../../acceptance.md), [`docs/engineering/workflow.md`](../../engineering/workflow.md), [`docs/engineering/code-quality.md`](../../engineering/code-quality.md)
- **Human approval:** Approved in session on 2026-10-07. Stack is the only power in this slice. The covered tile is retained. Uncovering it stays unresolved.
- **Final acceptance advanced:** The stack check in [`docs/acceptance.md`](../../acceptance.md): a drawn tile with stack, placed on an occupied position, matches the top tile's cells. The covered color no longer completes a pattern, and the top color does. Rotate, bounce, and the session and presentation checks that already shipped stay as they are.

## Outcome

The rules library can place a drawn stack tile on top of a tile already on the board. Matching at that position reads the top tile's cells. The covered tile stays underneath. The turn claims from the cells that placement wrote, then passes once.

## Why this slice is next

Claim fixtures, the local session, and the Unity table are on `main`. Rotate and bounce happen after the tile is down and need their own turn window. Stack is the placement itself, so the existing `Place` call can keep rejecting overlap, claiming, and passing the turn. The table and the session keep declining powers.

## Scope

### In scope

- An explicit stack placement, separate from the three-argument `Place`. That existing call still rejects an occupied cell, including when the drawn tile shows stack.
- One or more stack cells on the drawn tile allow that placement once. A stack tile can still take an ordinary full-side square.
- The top tile's four cells replace the covered cells for matching and can complete the acting seat's missions under the current written-cell rule.
- Every tile that position has covered stays underneath, in cover order. A later stack on the same position does not drop an earlier covered tile.
- A rules read of the covered tile ids, so retention is observable.
- The rules and architecture sentences that still describe stack-on-top as unplayed.

### Out of scope

- Rotate, bounce, and any post-placement power window.
- A command that removes the top tile or reveals a covered tile.
- `ISession`, `IPlayer`, `AiPlayer`, and the Unity table.
- Bounce of the last tile, a tile with nowhere legal, deck exhaustion, and census encoding.

## Strategic traceability

| Strategic requirement or criterion | How this slice advances it |
| --- | --- |
| Stack placement matches the top cells | A fixture covers an occupied position. The covered color no longer completes a pattern. The top color does. |
| A stack tile may still use an ordinary side | The three-argument `Place` stays rejected on overlap and accepted on a full side. |
| Extra stack cells do not grant a second placement | One drawn tile with more than one stack cell is consumed once, and the turn passes once. |
| Claim is placement-owned | The claim uses the four cells the stacked tile writes. |
| Covered tile retained | The covered tile ids remain readable. Uncover stays an open ruling. |
| Rules stay engine-free | The change stays in the rules library. |

## Gates and dependencies

### Hard gates

- Orthogonal placement, the written-cell claim rule, and `OrdinaryCatalog.Stack` are on `main`.
- Completely surrounded is not this slice. Stack does not depend on it.

### Sequencing recommendations

- None. This slice is one rules change.

### Closed by the human for this slice

- The covered tile is retained under the top tile.
- Rotate and bounce are not part of this slice.

### Still open

- Whether a later removal of the top tile reveals the covered tile or leaves it buried.
- Bounce of the last tile, a drawn tile with no legal placement, claim by rotate or bounce, claim by a stack effect other than the cells the stacked tile wrote, and an exhausted mission or match deck.
- Uncorrected census rows.

## Architecture and contracts

- **Affected seams:** Rules. `LocalSession` remains the session adapter and does not gain a stack command.
- **Public contracts:** `Place(int, int, int)` is unchanged. `Game` gains a stack placement and a read of the covered tile ids at a position. `ISession`, `IPlayer`, and the seat view stay as they are.
- **Data and migration considerations:** None. There is no saved game. A stack does not add a tile position. `TileCount` remains the count of positions.

## High-level approach

The three-argument `Place` declines stack, rejects overlap, claims from the cells it wrote, and passes the turn. A separate stack placement requires a stack cell on the drawn tile and an occupied position. It writes that tile's four cells as the top of that position, keeps the tiles already there underneath, consumes the drawn tile once, and then uses the same claim and turn-pass path as `Place`.

`docs/rules.md` records that matching reads the top cells and that the covered tiles remain. Uncover stays unresolved. `docs/architecture.md` records stack as a placement on the rules boundary. Rotate and bounce stay later commands.

## Verification

- `dotnet test rules/MissionSplat.Rules.sln --configuration Release`
- `dotnet test app/MissionSplat.App.sln --configuration Release`
- The existing occupied-cell tests still expect the three-argument `Place` to reject a stack tile.
- A new fixture, on a named smaller deck, shows the covered color no longer completing a pattern and the top color completing the acting seat's mission.
- No Unity run in this slice. The player does not change.

## Risks and stop conditions

- Stop if stacking only works by making the three-argument `Place` accept an overlap.
- Stop if the slice has to uncover a covered tile, play rotate or bounce, or award a claim from cells the stacked tile did not write.
- Stop if `ISession` or the Unity table has to change to keep the current game compiling.
- Deck exhaustion and a tile with nowhere legal stay open. Do not invent them.

## Execution issues

GitHub issues are the WIP tracker and source of task-level detail.

| Issue | Purpose | Dependencies |
| --- | --- | --- |
| [#13](https://github.com/cblack34/mission-splat/issues/13) — Stack the drawn tile and match the top cells | Stack placement, top-cell matching, retained covered tiles, and the rules and architecture sentences. | None |

## Delivery shape

- **Topology:** Direct PR to `main`.
- **Branch or spine:** `feat/rules-stack`, cut from `origin/main`. Upstream is unset so a plain push cannot target `main`.
- **Final PR:** Open when the issue is ready.
- **Human merge gate:** Only the human may physically merge the final PR to `main`. Agents must stop when it is ready.

## Amendments

None.

## Delivery record

Complete once when the final PR is ready for human merge. Do not use this section for WIP status.

- **Outcome:**
- **Verification:**
- **Deviations:**
- **Unresolved gates or risks:**
- **Final PR:**
- **Merge state:** Ready for the human to merge; agents do not merge to `main`.
