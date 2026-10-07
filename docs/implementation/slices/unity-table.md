# Slice plan — Unity table

## Strategic source

- **Active build pack:** [`docs/build-brief.md`](../../build-brief.md), [`docs/rules.md`](../../rules.md), [`docs/architecture.md`](../../architecture.md), [`docs/acceptance.md`](../../acceptance.md), [`docs/engineering/workflow.md`](../../engineering/workflow.md), [`docs/engineering/code-quality.md`](../../engineering/code-quality.md)
- **Human approval:** Approved in session on 2026-10-06. Editor pin `6000.3.16f1`. Bootstrap execution uses `grok-4.7-build-fast`. The table issue uses `grok-4.6`.
- **Final acceptance advanced:** The presentation checks in [`docs/acceptance.md`](../../acceptance.md), and the session check that one player binds 2–4 seats, human or AI, to `LocalSession`. Power-action fixtures, census encoding, device observation, and a desktop build wait.

## Outcome

A Unity 6.3 player starts a 2–4 seat game on one `LocalSession`. The table shows the board, the current seat's secret missions, and every seat's claim row. Legal placements highlight. Human seats pass the device between them. AI seats use `AiPlayer`.

## Why this slice is next

Claim fixtures and the local session are on `main`. The build brief's next consumer is the Unity player bound to `LocalSession`. Rotate, stack, and bounce stay later commands. `Place` still rejects an occupied cell, including a tile that shows stack.

## Scope

### In scope

- A Unity 6.3 URP project at `unity/MissionSplat`, created with the Unity CLI and editor `6000.3.16f1`.
- The local UPM package `packages/com.mission-splat.game`. Release builds copy the Rules and App `netstandard2.1` DLLs into it. Those DLLs stay untracked.
- Composition that starts `LocalSession` for 2–4 seats, any human and AI mix, with an explicit first seat.
- `HumanPlayer` in the Unity project. It implements `IPlayer` from a tap.
- A named representative deck owned by the player. It is original, large enough for the ordinary win of 4, and it is not the census.
- The table: board, pending match tile, that seat's unclaimed missions, every claim row, conceal until the incoming human confirms, and highlights where `Preview` accepts the chosen quarter-turn.

### Out of scope

- Rotate, stack-on-top, bounce, and any open ruling in [`docs/rules.md`](../../rules.md).
- Census encoding, a desktop player, device deployment, an Actions Unity job, the room service, and `RemoteSession`.
- Changes to `ISession`, `IPlayer`, `AiPlayer`, or Rules behavior.
- A NuGet feed, a committed rules DLL, or a second Unity package.

## Strategic traceability

| Strategic requirement or criterion | How this slice advances it |
| --- | --- |
| Unity consumes Rules and App through one local UPM package | The player references `packages/com.mission-splat.game`. The DLLs are build output. |
| Pass-and-play is local | Two to four human seats share one `LocalSession` on one player. Secrets stay hidden until the incoming human confirms. |
| Human and AI seats share the session | Composition binds `HumanPlayer` or `AiPlayer` per seat. The AI still sees only its seat view. |
| The table renders events and views | The board, the pending tile, the secret hand, and the claim row come from `SeatView`. Highlights come from `Preview`. |
| No copied trade dress | Shapes and colors follow [`docs/diagrams/`](../../diagrams/). The deck is original. |
| Powers stay later | This slice places tiles. It does not execute rotate, stack-on-top, or bounce. |

## Gates and dependencies

### Hard gates

- `LocalSession`, `SeatView`, `Preview`, and `AiPlayer` are on `main`.
- The Unity CLI `v1.0.0-beta.12` is signed in, Unity Personal is assigned, and editor `6000.3.16f1` is installed.
- `unity pipeline install` must succeed before project creation. That check belongs to the bootstrap issue.

### Sequencing recommendations

- Bootstrap the project and the package, then bind the table. The table needs a project that already compiles against the DLLs.

### Closed by the human for this slice

- Editor pin `6000.3.16f1`.
- The player owns a named representative deck. The census stays unencoded.
- A desktop build is not part of this slice.

### Still open

- How long a stack stays buried, bounce of the last tile, a drawn tile with no legal placement, claim by rotate, stack, or bounce, and an exhausted mission or match deck.
- Uncorrected census rows.
- Device installation and store signing.

## Architecture and contracts

