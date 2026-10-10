# Slice plan — App session driver

## Strategic source

- **Active build pack:** [`docs/build-brief.md`](../../build-brief.md), [`docs/rules.md`](../../rules.md), [`docs/architecture.md`](../../architecture.md), [`docs/acceptance.md`](../../acceptance.md), [`docs/engineering/workflow.md`](../../engineering/workflow.md), [`docs/engineering/code-quality.md`](../../engineering/code-quality.md)
- **Human approval:** Approved in session on 2026-10-10, after [`rules-match-split.md`](rules-match-split.md) merged. The maintainer approved `ISession` submitting a `GameAction` and forwarding the legal-move queries; the turn driver, render model, setup options, and deck content moving into App; `IPlayer` for automated seats only; `Game.PowersInPlay` as the one Rules addition; the shuffle toggle meaning a random seed versus the authored order; a typed status in the render model with text composed by the GUI; and the Unity player reduced to setup form, input adapter, and renderer.
- **Final acceptance advanced:** The Session lines in [`docs/acceptance.md`](../../acceptance.md): two to four human seats alternate submitting actions on `LocalSession`; the AI is asked for an action; the turn driver advances an AI seat without the Unity player. The Presentation lines keep their human evidence; the stack line's "the legal-placement query lists an occupied position as on-top" becomes visible in the player.

## Outcome

A GUI talks to the table through one action-shaped seam and draws one render model. `ISession` exposes start, the current seat, the powers in play, a seat's view, `Submit(seat, action)`, the legal placements and targets, and a preview. The App library owns the turn driver (which seats are automated, running them, the hand-off between humans, the chosen orientation, stops), the render model with a typed status, the setup options, and the named deck with a seeded shuffle. `AiPlayer` chooses from the session's legal placements. The Unity player builds the setup, forwards taps as actions, and draws the model; it computes no legality, runs no turn loop, holds no deck, and highlights every legal placement including on-top, so a stack is tappable. Rotate and bounce are reachable through the session; their target-selection UI is the next slice.

## Why this slice is next

It is the last Shipped-state lag in [`docs/architecture.md`](../../architecture.md) and the one that makes the maintainer's goal — swap the GUI without touching the game — true. `TableSession` is a 315-line `MonoBehaviour` holding the turn loop, AI driving, conceal logic, and a neighbor walk that guesses legality; `TableDeck` and `TableStart` are content and setup data inside the Unity project; `AiPlayer` has its own neighbor walk. `Game` now exposes `Apply`, the legal-move queries, and `Tiles`, so all of it can move behind `ISession` without inventing anything.

## Scope

### In scope

- The `ISession` contract above, implemented by `LocalSession` over one `Game`; the App `Placement` and `PlacementPreview` types replaced by `Rules.Place` and an action preview; the session's duplicate not-your-turn check removed.
- `SeatView` carrying the remaining uses per in-play use power.
- `IPlayer.ChooseAction` for automated seats; `AiPlayer` over the session's queries; `HumanPlayer` deleted.
- The turn driver, render model, `TableStart`, and `TableDeck` (seeded shuffle) in App.
- `Game.PowersInPlay`.
- App tests for the driver, the deck seed, and the rewritten session, view, pass-and-play, and AI tests.
- The Unity adapter, the view consuming the App model and highlighting every legal placement, deletion of the moved Unity files, adapted play-mode tests, and captures.
- `docs/architecture.md`: the Shipped-state section removed.

### Out of scope

- Rotate and bounce target selection and a skip control in the GUI. Next slice.
- Any Rules change beyond `PowersInPlay`.
- `RemoteSession`, a desktop build, device deployment.
- Whether an omitted power's cell renders as a blank; the maintainer decides before the power-UI slice.
- The open rulings.

## Strategic traceability

| Strategic requirement or criterion | How this slice advances it |
| --- | --- |
| Pass-and-play is local; 2–4 human seats share one `LocalSession` | The driver hands the device between human seats in App; an app test runs the sequence with no Unity reference. |
| The Unity player computes no legality and runs no turn loop | The adapter forwards events and draws the model; greps show no neighbor walk or seat cursor in Unity. |
| The GUI never decides legality | Highlights come from `ISession.LegalPlacements`; the match accepts or rejects. |
| A later room service is a seam, not a deliverable | `ISession` carries one action type and returns state and events; `RemoteSession` is a second adapter behind it. |
| The match can list the powers in play so a GUI can refuse what it cannot present | `Game.PowersInPlay` through `ISession.PowersInPlay`. |
| Acceptance: the AI is asked for an action; the driver advances an AI seat without Unity | `IPlayer.ChooseAction`; the driver test. |

## Gates and dependencies

### Hard gates

