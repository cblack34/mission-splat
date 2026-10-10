# Slice plan — Power UI

## Strategic source

- **Active build pack:** [`docs/build-brief.md`](../../build-brief.md), [`docs/rules.md`](../../rules.md), [`docs/architecture.md`](../../architecture.md), [`docs/acceptance.md`](../../acceptance.md), [`docs/engineering/workflow.md`](../../engineering/workflow.md), [`docs/engineering/code-quality.md`](../../engineering/code-quality.md)
- **Human approval:** Approved in session on 2026-10-10, after [`app-session-driver.md`](app-session-driver.md) merged. The maintainer approved a power phase driven by the App table (select a power, tap a target, choose the rotate amount on the existing quarter-turn row, place to end the window), the AI continuing to decline powers, out-of-play power cells keeping their printed face, and a GUI redesign as its own later slice before the completing device evidence.
- **Final acceptance advanced:** The build brief's in-scope line — a player that submits a tap as an action for every legal move the session reports — now covers rotate and bounce. [`docs/acceptance.md`](../../acceptance.md) Presentation gains the human-evidence line for the power phase. The Rules lines for rotate and bounce were already automated.

## Outcome

A human seat can use rotate and bounce from the table as the physical game goes: the drawn tile's powers and remaining uses are shown, the seat selects one, taps a highlighted target (and sets 1–3 quarter-turns for rotate), sees the board change, and repeats or places. Placing ends the power window. `Table` owns the selection and submits the use; the snapshot says which highlight set to draw; the Unity view draws it and forwards taps. All three powers are playable on the device.

## Why this slice is next

After the session slice, `TableSnapshot` already carries the legal targets per power and the remaining uses, and the Unity adapter already forwards any action. Only the choosing is missing: which power is active and how far a rotate turns. This is the last interaction the table must support, so it precedes the GUI redesign — a redesign before it would be drawn against an incomplete set of controls — and the completing device evidence.

## Scope

### In scope

- `Table.SelectPower(SymbolId?)` and `Table.UseAt(x, y)`; `TableSnapshot.SelectedPower`, `SelectedTargets`, and `CanAct`; the quarter-turn value doubling as the rotate amount while rotate is selected.
- A power panel in the Unity view listing each in-play use power with its remaining count; target highlights in a third tint; a way back to placement; status text for the phase.
- Play-mode tests through the rendered buttons, and captures of the panel and a bounce.
- `docs/acceptance.md`: one Presentation line with human evidence for the power phase.

### Out of scope

- The AI using powers. Rules make power use optional; the AI keeps placing.
- Rendering out-of-play power cells as blank. The maintainer chose the printed face; no code.
- Event-driven animation; the view re-renders from state.
- The GUI redesign (next slice), `RemoteSession`, the census, the open rulings.

## Strategic traceability

| Strategic requirement or criterion | How this slice advances it |
| --- | --- |
| The player submits a tap as an action for the legal moves the session reports | Target taps become `UseRotate`/`UseBounce` through `Table`; highlights come from `LegalTargets`. |
| The GUI never decides legality | The panel enables a power from `RemainingUses`; targets come from the session; Rules rejects a zero-turn rotate. |
| Powers are used before the placement, one at a time | The phase lives in the match; the table only selects and submits; placing ends it. |
| Pass-and-play on one device | The hand-off rule is unchanged; a power use never re-conceals. |

## Gates and dependencies

### Hard gates

