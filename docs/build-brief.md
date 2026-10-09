# Build Brief

_Read this first, then follow the active-pack order in `AGENTS.md`. This document defines the complete strategic outcome; the implementation agent will inspect the repository and propose the tactical plan with the user._

_Code, types, schemas, and file layouts are **illustrative guidance, not mandated implementation**. Described behavior, architecture contracts, non-negotiables, and [`acceptance.md`](acceptance.md) are authoritative._

## Product outcome

Mission Splat is a 2–4 seat tile game for one phone. Each seat has two secret mission cards. On a turn a seat draws a 2×2 match tile and places it orthogonal to the board. If that placement finishes one or more of that seat's missions — a line, square, or L of one color — the seat claims each of them, lays it face up, and draws one replacement per claim. The ordinary game is first to four claims. Setup can require another count.

The POC is offline. One human can play every seat (pass-and-play), or play alongside AI seats. Any seat count from 2 to 4 and any human/AI mix is allowed. A later room service is a seam, not a deliverable.

Intended users are the people at the table, including a child who can follow a secret card. The player is Unity 6, URP, C#. iOS and Android are the phone targets. A desktop player is the same Unity project, built when a laptop demo is needed.

## In scope

- The rules in [`rules.md`](rules.md): setup, orthogonal placement, three mission shapes, claim-on-your-placement, power cells, and a win at the configured claim count, which is four in the ordinary game.
- A rules library a `dotnet test` run can execute with no Unity editor.
- An application library with `ISession` and `IPlayer`, a `LocalSession` adapter, an `AiPlayer` adapter that does not see any other seat's secret missions, the turn driver that runs AI seats and hands the device between humans, and the named deck content.
- A Unity player that collects setup, renders the board, the secret hand, the face-up claim row, and the legal moves the session reports, and submits a tap as an action for 2–4 seats on `LocalSession`. It computes no legality and runs no turn loop.
- Pass-and-play on one device, and human versus AI on one device.
- Schematic diagrams in [`diagrams/`](diagrams/) as the visual reference. Source photographs stay out of the repo.

## Out of scope

- A network room, accounts, matchmaking, and `RemoteSession`. The interfaces exist so a later adapter can send the same command and render returned events.
- WebGL or a browser client. Unity web builds are a rejected path for this horizon.
- Store listing, purchases, ads, and platform services.
- More than four seats.
- A second rule set or extra tiles. The base deck is [`census.md`](census.md). Another definition may replace it. The matcher does not change.
- The physical game's name, logo, farm characters, and photographed art.

## User directives and non-negotiables

1. **Rules stay engine-free.** The domain library references no `UnityEngine` type. Failure: a server or `dotnet test` cannot judge a move without the editor. Verification: `dotnet test` on the rules solution, with no Unity reference in that project.
2. **Claim is placement-owned.** A pattern on your mission scores only if the tile you just placed completed it. Failure: another seat's tile completes your card and you take the claim. Verification: rules fixtures for both the legal claim and the stolen-pattern rejection.
3. **Offline multiplayer is local.** Two to four human adapters share one `LocalSession`. Failure: pass-and-play opens a socket or requires a second device. Verification: human observation of a 3-seat game on one player, plus an automated test that four human command sources can alternate on `LocalSession`.
4. **No copied trade dress.** Original splat shapes and original names only. Failure: a farm character, the physical product name, or a source photo lands in the tree or the player. Verification: review of the diff and of the built content.

## Architecture boundaries and contracts

Canonical rules behavior is [`rules.md`](rules.md). The C4 ownership cut is [`architecture.md`](architecture.md) and [`diagrams/c4.svg`](diagrams/c4.svg).

- `Rules` owns two parts with a named seam. A stateless rules engine judges one action against the board, the drawn tile, the acting seat's hand, and the catalog: where the drawn tile may go, which board tiles a power may act on, what a use does, and which missions the placement completed. The match owns seats, hands, decks, claim rows, the current seat, and the turn phase; it accepts one action from a closed, typed set per ruleset — use rotate, use bounce, place — and returns the next match with events. The board a seat renders comes from the match.
- `App` owns `ISession` and `IPlayer`. `LocalSession` and `AiPlayer` are adapters in `App`. `App` also owns the turn driver (which seats are automated, running them, the hand-off between humans, the render model a GUI draws) and the deck content. `IPlayer` is the action source for an automated seat.
- The Unity player owns the setup form, the table view, and the composition root. It submits a human's tap as an action and renders what the session reports. It computes no legality, runs no turn loop, holds no deck, and does not reimplement matching.
- A later room service would reference the same rules library and accept an action only if that library accepts it. It is not built in this horizon.

## Research, decisions, and open gates

Adopted from the design session, not from a second implementation:

