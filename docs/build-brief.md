# Build Brief

_Read this first, then follow the active-pack order in `AGENTS.md`. This document defines the complete strategic outcome; the implementation agent will inspect the repository and propose the tactical plan with the user._

_Code, types, schemas, and file layouts are **illustrative guidance, not mandated implementation**. Described behavior, architecture contracts, non-negotiables, and [`acceptance.md`](acceptance.md) are authoritative._

## Product outcome

Mission Splat is a 2–4 seat tile game for one phone. Each seat has two secret mission cards. On a turn a seat draws a 2×2 match tile and places it orthogonal to the board. If that placement finishes a line, square, or L of one color on one of that seat's missions, the seat claims that mission, lays it face up, and draws a replacement. First to four claims wins.

The POC is offline. One human can play both seats (pass-and-play), or one human can play an AI seat. A later room service is a seam, not a deliverable.

Intended users are the people at the table, including a child who can follow a secret card. The player is Unity 6, URP, C#. iOS and Android are the phone targets. A desktop player is the same Unity project, built when a laptop demo is needed.

## In scope

- The rules in [`rules.md`](rules.md): setup, orthogonal placement, three mission shapes, claim-on-your-placement, power cells, win at four claims.
- A rules library a `dotnet test` run can execute with no Unity editor.
- An application library with `ISession` and `IPlayer`, a `LocalSession` adapter, and an `AiPlayer` adapter that does not see the opponent's secret missions.
- A Unity player that renders the board, the secret hand, the face-up claim row, and legal placement highlights, and that binds 2–4 seats to `LocalSession`.
- Pass-and-play on one device, and human versus AI on one device.
- Schematic diagrams in [`diagrams/`](diagrams/) as the visual reference. Source photographs stay out of the repo.

## Out of scope

- A network room, accounts, matchmaking, and `RemoteSession`. The interfaces exist so a later adapter can send the same command and render returned events.
- WebGL or a browser client. Unity web builds are a rejected path for this horizon.
- Store listing, purchases, ads, and platform services.
- More than four seats.
- A census of every physical tile. Deck composition beyond the photographed symbols is an open gate, not a guessed list.
- The physical game's name, logo, farm characters, and photographed art.

## User directives and non-negotiables

1. **Rules stay engine-free.** The domain library references no `UnityEngine` type. Failure: a server or `dotnet test` cannot judge a move without the editor. Verification: `dotnet test` on the rules solution, with no Unity reference in that project.
2. **Claim is placement-owned.** A pattern on your mission scores only if the tile you just placed completed it. Failure: the other seat's tile completes your card and you take the claim. Verification: rules fixtures for both the legal claim and the stolen-pattern rejection.
3. **Offline multiplayer is local.** Two to four human adapters share one `LocalSession`. Failure: pass-and-play opens a socket or requires a second device. Verification: human observation of a 3-seat game on one player, plus an automated test that four human command sources can alternate on `LocalSession`.
4. **No copied trade dress.** Original splat shapes and original names only. Failure: a farm character, the physical product name, or a source photo lands in the tree or the player. Verification: review of the diff and of the built content.

## Architecture boundaries and contracts

Canonical rules behavior is [`rules.md`](rules.md). The C4 ownership cut is [`architecture.md`](architecture.md) and [`diagrams/c4.svg`](diagrams/c4.svg).

- `Rules` owns the board, placement, power resolution, matching, and claim. It accepts a command and returns events.
- `App` owns `ISession` and `IPlayer`. `LocalSession` and `AiPlayer` are adapters in `App`.
- The Unity player owns `HumanPlayer`, the table view, and the composition root. It does not reimplement matching.
- A later room service would reference the same rules library and accept a command only if that library accepts it. It is not built in this horizon.

## Research, decisions, and open gates

Adopted from the design session, not from a second implementation:

- Unity 6 with URP is the player. The built-in pipeline is being retired; HDRP is not a phone pipeline.
- Shared rules are a `netstandard2.1` library. The player consumes a local UPM package that holds the built DLL. A NuGet feed inside Unity is rejected for this repo.
- The later server, if built, is ASP.NET Core so it can reference that library. FastAPI was rejected because it would fork the matcher.
- Web is out. Desktop is another player build.

Assumptions, labeled as such:

- A stacked tile's cells replace the covered tile's cells for matching.
- "Completely surrounded" means all four orthogonal neighbors are occupied.
- The wildcard color is chosen once and kept for the rest of that game.
- Gray power cells and the blank cell do not score as a color.
- For AI, the human may choose who is first. The physical "youngest goes first" rule has no AI meaning.

Open gate: the photographed rules do not list the full match-tile census. A representative deck may ship if fixtures name it as representative. A claim that the deck matches the physical box is not allowed until a census exists.

## Risks and failure modes

- **Splat lattice bugs.** Four-in-a-row across tile boundaries is the expensive rule. Mitigation: fixtures for row, square, L, rotation, wildcard, and false claim before view work. Detection: `dotnet test`.
- **Stolen claims.** The other seat completes your pattern. Mitigation: the claim function takes the seat that placed the tile. Detection: a fixture where the pattern exists and the claim is rejected.
- **Trade-dress leak.** Mitigation: diagrams are original schematics; review rejects photos and farm art.
- **Unity swallowing the rules.** Mitigation: the rules project has no Unity reference; review rejects `UnityEngine` in that tree.

## Known dependencies

Matching and claim cannot be validated until the splat lattice and the placement-owned claim function exist. View work done before those fixtures pass will encode the wrong geometry. The room service cannot be proven until it executes the same fixtures; that proof is outside this horizon.

## Suggested implementation approach

_This is strategic guidance, not a required sequence. The implementation agent should evaluate it against the live repository and may reorder it when code, tests, or unforeseen constraints support a better plan._

1. Rules library and claim fixtures. The lattice is the long pole, and every later adapter depends on it.
2. Application interfaces, `LocalSession`, and `AiPlayer`, still without Unity.
3. Unity composition and table view, bound to `LocalSession`.
4. Desktop player build only if a laptop demo is requested.

## Unity tooling

Prefer the Unity CLI for project creation, editor driving, tests, builds, and play-mode evidence. Install its agent skill with `unity skill install` and pin the CLI version, because it is beta (`v1.0.0-beta.12` when evaluated). Project creation needs a signed-in, licensed user and the `com.unity.pipeline` package (`unity pipeline install`). Fall back to the Editor GUI or human observation when the CLI fails, and tell the user. iOS signing, Android keystores, and device deployment are human steps. Verify CLI behavior on this project before relying on it.

## Definition of done

An offline game of 2–4 seats can be played to four claims on a device, including a human-only table and a table with an AI seat, and the rules fixtures pass:

```bash
dotnet test rules/MissionSplat.Rules.sln --configuration Release
dotnet test app/MissionSplat.App.sln --configuration Release
```

Every item in [`acceptance.md`](acceptance.md) passes. The room service is absent.
