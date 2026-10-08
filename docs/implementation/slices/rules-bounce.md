# Slice plan — Rules bounce

## Strategic source

- **Active build pack:** [`docs/build-brief.md`](../../build-brief.md), [`docs/rules.md`](../../rules.md), [`docs/architecture.md`](../../architecture.md), [`docs/acceptance.md`](../../acceptance.md), [`docs/engineering/workflow.md`](../../engineering/workflow.md), [`docs/engineering/code-quality.md`](../../engineering/code-quality.md)
- **Human approval:** Approved in session on 2026-10-08. Bounce is the only power left unplayed. Bouncing a stacked position peels only its top tile and reveals the tile beneath with its original cells restored. Self-bounce is rejected via `CommandResult.Reject`, the same mechanism as `TileSurrounded` or `NoTileToRotate` — it is a resolved illegal move, not a gap `UnresolvedRulingException` is reserved for. Board-emptying is consequently unreachable and has no check in code.
- **Final acceptance advanced:** The bounce check in [`docs/acceptance.md`](../../acceptance.md): board count drops by one on a single-layer bounce, the removed tile is the last element of the draw deck, and the order of the other deck tiles is unchanged. A new sentence on the same line covers the stacked case: tile count is unchanged and the layer beneath becomes visible with its original cells. Rotate and stack stay as they are.

## Outcome

The rules library can remove one board tile per bounce cell on the tile just drawn, after that placement and before claims. Bouncing a single-layer position removes it and sends that exact tile to the bottom of the draw deck. Bouncing a stacked position peels only its top tile to the deck and reveals the tile beneath with the cells it had when it was covered. The turn claims from the cells the placed tile wrote, then passes once. The three-argument `Place`, `Stack`, and `PlaceWithRotates` are unchanged and still decline bounce.

## Why this slice is next

