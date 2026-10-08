# Slice plan — Rules rotate

## Strategic source

- **Active build pack:** [`docs/build-brief.md`](../../build-brief.md), [`docs/rules.md`](../../rules.md), [`docs/architecture.md`](../../architecture.md), [`docs/acceptance.md`](../../acceptance.md), [`docs/engineering/workflow.md`](../../engineering/workflow.md), [`docs/engineering/code-quality.md`](../../engineering/code-quality.md)
- **Human approval:** Approved in session after pull request #14 merged. Rotate is the only power in this slice. Claims stay on the cells the placed tile wrote. If that same tile is rotated, the claim sees its cells after the turn. Rotating any other tile does not award a claim.
- **Final acceptance advanced:** The rotate checks in [`docs/acceptance.md`](../../acceptance.md): a free-side rotate changes only that tile's cells, a completely surrounded rotate is rejected, two rotate cells allow two rotates and a third is rejected, and a rotate symbol already on the board cannot be spent. The stack checks already shipped stay as they are.

## Outcome

The rules library can turn a tile that is not completely surrounded, once for each rotate cell on the tile just drawn. The turn claims from the cells the placed tile wrote after that rotation, then passes once. The three-argument `Place` still declines rotate.

## Why this slice is next

Stack is on `main`. Bounce removes a tile and still has an open last-tile ruling. Rotate is one command, and completely surrounded already means four orthogonal neighbors. The table keeps calling `Place`, so this slice must not hold that command's turn open.

## Scope

### In scope

- A rotate-bearing placement, separate from the three-argument `Place` and from `Stack`. It sets the drawn tile on an empty full-side square, applies that tile's rotate uses, then claims and passes the turn.
- One use per rotate cell on the drawn tile. Each use is one, two, or three clockwise quarter-turns of one board tile that has a free orthogonal side.
- A surrounded tile is rejected, and the whole command leaves the game unchanged.
- A rotate symbol already on the board adds no use.
- The claim reads the placed tile's four cells after the rotates. Another tile's rotated cells are not added to that written set.
- The rules and architecture sentences that still describe rotate as unplayed.

### Out of scope

- Bounce, uncover, and any command that holds the turn open after the three-argument `Place`.
- Rotate uses on `Stack`. A tile with both symbols can stack without rotating, or take a side and rotate. It cannot do both in one command here.
- `ISession`, `IPlayer`, `AiPlayer`, and the Unity table.
- Census encoding, a desktop build, and device play.

## Strategic traceability

| Strategic requirement or criterion | How this slice advances it |
| --- | --- |
| Free-side rotate | A fixture turns a tile with an open orthogonal side, and only that tile's cells change. |
| Surrounded rotate rejected | Four orthogonal neighbors reject the use and leave the game unchanged. |
| One use per rotate cell | Two rotate cells allow two rotates. A third is rejected. |
| Board symbols are spent | A rotate symbol already on the board adds no use. |
| Claim is placement-owned | The written set is the placed tile's four cells after the turn. |
| Rules stay engine-free | The change stays in the rules library. |

## Gates and dependencies

### Hard gates

- Orthogonal placement, the written-cell claim rule, and `OrdinaryCatalog.Rotate` are on `main`.
- Completely surrounded means all four orthogonal neighbors are occupied.

### Sequencing recommendations

- None. This slice is one rules change.

### Closed by the human for this slice

- Rotate uses are spent before the claim, inside the placement that drew the tile.
- The three-argument `Place` declines those uses and still passes the turn.
- Rotating a tile other than the one just placed does not award a claim from that other tile's cells.

### Still open

- Bounce, including bounce of the last tile.
- Whether uncovering a stack reveals the covered tile.
- A pattern completed by stack or bounce rather than by the placed tile's own cells.
- A drawn tile with no legal placement, an exhausted mission or match deck, and the uncorrected census rows.

## Architecture and contracts

- **Affected seams:** Rules. `LocalSession` remains the session adapter and does not gain a rotate command.
- **Public contracts:** `Place(int, int, int)` and `Stack` are unchanged. `Game` gains a rotate-bearing placement. `ISession`, `IPlayer`, and the seat view stay as they are. No new event type.
- **Data and migration considerations:** None. There is no saved game. A rotate does not move a tile, change `TileCount`, or disturb covered tiles under a stack.

## High-level approach

The three-argument `Place` declines rotate, rejects overlap, claims from the cells it wrote, and passes the turn. A separate placement uses the same orthogonal rules, then turns one board tile per rotate cell on the drawn tile, then claims. Every use is checked before the game changes. An illegal use returns the same game instance.

`docs/rules.md` records the surrounded rule and the claim rule above. Bounce stays unresolved. `docs/architecture.md` records rotate as part of the placement that spends the drawn tile's rotate cells. Bounce stays a later command.

## Verification

- `dotnet test rules/MissionSplat.Rules.sln --configuration Release`
- `dotnet test app/MissionSplat.App.sln --configuration Release`
- New fixtures, on a named smaller deck, for a free-side rotate, a surrounded rejection, two uses from two rotate cells, and a board rotate symbol that cannot be spent.
- No Unity run in this slice. The player does not change.

## Risks and stop conditions

- Stop if rotate only works by keeping the three-argument `Place` from passing the turn.
- Stop if a rotate of some other tile has to award a claim from that tile's cells.
- Stop if `ISession` or the Unity table has to change to keep the current game compiling.
- Stop if the slice has to bounce, uncover, or combine stack and rotate in one command.

## Execution issues

GitHub issues are the WIP tracker and source of task-level detail.

| Issue | Purpose | Dependencies |
| --- | --- | --- |
| [#15](https://github.com/cblack34/mission-splat/issues/15) — Rotate the drawn tile's charges before that placement claims | Rotate-bearing placement, surrounded rejection, charge counts, and the rules and architecture sentences. | None |

## Delivery shape

- **Topology:** Direct PR to `main`.
- **Branch or spine:** `feat/rules-rotate`, cut from `origin/main`. Upstream is unset so a plain push cannot target `main`.
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