- **Affected seams:** Unity owns `HumanPlayer`, the table, and the composition root. `LocalSession` remains the session adapter. Rules remains the command authority.
- **Public contracts:** `ISession`, `IPlayer`, and Rules stay as they are. The only types that cross into Unity are the existing commands, events, seat views, and the two interfaces, through the UPM package.
- **Data and migration considerations:** None. There is no saved game. Copied DLLs are local build output.

## High-level approach

Composition asks for the seat count, which seats are AI, and the first seat, then starts one `LocalSession` with the named deck. The turn loop reads the current seat's view. A human confirms before the hand is shown, then taps a highlighted slot. Quarter-turns 0 through 3 are part of that placement. An AI seat calls `AiPlayer.ChoosePlacement` with its view and the session preview, and the session places the result. Claimed missions render beside the lattice. Automated play uses a fixed deck order. A human session may shuffle that same deck in the player before start.

The package build copies Release DLLs into `packages/com.mission-splat.game`. Unity does not reference the App project.

## Verification

- `dotnet test rules/MissionSplat.Rules.sln --configuration Release`
- `dotnet test app/MissionSplat.App.sln --configuration Release`
- The Unity CLI shows the project compiling against the copied DLLs.
- `unity test` covers one scripted human placement and one AI turn.
- Play-mode captures show the board, the current secrets, the claim row, a legal highlight set, and the conceal step between two human seats.
- Diff review finds no photograph, farm character, or physical-product name.
- Device observation waits for the completing PR. This slice does not add Unity to the `rules` job.

## Risks and stop conditions

- Stop if the CLI cannot create or drive a Unity 6.3 URP project. Do not hand-write a project in place of that failure.
- Stop if the table needs another seat's unclaimed missions, a later deck tile, or a `Game` reference inside Unity.
- Stop if a normal game to four claims exhausts a deck or deals a tile with no legal placement.
- Stop if the package needs a NuGet feed, a committed DLL, or a second package.
- iOS and Android device installs stay human.

## Execution issues

GitHub issues are the WIP tracker and source of task-level detail.

| Issue | Purpose | Dependencies |
| --- | --- | --- |
| [#10](https://github.com/cblack34/mission-splat/issues/10) — Bootstrap the Unity player and local UPM package | Create the Unity 6.3 URP project, the local UPM package, and the untracked DLL copy. | None |
| [#11](https://github.com/cblack34/mission-splat/issues/11) — Bind the table to the local session | Composition, `HumanPlayer`, the named deck, the table, highlights, and play-mode evidence. | #10 |

## Delivery shape

- **Topology:** Direct PR to `main`.
- **Branch or spine:** `feat/unity-table`, cut from `origin/main`. Upstream is unset so a plain push cannot target `main`.
- **Final PR:** https://github.com/cblack34/mission-splat/pull/12 Title `feat: add the Unity table bound to the local session`.
- **Human merge gate:** Only the human may physically merge the final PR to `main`. Agents must stop when it is ready.

## Amendments

None.

## Delivery record

Complete once when the final PR is ready for human merge. Do not use this section for WIP status.

- **Outcome:** `unity/MissionSplat` on editor `6000.3.16f1` starts a 2–4 seat game on `LocalSession`. The table shows the board, the pending tile at the chosen quarter-turn, the current human seat's missions after confirm, and every claim row. Highlights come from `Preview`. AI seats use `AiPlayer`.
- **Verification:** `dotnet test rules/MissionSplat.Rules.sln --configuration Release` passed, 78 tests, 0 failed. `dotnet test app/MissionSplat.App.sln --configuration Release` passed, 15 tests, 0 failed. `unity test unity/MissionSplat --mode PlayMode` passed 2, failed 0. Captures are in `unity/MissionSplat/Assets/Captures/`.
- **Deviations:** Package Manager pinned the URP blank template, including packages the table does not use. The pending tile is drawn with the same clockwise map as placement. `board.png`, `secrets.png`, and `highlights.png` are one post-confirm frame. `claim-row.png` and `conceal-between-humans.png` are one post-place frame.
- **Unresolved gates or risks:** Rotate, stack-on-top, bounce, and the open rulings in `docs/rules.md`. Uncorrected census rows. Device play. A long game that never claims can still exhaust a deck.
- **Final PR:** https://github.com/cblack34/mission-splat/pull/12
- **Merge state:** Ready for review. Agents do not merge to `main`.