Stack (#14) and rotate (#16) are on `main`. Bounce is the only power with no code. It is one command, on the same pattern as `PlaceWithRotates` and `Stack`, and its remaining open rulings (stacked bounce, self-bounce) are now closed by the human so the slice can proceed without inventing them.

## Scope

### In scope

- A bounce-bearing placement, separate from `Place(int, int, int)`, `Stack`, and `PlaceWithRotates`. It places the drawn tile on an empty full-side square, applies that tile's bounce uses, then claims and passes the turn.
- One use per bounce cell on the drawn tile. Each use names a board tile position to remove.
- Charges come only from the tile just drawn. A bounce symbol already on the board adds no use.
- A single-layer target is removed from the board entirely; the removed tile is the exact `Tile` that was placed there, appended to the bottom of the draw deck. The other deck tiles keep their order. The board's tile count at that position drops to zero (the position itself disappears).
- A stacked target has only its top tile removed to the bottom of the draw deck. The tile immediately beneath becomes visible, restored to the cells it had at the moment it was covered — not re-derived from its original placement orientation, since a rotate can happen between a tile's placement and it being covered. If further tiles remain buried beneath the newly revealed one, they stay buried. The board's tile count at that position is unchanged.
- The tile the seat just placed this turn is never a legal bounce target (self-bounce), regardless of whether it covered another tile. That use is rejected and the whole command leaves the game unchanged.
- Every use is checked before the game changes. An illegal use (no tile at that position, or the just-placed tile) rejects the whole command and returns the same game instance.
- The rules and architecture sentences that still describe bounce as unplayed, and the acceptance sentence covering the stacked case.

### Out of scope

- Uncovering a position by any command other than this bounce. There is still no separate "reveal" command.
- Rotate or stack combined with bounce in one command. A tile with both a bounce cell and a rotate or stack cell spends one kind per command, the same deviation the rotate slice already took.
- `ISession`, `IPlayer`, `AiPlayer`, and the Unity table. They keep calling `Place` and decline every power.
- A drawn tile with no legal placement, deck exhaustion, and the uncorrected census rows.
- Board-emptying bounce. With self-bounce rejected, the just-placed tile always remains as at least one single-layer position, so no combination of bounce charges can remove every position. This closes rules.md's "bounce of the last remaining tile" as moot rather than as a thrown case to test.

## Strategic traceability

| Strategic requirement or criterion | How this slice advances it |
| --- | --- |
| Bounce removes a tile to the bottom of the draw deck | A fixture on a single-layer position shows the board count drop by one, the bounced tile last in the deck, and the other deck tiles in order. |
| One use per bounce cell | Two bounce cells allow two bounces. A third is rejected. |
| Board symbols are spent | A bounce symbol already on the board adds no use. |
| Stacked bounce reveals the layer beneath | A fixture stacks, then bounces the top; the revealed tile's cells match what it had when covered, and the board's tile count there is unchanged. |
| Self-bounce rejected | A command naming the just-placed tile's own position as a bounce target is rejected and leaves the game unchanged. |
| Claim is placement-owned | The written set stays the placed tile's four cells; a bounce does not add or remove from that set. |
| Rules stay engine-free | The change stays in the rules library. |

## Gates and dependencies

### Hard gates

- Orthogonal placement, the written-cell claim rule, `OrdinaryCatalog.Bounce`, `Stack`, and `PlaceWithRotates` are on `main`.
- The human-approved rulings above: stacked bounce peels and reveals; self-bounce is rejected; board-emptying is consequently unreachable and is not a case to implement a check for.

### Sequencing recommendations

- None. This slice is one rules change.

## Architecture and contracts

- **Affected seams:** Rules. `Grid` changes its internal storage to support a reveal: `_tiles` stores the `Tile` object (not just `TileId`) so a single-layer bounce can hand the exact tile back to the deck, and `_covered` stores each buried layer's `Tile` together with a snapshot of the four cell values it had at the moment it was covered, so a reveal restores those cells exactly rather than re-deriving them from placement-time orientation. `CoveredTileIds` keeps returning plain `TileId` values; that public read does not change. `LocalSession` remains the session adapter and does not gain a bounce command.
- **Public contracts:** `Place(int, int, int)`, `Stack`, and `PlaceWithRotates` are unchanged. `Game` gains a bounce-bearing placement. `ISession`, `IPlayer`, and the seat view stay as they are. No new event type.
- **Data and migration considerations:** None. There is no saved game. A single-layer bounce removes a tile position; `TileCount` reflects that. A stacked bounce does not change `TileCount`.

## High-level approach

The three-argument `Place` declines bounce, rejects overlap, claims from the cells it wrote, and passes the turn. A separate bounce-bearing placement uses the same orthogonal rules, then for each bounce use removes one board tile: if that position has no buried layer beneath it, the position is deleted and its tile goes to the bottom of the match deck; if it has a buried layer, only the top is removed to the deck and the layer beneath becomes visible using its covered-time cell snapshot. The just-placed tile's own position is rejected as a target before any removal happens. Every use is validated before the game changes, then claims resolve once from the placed tile's written cells.

`docs/rules.md` records the stacked-bounce and self-bounce rulings and removes "bounce of the last remaining tile" and "whether a stack buries the covered tile forever" from the open list — the first is unreachable and the second is answered: only while covered. `docs/architecture.md` records bounce as the placement that spends the drawn tile's bounce cells. `docs/acceptance.md` gains one sentence on the existing bounce line covering the stacked case; the existing single-layer check is unchanged.

## Verification

- `dotnet test rules/MissionSplat.Rules.sln --configuration Release`
- `dotnet test app/MissionSplat.App.sln --configuration Release`
- New fixtures, on a named smaller deck, for: a single-layer bounce (count drops by one, bounced tile last in the deck, other deck tiles in order); two bounce cells allow two bounces and a third is rejected; a bounce symbol already on the board cannot be spent; a bounce naming a position with no tile is rejected; two bounce charges on one stacked position peel two layers, each reveal's cells correct; a bounce naming the just-placed tile's own position is rejected and leaves the game unchanged.
- No Unity run in this slice. The player does not change.

## Risks and stop conditions

- Stop if bounce only works by making the three-argument `Place`, `Stack`, or `PlaceWithRotates` hold the turn open differently than they do today.
- Stop if a reveal has to be re-derived from placement-time orientation instead of a covered-time snapshot — that would require a wider `Grid` or `Tile` change than approved here.
- Stop if self-bounce turns out to be needed for a legal line of play discovered during fixtures; return to the human rather than relaxing the rejection.
- Stop if `ISession` or the Unity table has to change to keep the current game compiling.

## Execution issues

GitHub issues are the WIP tracker and source of task-level detail.

| Issue | Purpose | Dependencies |
| --- | --- | --- |
| [#17](https://github.com/cblack34/mission-splat/issues/17) — Bounce a board tile to the bottom of the draw deck before the placing turn claims | Bounce-bearing placement, single-layer removal, stacked-position reveal, self-bounce rejection, and the rules, architecture, and acceptance sentences. | None |

## Delivery shape

- **Topology:** Direct PR to `main`.
- **Branch or spine:** `feat/rules-bounce`, cut from `origin/main`. Upstream is unset so a plain push cannot target `main`.
- **Final PR:** Open when the issue is ready.
- **Human merge gate:** Only the human may physically merge the final PR to `main`. Agents must stop when it is ready.

## Amendments

None.

## Delivery record

Complete once when the final PR is ready for human merge. Do not use this section for WIP status.

- **Outcome:** The rules library removes one board tile per bounce cell on the tile just drawn, after that placement and before claims. A single-layer target is deleted and its exact `Tile` goes to the bottom of the draw deck, keeping the other deck tiles in order. A stacked target has only its top tile removed to the deck; the layer beneath becomes visible, restored to the cells it had when it was covered. Self-bounce is rejected. The turn claims from the placed tile's written cells, then passes once. `Place`, `Stack`, and `PlaceWithRotates` are unchanged and still decline bounce.
- **Verification:** On `33a43e9` (after PR #18 review response), `dotnet test rules/MissionSplat.Rules.sln --configuration Release` passed 135, failed 0, and `dotnet test app/MissionSplat.App.sln --configuration Release` passed 15, failed 0, both re-run by the implementation lead independently of the responder's own receipt. CI is green on that head. A fresh-context design review (the plugin's `design-reviewer` agent) covered all four changed production files and both changed test files; two of its five suggestions were applied (`Grid.LayerCountAt`, a dead-condition removal in `Grid.Bounce`), three were rejected or deferred with evidence. Evidence is on #17. A subsequent PR review (medium effort, 4 lenses + fresh-eyes round, 12 candidates adversarially verified, reconciled against Copilot's independent review of the same head) added 4 tests pinning claim-evaluation behavior through bounce and closed 5 smaller test-coverage gaps; evidence is on PR #18's review threads.
- **Deviations:** Self-bounce is rejected via `CommandResult.Reject` (the `TileSurrounded`/`NoTileToRotate` mechanism), not `UnresolvedRulingException` as the approval-session wording literally said. The substance approved — self-bounce is illegal, game unchanged — is unchanged; only the delivery mechanism differs, because `UnresolvedRulingException` is reserved for a genuinely unresolved rules gap (deck exhaustion) and self-bounce is a resolved illegal move, matching every other move-legality rejection in `Game`. This slice's own scope section and issue #17's checklist already specified the rejection form; only two sentences elsewhere in this plan had the stale wording, corrected on `b5a30b9`. Flagging for the human to confirm on review rather than treating it as silently settled.
- **Unresolved gates or risks:** A drawn tile with no legal placement, mission-deck and match-deck exhaustion, and the uncorrected census rows stay open, unaffected by this slice. A pattern completed by stack or bounce rather than the placed tile's own cells is now exercised by two fixtures (`BounceRemovesAWouldBePattern_IsNotClaimed`, `RemoteBounceReveal_FormsAPatternMissingTheWrittenCells_IsNotClaimed`) confirming today's code declines the claim in both directions, but the open ruling itself is unresolved — a human still needs to decide whether that's the intended answer. Whether a bounce may disconnect the board, raised during PR review, is now closed: ruled allowed, documented in `docs/rules.md`'s Bounce bullet on `f666067`, no code change. `TileRotated`/`TileBounced` events and the cross-file test-helper duplication (`AssertTile`/`AssertRejected`) were both raised and deliberately deferred: the events gap is tracked as issue #19 (its own slice, motivated by an eventual authoritative server); the duplication is deferred to a DRY-up slice once rotate, stack, and bounce are all in place, with no issue opened yet since it isn't actionable until then. The player still calls only `Place`.
- **Refactor and handoff receipt:** https://github.com/cblack34/mission-splat/issues/17#issuecomment-6053064259
- **Final PR:** https://github.com/cblack34/mission-splat/pull/18
