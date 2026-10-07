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
- **Verification:** `dotnet test rules/MissionSplat.Rules.sln --configuration Release` passed, 78 tests, 0 failed. `dotnet test app/MissionSplat.App.sln --configuration Release` passed, 15 tests, 0 failed. `unity test unity/MissionSplat --mode PlayMode` passed 4, failed 0 (`ScriptedHumanPlacement_ThenOneAiTurn_PlacesThroughLocalSession`, `InteractiveLayoutsSitInsideTheReportedSafeArea`, `RuntimeInputModule_BindsPointClickAndSubmit`, `StatusStopTextWrapsInsideItsRect`). Captures are in `unity/MissionSplat/Assets/Captures/`.
- **Deviations:** Package Manager pinned the URP blank template, including packages the table does not use. The pending tile is drawn with the same clockwise map as placement. `board.png`, `secrets.png`, and `highlights.png` are one post-confirm frame. `claim-row.png` and `conceal-between-humans.png` are one post-place frame.
- **Unresolved gates or risks:** Rotate, stack-on-top, bounce, and the open rulings in `docs/rules.md`. Uncorrected census rows. Device play. A long game that never claims can still exhaust a deck.
- **Final PR:** https://github.com/cblack34/mission-splat/pull/12
- **Merge state:** Ready for review. Agents do not merge to `main`.

## Refactor and handoff receipt