- Unity 6 with URP is the player. The built-in pipeline is being retired; HDRP is not a phone pipeline.
- Shared rules are a `netstandard2.1` library. The player consumes a local UPM package that holds the built DLL. A NuGet feed inside Unity is rejected for this repo.
- The later server, if built, is ASP.NET Core so it can reference that library. FastAPI was rejected because it would fork the matcher.
- Web is out. Desktop is another player build.
- A wildcard stays wild. Each mission check counts it as that mission's color. Nothing stores a chosen color.
- One placement claims every secret mission the acting seat holds that the placement completed. The turn then passes once.
- The ordinary win count is 4. Setup can require another positive count.
- Colors, non-scoring symbols, the patterns in play, and the win count are setup data. The ordinary catalog is red, blue, green, purple, blank, rotate, stack, bounce, a wildcard cell, and the row, square, and L. Of the non-scoring symbols setup lists, those that are rotate, stack, or bounce are the powers in play; any other listed symbol is non-scoring with no power, and a power symbol setup does not list is a blank.
- Rotate and bounce are used before the drawn tile is placed, one use per cell, one at a time, each on a tile already on the board. The drawn tile is never a target. A bounce may empty the board; an empty board accepts the drawn tile only at the origin. Stack is the placement itself, on an occupied position. Decided 2026-10-09 from the physical game, replacing an earlier after-placement reading.
- Only the cells the placed tile wrote complete a mission. A power reshapes the board and never claims on its own.
- The match accepts one action type per ruleset: a closed, typed set, never a string. Powers are composable: each power's legality and effect lives in its own part of the rules engine, and a ruleset enables a set of them. Whether customization ships as preset rulesets or per-power toggles is decided when a second ruleset is wanted.

Assumptions, labeled as such:

- A stacked tile's cells replace the covered tile's cells for matching.
- "Completely surrounded" means all four orthogonal neighbors are occupied, counting only tiles on the board.
- The first seat is an explicit start input. Rules do not compute youngest. With an AI seat, the human chooses.
- An accepted power use stays applied when a later use or the placement is rejected; the seat chooses again.

The base deck is [`census.md`](census.md). A fixture may use a smaller deck if it names that deck. A claim that the shipped deck matches the physical box uses the census definition.

## Risks and failure modes

- **Splat lattice bugs.** Four-in-a-row across tile boundaries is the expensive rule. Mitigation: fixtures for row, square, L, rotation, wildcard, and false claim before view work. Detection: `dotnet test`.
- **Stolen claims.** The other seat completes your pattern. Mitigation: the claim function takes the seat that placed the tile. Detection: a fixture where the pattern exists and the claim is rejected.
- **Trade-dress leak.** Mitigation: diagrams are original schematics; review rejects photos and farm art.
- **Unity swallowing the rules.** Mitigation: the rules project has no Unity reference; review rejects `UnityEngine` in that tree.
- **Unity swallowing the table.** The turn loop and the deck drifted into the Unity player once. Mitigation: the turn driver and deck content live in `App`, with an app test that runs a turn sequence with no Unity reference; review rejects a `MonoBehaviour` that decides whose turn it is. Detection: `dotnet test` on the app solution.
- **Mid-turn state.** The match now holds a turn phase: which power cells are spent and that the drawn tile is still pending. Mitigation: fixtures for a rejected use leaving the match unchanged, an accepted use staying applied, and a use after the placement rejected.

## Known dependencies

Matching and claim cannot be validated until the splat lattice and the placement-owned claim function exist. View work done before those fixtures pass will encode the wrong geometry. The room service cannot be proven until it executes the same fixtures; that proof is outside this horizon.

## Suggested implementation approach

_This is strategic guidance, not a required sequence. The implementation agent should evaluate it against the live repository and may reorder it when code, tests, or unforeseen constraints support a better plan._

1. Rules library and claim fixtures. The lattice is the long pole, and every later adapter depends on it.
2. Application interfaces, `LocalSession`, and `AiPlayer`, still without Unity.
3. Unity composition and table view, bound to `LocalSession`.
4. Desktop player build only if a laptop demo is requested.

After the 2026-10-09 correction, with 1–3 shipped against the earlier reading: the corrected turn actions in Rules; the match/rules-engine split; the session, turn driver, and deck content in `App`; the power UI; then the completing device evidence.

## Unity tooling

Prefer the Unity CLI for project creation, editor driving, tests, builds, and play-mode evidence. Install its agent skill with `unity skill install` and pin the CLI version, because it is beta (`v1.0.0-beta.12` when evaluated). Project creation needs a signed-in, licensed user and the `com.unity.pipeline` package (`unity pipeline install`). Fall back to the Editor GUI or human observation when the CLI fails, and tell the user. iOS signing, Android keystores, and device deployment are human steps. Verify CLI behavior on this project before relying on it.

## Definition of done

An offline game of 2–4 seats can be played to the configured claim count on a device (four in the ordinary game), including a human-only table and a table with an AI seat, and the rules fixtures pass:

```bash
dotnet test rules/MissionSplat.Rules.sln --configuration Release
dotnet test app/MissionSplat.App.sln --configuration Release
```

Every item in [`acceptance.md`](acceptance.md) passes. The room service is absent.
