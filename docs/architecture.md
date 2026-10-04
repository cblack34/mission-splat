# Architecture

Descriptive. Code wins after it exists; update this document when a boundary changes.

## Containers

The POC is one process on the device. The later room is the same command and event seam, drawn dashed in [`diagrams/c4.svg`](diagrams/c4.svg).

```text
rules/                 netstandard2.1 rules library and dotnet test
app/                   ISession, IPlayer, LocalSession, AiPlayer
unity/MissionSplat/    player, HumanPlayer, table view, composition
packages/com.mission-splat.rules/   local UPM package holding the built rules DLL
server/                empty in the POC; later ASP.NET project reference
```

Names are illustrative. The dependency rule is not: Unity may reference App and the UPM package; App may reference Rules; Rules references neither.

## Components

- **Composition root** (Unity) binds `HumanPlayer` or `AiPlayer` to `LocalSession`.
- **HumanPlayer** (Unity) implements `IPlayer` by waiting on a tap.
- **Table view** (Unity) renders events. It does not decide claims.
- **LocalSession** (App) implements `ISession` in process.
- **AiPlayer** (App) implements `IPlayer` from its own missions and the public board.
- **RemoteSession** is not in the POC. When it exists, it implements `ISession` and stays outside Unity.
- **Rules** accepts a command and returns events: placed, rotated, stacked, bounced, wildcard chosen, claimed, won.

A tap becomes a command. `LocalSession` asks Rules. Rules returns events. The table renders events. On the AI seat, `AiPlayer` is asked for a command. Pass-and-play is 2–4 `HumanPlayer` seats on one `LocalSession`. Empty seats may be AI adapters.

## Contracts

The only types that cross the Unity boundary are commands, events, and the two interfaces. Prefabs, touch, and scenes do not appear in Rules or App.

The player must not take the rules package from a NuGet feed. Build the DLL and consume the local UPM package. A later server uses a project reference, not the UPM package.

## Rejected alternatives

- A JS game inside a WebView. Rejected for this horizon so the exercise matches the store-game stack.
- FastAPI as the authority. Rejected because it cannot reference the C# rules library.
- Unity as a WebView host. Rejected because the player is the game, not an installer.

## Dependency policy

Add a dependency only when the BCL or a package already required by the player cannot do the job. The license must be MIT, Apache-2.0, or BSD, or the Unity package must be under the Unity companion license. It must target `netstandard2.1` if Rules or App reference it, and it must load in Unity 6.3. Do not add a package with a known high-severity advisory. Review rejects a NuGet feed inside the Unity project for the rules library.