- Changed production files reviewed: `app/MissionSplat.App/MissionSplat.App.csproj`, `packages/com.mission-splat.game/package.json`, `packages/com.mission-splat.game/Runtime/MissionSplat.Game.asmdef`, `packages/com.mission-splat.game/Runtime/PackageCompileProbe.cs`, `unity/MissionSplat/Assets/Scripts/CellPainter.cs`, `unity/MissionSplat/Assets/Scripts/HumanPlayer.cs`, `unity/MissionSplat/Assets/Scripts/SplatPalette.cs`, `unity/MissionSplat/Assets/Scripts/TableDeck.cs`, `unity/MissionSplat/Assets/Scripts/TablePlayView.cs`, `unity/MissionSplat/Assets/Scripts/TableSession.cs`, `unity/MissionSplat/Assets/Scripts/TableSetupView.cs`, `unity/MissionSplat/Assets/Scripts/TableSnapshot.cs`, `unity/MissionSplat/Assets/Scripts/TableStart.cs`, `unity/MissionSplat/Assets/Scripts/TableView.cs`, `unity/MissionSplat/Assets/Scripts/Ui.cs`, `unity/MissionSplat/Assets/Scripts/MissionSplat.Player.asmdef`, `unity/MissionSplat/Assets/Tests/PlayMode/TableSessionPlayModeTests.cs`, `unity/MissionSplat/Assets/Tests/PlayMode/MissionSplat.Player.Tests.asmdef`, `unity/MissionSplat/Packages/manifest.json`, `unity/MissionSplat/Packages/packages-lock.json`, `unity/MissionSplat/ProjectSettings/EditorBuildSettings.asset`, `unity/MissionSplat/ProjectSettings/ProjectVersion.txt`, `docs/architecture.md`, and the caption of `docs/diagrams/c4.svg`. `*.meta`, capture PNGs, TutorialInfo, SampleScene, URP settings assets, `.vscode` json, and the rest of ProjectSettings were skipped as Unity template or generated import metadata.
- Owning abstractions/callers reviewed: `ISession`, `IPlayer`, `LocalSession`, `AiPlayer` (including its orthogonal neighbor walk), `SeatView`, `SeatClaims`, `BoardView`, `Placement`, `PlacementPreview`, `GameSetup`, `Tile.Local` and private `TurnClockwise`, `Game.Place` (full-side share and occupied-cell rejection), and `OrdinaryCatalog`. The Table scene hosts an empty `TableSession` only; the board is not scene state. The c4 caption matches the tap, session, rules, seat-view, and AI flow.
- Cohesion/SRP findings: none from the fresh-context review. No further structural change is justified for `TableSession`, `TableView`, `TableSetupView`, `TablePlayView`, `TableSnapshot`, `TableStart`, `TableDeck`, `HumanPlayer`, `CellPainter`, `SplatPalette`, and `Ui`, because each already has one reason to change. `TableSession` is the composition root and turn driver: it binds `HumanPlayer` or `AiPlayer` to one `LocalSession`, keeps the seat cursor so `View` is called only for that seat, and offers highlights only where `Preview` accepts. It does not decide claims. `TableView` only builds the canvas, applies one safe-area inset to interactive content, and forwards `Tapped`, `Confirmed`, `QuarterTurnsSelected`, and `Started`. `TableSetupView` owns the draft form. `TablePlayView` only renders a snapshot. `TableSnapshot` is the render input. `TableStart` is the one home for the seat count, AI flags, first seat, shuffle, and seat ids. `TableDeck` is the one home for the named deck and the human-only shuffle. `HumanPlayer` stays a tap buffer behind the frozen synchronous `IPlayer`. `CellPainter` is how a cell or mission looks; `SplatPalette` is the color map; `Ui` is the widget construction used by both views. The clockwise map copied in `TablePlayView` is required because `Tile.TurnClockwise` is private and this slice cannot open Rules. The four neighbor steps also present on `AiPlayer` stay duplicated because that type is frozen and the two searches are not the same policy. Neither copy is a second authority: `Place` and `Preview` still accept or reject.
- Structural changes made: the ones already on this branch before the review. Setup view was split from play rendering. `TableView` was left as canvas, safe area, and event forwarder. Assembly copy is Release-only. A local seat cursor requests `View` only for the current seat. The fresh review justified no further structural change.
- Findings rejected and evidence: none were raised by the fresh-context review. An earlier cohesion comment was already applied as the setup/play split in `ddf41e81f6f3c1d8dd7861b5fa93978662d74dc1`.
- Package/artifact verification: the first receipt noted that no clean-install import had been run; this commit supersedes that note with the result. A detached worktree at `0054fbeca9912a37ecd9ffecb410243e29977a05` had no Library or Temp. `dotnet test rules/MissionSplat.Rules.sln --configuration Release` passed 78, failed 0, and `dotnet test app/MissionSplat.App.sln --configuration Release` passed 15, failed 0. That Release copy wrote gitignored `packages/com.mission-splat.game/Runtime/Plugins/MissionSplat.App.dll` (23040 bytes) and `MissionSplat.Rules.dll` (43008 bytes). A cold import of that commit then ran `unity test` PlayMode on editor `6000.3.16f1`: 2 passed, 0 failed, and 0 compile errors. That tree did not yet contain `RuntimeInputModule_BindsPointClickAndSubmit`. `Packages/manifest.json` and `Packages/packages-lock.json` were unchanged. The editor rewrote `Assets/Settings/Mobile_RPAsset.asset` inside that worktree only; that rewrite was discarded and not copied back. `CopyGameAssemblies` publishes those DLLs only for Release, into the single local package's `Runtime/Plugins`, and those binaries plus their metas stay gitignored. The Game asmdef sets `overrideReferences` and names both DLLs. `PackageCompileProbe` mentions only `ISession`; `Rules.dll` remains on that compile list, and the player uses Rules types directly. The player and play-mode test asmdefs repeat those DLL names because `overrideReferences` is on; they reference `MissionSplat.Game`, not the App or Rules projects. `manifest.json` depends on `com.mission-splat.game` via `file:../../../packages/com.mission-splat.game`, and `packages-lock.json` matches that local package. URP 17.3.0, uGUI 2.0.0, Input System 1.19.0, Test Framework 1.6.0, and `com.unity.pipeline` 0.8.0-exp.1 match the lock. Unused URP-template packages remain; that is the recorded template pin. `EditorBuildSettings` enables only `Assets/Scenes/Table.unity`. `ProjectVersion.txt` is `6000.3.16f1`. No NuGet feed and no second package.
- Post-refactor focused checks: play-mode on the current code. `unity test unity/MissionSplat --mode PlayMode` passed 4, failed 0 (`ScriptedHumanPlacement_ThenOneAiTurn_PlacesThroughLocalSession`, `InteractiveLayoutsSitInsideTheReportedSafeArea`, `RuntimeInputModule_BindsPointClickAndSubmit`, `StatusStopTextWrapsInsideItsRect`). Fresh `test-results.xml` has `result="Passed"` and `total="4"`.
- Complete repository checks: `dotnet test rules/MissionSplat.Rules.sln --configuration Release` passed 78, failed 0. `dotnet test app/MissionSplat.App.sln --configuration Release` passed 15, failed 0. `unity test unity/MissionSplat --mode PlayMode` passed 4, failed 0.
- Fresh-context design review: read-only review of the production surface at `ddf41e81f6f3c1d8dd7861b5fa93978662d74dc1`, findings none, no further structural change.
- Remaining risks or justified debt: rotate, stack-on-top, bounce, and the open rulings in `docs/rules.md`; uncorrected census rows; device play; a long game that never claims can still exhaust a deck. The clockwise map in `TablePlayView` and the neighbor walk duplicated with `AiPlayer` stay as described above.
