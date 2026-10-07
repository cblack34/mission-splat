# Architecture

Descriptive. Code wins after it exists; update this document when a boundary changes.

## Containers

The POC is one process on the device. The later room is the same command and event seam, drawn dashed in [`diagrams/c4.svg`](diagrams/c4.svg).

```text
rules/                 netstandard2.1 rules library and dotnet test
app/                   ISession, IPlayer, LocalSession, AiPlayer
unity/MissionSplat/    player, HumanPlayer, table view, composition
packages/com.mission-splat.game/   local UPM package holding the built Rules and App DLLs
server/                empty in the POC; later ASP.NET project reference
```

Names are illustrative. The dependency rule is not: Unity consumes Rules and App only through the single local UPM package. It does not also reference the App project. App references Rules by project reference. The later server references Rules by project reference. Rules references neither.

## Components

- **Composition root** (Unity) binds `HumanPlayer` or `AiPlayer` to `LocalSession`. It collects the seat count, which seats are AI, and the first seat, then starts one session.
- **HumanPlayer** (Unity) implements `IPlayer` by waiting on a tap.
- **Table view** (Unity) renders events. It does not decide claims. Highlights come from `Preview`.
- **Named table deck** (Unity) is original player content, not the census. Automated play keeps its order. A human session may shuffle it before `Start`. The ordinary win count stays 4.
- **LocalSession** (App) implements `ISession` in process.
- **AiPlayer** (App) implements `IPlayer` from its own missions and the public board.
- **RemoteSession** is not in the POC. When it exists, it implements `ISession` and stays outside Unity.
- **Rules** accepts a command and returns events. Place is the command that resolves a claim, and setup does not claim. A successful place can return more than one claimed event, then won when the configured count is met. There is no wildcard-chosen event: a wild cell stays wild, and each mission check counts it as that mission's color. Rotate, stack, and bounce remain later commands on this same boundary. Matching reads the cell grid a placement writes. Setup supplies the color catalog, the non-scoring symbols, the active patterns, and the claims required to win.

A tap becomes a command. `LocalSession` asks Rules. Rules returns events. The table renders events. On the AI seat, `AiPlayer` is asked for a command. Pass-and-play is 2–4 `HumanPlayer` seats on one `LocalSession`. Empty seats may be AI adapters.

## Contracts

The only types that cross the Unity boundary are commands, events, seat-scoped state views, and the two interfaces. Prefabs, touch, and scenes do not appear in Rules or App. Anything handed to a seat, event or state view, is scoped to that seat: another seat's unclaimed mission identities never appear in it. Claimed missions are public. A seat view includes the one match tile the next placement will consume and that tile's four cells. The rest of either deck stays hidden, and after the game has ended that next tile is absent. The local UPM package holds both the Rules and App DLLs; there is no second package and no NuGet feed inside Unity.

The player must not take the rules package from a NuGet feed. The rules and app build copy the DLLs into the local UPM package. Those DLLs are build output and are not committed. A later server uses a project reference, not the UPM package.

## Rejected alternatives

- A JS game inside a WebView. Rejected for this horizon so the exercise matches the store-game stack.
- FastAPI as the authority. Rejected because it cannot reference the C# rules library.
- Unity as a WebView host. Rejected because the player is the game, not an installer.

## Dependency policy

Add a dependency only when the BCL or a package already required by the player cannot do the job. The license must be MIT, Apache-2.0, or BSD, or the Unity package must be under the Unity companion license. It must target `netstandard2.1` if Rules or App reference it, and it must load in Unity 6.3. Do not add a package with a known high-severity advisory. Review rejects a NuGet feed inside the Unity project for the rules library.