- The match/rules-engine split (PR #27) is on `main`. It is.
- The Unity CLI runs in this environment (`~/.unity/bin/unity`, editor `6000.3.16f1`). Confirmed before materialization.

### Sequencing recommendations

- App first, then the Unity adapter, on one branch: the Unity project compiles only against the rebuilt Release DLLs, so the adapter starts after the App issue is green.

## Architecture and contracts

- **Affected seams:** `ISession` and `IPlayer` change shape as the pack's target states. The Unity player's boundary shrinks to setup, input, render.
- **Public contracts:** `ISession`, `IPlayer`, `SeatView`, and the removal of App `Placement`/`PlacementPreview` are intentional changes approved here. Rules gains one read. `GameAction`, `GameEvent`, `CommandResult`, `Rejection`, `LegalPlacement`: unchanged.
- **Data and migration considerations:** None. The UPM package's DLL contents change; the refactor gate's clean-import check applies.

## High-level approach

Reshape `ISession` around `Submit(seat, action)` and the queries, letting `LocalSession` forward each to `Game`. Build the driver as the one place that knows which seats are automated: after any accepted action it asks automated seats for actions until a human is current, decides whether the hand is concealed from the last human seat, and assembles the render model from the view and the queries. Move the setup options and the deck into App with a seed. Then shrink the Unity `TableSession` to event forwarding and rendering, delete the moved files, and adapt the play-mode tests to the adapter's surface.

## Verification

- `dotnet test rules/MissionSplat.Rules.sln --configuration Release`
- `dotnet test app/MissionSplat.App.sln --configuration Release`
- `unity test unity/MissionSplat --mode PlayMode` on freshly copied Release DLLs, with a cold-import check of the package.
- Greps: no `MonoBehaviour` decides whose turn it is; no neighbor walk outside Rules; `HumanPlayer`, `TableDeck`, and `Preview(` absent from `unity/`; no `UnityEngine` in Rules or App.
- Captures of a three-seat hand-off and a stack placement in the PR.
- Repository definition-of-done commands remain mandatory.

## Risks and stop conditions

- Stop if the render model needs something the view and the queries cannot supply, or if Unity needs a `Game` reference.
- Stop if the Unity CLI fails to run the play-mode tests; editor or device runs are the maintainer's.
- Stop if the `PowersInPlay` guard would prevent the ordinary deck from starting.
- The Unity project will not compile between the App issue and the Unity issue on this branch; CI does not build Unity, and the PR is one unit, so `main` is never in that state.

## Execution issues

GitHub issues are the WIP tracker and source of task-level detail. Every issue for this slice carries the `slice:app-session-driver` label; this plan links the query, not the issues: https://github.com/cblack34/mission-splat/issues?q=label%3Aslice%3Aapp-session-driver

## Delivery shape

- **Topology:** Direct PRs to `main`.
- **Human merge gate:** Only the human may physically merge any PR whose base is `main`. Agents must stop when it is ready.

### Direct PRs

- **Branch:** `feat/app-session-driver`, cut from `origin/main` at `c697889`.

## Amendments

None.

## Delivery record

Complete once when the final PR is ready for human merge. Do not use this section for WIP status.

- **Outcome:** `ISession` submits a `GameAction` and forwards the legal-move queries; `LocalSession` is a thin adapter over `Game`. App owns the turn driver `Table` (automated seats, the hand-off between humans, the chosen orientation, stops, the render model), `TableSnapshot` and a typed `TableStatus`, `TableStart`, and `TableDeck` with a seeded shuffle; no randomness lives in App. `IPlayer.ChooseAction` serves automated seats and `AiPlayer` chooses from the session's legal placements. `Game.PowersInPlay` is the one Rules addition. The Unity player is setup form, input adapter, and renderer: `TableSession` forwards events to `Table` and draws its snapshot (71 lines, was 315); every legal placement including on-top is highlighted, so a stack is tappable; `HumanPlayer`, `TableDeck`, `TableStart`, and `TableSnapshot` left the Unity project. The hand is concealed only when control moves to a human other than the last human to hold the device.
- **Verification:** On `8adba78`, `dotnet test rules/MissionSplat.Rules.sln --configuration Release` passed 195, `dotnet test app/MissionSplat.App.sln --configuration Release` passed 41 (was 16), and `unity test unity/MissionSplat --mode PlayMode` on editor 6000.3.16f1 passed 5 of 5, all run by the implementation lead independently of the execution agents' receipts. A clean import in a detached worktree with no `Library/` passed the same 5 with no tracked file altered. Greps confirm no `UnityEngine` in App or Rules, no `new Random()` in App, no `HumanPlayer`/`TableDeck`/neighbor walk/`Preview(` in Unity, `MonoBehaviour` only in `TableSession` and `TableView`, and no `Game` type reference in Unity scripts. Captures of a three-seat hand-off and a stack placement are tracked by LFS. A fresh-context design review of all sixteen production files found no blocking finding; five of nine suggestions were applied, two rejected and one noted with evidence in the PR.
- **Deviations:** `AiPlayer` takes the session's query and preview functions rather than `ISession`, preserving the test that forbids a session reference in the AI. The planned start-time `PowersInPlay` guard in the GUI was dropped as a no-op for this slice; the query stays for the power-UI slice. `TableSession.Begin(TableStart, GameSetup)` is public for the stack play-mode test and the headless captures. On an AI turn with the match deck exhausted the stop message is the AI's, not the rules' open-ruling text, because the queries are total. The captures were rendered through a camera canvas to a RenderTexture in a headless editor; the shipping overlay canvas is unchanged. `TableStatus.Ended` is a fallback kind, unreachable today.
- **Unresolved gates or risks:** The rotate and bounce target UI is the next slice; `TableSnapshot` already carries the legal targets and remaining uses. Whether an omitted power's cell renders as a blank is the maintainer's call before that slice. `TablePlayView.TurnClockwise` still mirrors the private map in `Rules.Tile`. The open rulings are unchanged.
- **Refactor and handoff receipt:** In the PR body, https://github.com/cblack34/mission-splat/pull/30, and on issues #28 and #29.
- **Final PR:** https://github.com/cblack34/mission-splat/pull/30