- The session slice (PR #30) is on `main`. It is.

### Sequencing recommendations

- Driver first, then the view, on one branch, because the Unity project compiles against the rebuilt Release DLLs.

## Architecture and contracts

- **Affected seams:** `Table` and `TableSnapshot` in App; the Unity view. `ISession`, `IPlayer`, `SeatView`, and Rules are unchanged.
- **Public contracts:** `Table` gains two methods (`SelectPower`, `UseAt`); the snapshot gains `SelectedPower`, `SelectedTargets`, and `CanAct`. All are App types the Unity adapter already consumes.
- **Data and migration considerations:** None.

## High-level approach

Add the selection to the driver beside the quarter-turn choice: it is input state, not legality. When a power is selected the snapshot's highlight set is that power's targets; a tap on one submits the use through the same path as a placement, so the AI run, the hand-off rule, stops, and refresh apply unchanged; the selection clears when the use count reaches zero, on an accepted placement, on a stop, or when the seat changes. The view adds a panel of power buttons, a third highlight tint, a return to placement, and phase text, and the play-mode tests drive the rendered buttons end to end.

## Verification

- `dotnet test rules/MissionSplat.Rules.sln --configuration Release`
- `dotnet test app/MissionSplat.App.sln --configuration Release`
- `unity test unity/MissionSplat --mode PlayMode` on freshly copied Release DLLs, with a clean-import run.
- Captures of the power panel with targets highlighted and of the board after a bounce.
- Repository definition-of-done commands remain mandatory.

## Risks and stop conditions

- Stop if the view needs anything the snapshot cannot carry.
- Stop if reusing the quarter-turn row as the rotate amount reads badly in the capture; a separate 1–3 row is the fallback, GUI-only.
- Stop if a play-mode test needs a ruling `docs/rules.md` does not give.

## Execution issues

GitHub issues are the WIP tracker and source of task-level detail. Every issue for this slice carries the `slice:power-ui` label; this plan links the query, not the issues: https://github.com/cblack34/mission-splat/issues?q=label%3Aslice%3Apower-ui

## Delivery shape

- **Topology:** Direct PRs to `main`.
- **Human merge gate:** Only the human may physically merge any PR whose base is `main`. Agents must stop when it is ready.

### Direct PRs

- **Branch:** `feat/power-ui`, cut from `origin/main` at `5a41118`.

## Amendments

None.

## Delivery record

Complete once when the final PR is ready for human merge. Do not use this section for WIP status.

- **Outcome:** A human seat uses rotate and bounce from the table: the power band shows each in-play use power with its remaining uses; selecting one highlights only that power's targets; tapping a target applies the use through the same path as a placement; the quarter-turn row is the rotate amount while rotate is selected; placing ends the window. `Table` owns the selection (`SelectPower`, `UseAt`) and the snapshot carries `SelectedPower`, `SelectedTargets`, and `CanAct`, so the view draws and decides nothing. A rotate's amount is spent with it. All three powers are playable on the device; the AI keeps placing; out-of-play power cells keep their printed face.
- **Verification:** On `5f5fba4`, `dotnet test rules/MissionSplat.Rules.sln --configuration Release` passed 195, `dotnet test app/MissionSplat.App.sln --configuration Release` passed 56 (was 45; two repeat-use cases were added in review), and `unity test unity/MissionSplat --mode PlayMode` on editor 6000.3.16f1 passed 11 of 11 (four new, all through rendered buttons), run by the implementation lead independently of the execution agents' receipts. A clean import of `d8e9bbe` in a detached worktree with no `Library/` passed the same 11 with no tracked file altered. Greps confirm no `UnityEngine` in App or Rules, `MonoBehaviour` only in `TableSession` and `TableView`, and no `Game` type reference in Unity scripts. Captures of the power panel with targets highlighted and of the board after a bounce are tracked by LFS. A fresh-context design review of all six production files found no blocking or issue-level finding; five of seven suggestions were applied, two deferred to the GUI slice.
- **Deviations:** The pending-tile preview ignores the quarter-turn value while rotate is selected (the row is then the rotate amount); in bounce mode it follows the row. The power band is full-width under the top row and the board lost about 15% of its height. A test that pinned the rotate amount leaking into the placement orientation now pins the reset. Observed for the GUI slice, not changed here: the target tint muddies the cell colours beneath it; the lattice rescales between placement and target modes; a power's name is composed two ways; input events relay through three classes.
- **Unresolved gates or risks:** The GUI redesign is the next slice, before the completing device evidence. The census, deck exhaustion, no legal placement on a non-empty board, and a tile showing two different powers stay open.
- **Refactor and handoff receipt:** In the PR body, https://github.com/cblack34/mission-splat/pull/33, and on issues #31 and #32.
- **Final PR:** https://github.com/cblack34/mission-splat/pull/33
