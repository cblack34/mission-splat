# AGENTS.md — Mission Splat

Instructions for the AI agent that plans and builds this project. This file is the source of truth for **how** to work; the active strategic pack listed below defines **what** must be true.

**Code, types, schemas, commands, and file layouts in strategic docs are illustrative guidance, not mandates.** Described behavior, architecture contracts, non-negotiables, and final acceptance are authoritative. Verify implementation details against current official documentation and the live repository.

## What this is

Mission Splat is a two-player tile game for a phone: place a 2×2 splat tile, claim a secret shape only if your placement finished it, first to four claims wins. The POC is an offline Unity 6 player (iOS, Android, and a later desktop build of the same project). Rules live in a `netstandard2.1` library with no Unity reference. There is no server in the POC.

## Prime directive

**Cold-read the active pack and current repository, then propose only the single best next implementation slice before coding.** Preserve the complete strategic outcome and stop when final acceptance passes.

The pack's high-level suggested implementation approach is informed but non-binding. Keep hard causal dependencies, but revise advisory order when current code, tests, or unforeseen constraints justify a better plan. Discuss material replanning with the user.

The user approves each tactical slice. Agents may open PRs. Only the user merges to `main`. Scope, contract, and non-negotiable changes require the user.

## Definition of done

Run these exact commands before every PR is integration-ready and before declaring the project complete:

```bash
dotnet test rules/Barnyard.Rules.sln --configuration Release
dotnet test app/Barnyard.App.sln --configuration Release
```

Every item in [`docs/acceptance.md`](docs/acceptance.md) must also pass. Slice-level checks show progress but never replace final acceptance. Prefer the Unity CLI (`unity test`, `unity build`, and the play-mode verification loop from `unity skill show`) for Unity evidence, and record it in the PR. Device deployment and on-device play stay human.

## Non-negotiables

1. Rules do not reference Unity. The same library must be callable from `dotnet test` and, later, from a server.
2. A claim scores only when the current seat placed the completing tile.
3. Pass-and-play is two human seats on one in-process session, not a network mode.
4. No physical-game name, logo, farm characters, or photographed art ships in the repo or the player.

## Strategic-to-tactical handoff

- The strategic lead and pack own scope, directives, architecture boundaries, research gates, risks, known dependencies, suggested high-level order, and final acceptance.
- The implementation lead understands the full strategy but proposes, plans, and executes only one slice at a time. It retains issue, sequencing, integration, and verification responsibility.
- Create the slice plan, GitHub issues, branches, PRs, or sub-agent assignments only after that slice is agreed and the relevant action is authorized.
- Keep the durable slice plan focused on high-level what and why. Use linked GitHub issues for task checklists, WIP, blockers, assignments, and evidence.
- Give execution sub-agents bounded code and test assignments. Select the least expensive capable model and reasoning effort for each task. They surface surprises to the implementation lead rather than changing scope.
- When evidence invalidates the plan, explain the impact and propose a revision.

## Delivery governance

- A human is the only authority that physically merges to `main` in GitHub. Agents never merge, auto-merge, queue, automate, delegate, or push directly to `main`.
- **Active topology:** direct PRs to `main`. Each user-approved unit is a branch from current `main` and a PR back to `main`.
- Passing checks make a working draft, not a handoff-ready unit. Every delivery unit completes the refactor-before-handoff gate in [`docs/engineering/workflow.md`](docs/engineering/workflow.md) before its PR is declared ready.
- The agent stops after review and green CI for human merge.
- Request GitHub Copilot review first when available; use `pr-review` from the maintainer's skills catalog when it is unavailable; otherwise delegate inline adversarial review to a fresh review sub-agent. The author's own self-review never satisfies the independent gate.
- Use a Conventional Commits PR title. Only a PR to `main` may carry `Closes #N`.

## Always / Ask first / Never

- **Always:** follow [`docs/engineering/workflow.md`](docs/engineering/workflow.md); resolve required research gates; verify unfamiliar APIs against current official docs; run required checks; update affected strategic and descriptive docs with behavior changes.
- **Ask first or stop:** changing active scope, public contracts, non-negotiables, final acceptance, or an architecture boundary; adopting a paid service; making an external or destructive change beyond recorded authority; starting a refactor beyond the current unit's changed surface.
- **Never:** invent repository facts; commit secrets; bypass red verification; merge or push directly to `main`; put source photographs in the repo; implement the room service or `RemoteSession` in the POC; copy the physical game's trade dress.

## Dependencies

Prefer the .NET BCL and Unity packages already required by the player. Add a dependency when it clearly beats hand-rolling and passes the maintenance, license, security, and compatibility policy in [`docs/architecture.md`](docs/architecture.md). Do not take a NuGet feed inside the Unity project for the rules library; consume a local UPM package built from that library.

## Code quality

- Follow [`docs/engineering/code-quality.md`](docs/engineering/code-quality.md).
- Call the session boundary an interface and its implementations adapters. Do not introduce a second vocabulary for the same seam.

## Active build pack

1. [`docs/build-brief.md`](docs/build-brief.md) — outcome, scope, invariants, and the suggested order.
2. [`docs/rules.md`](docs/rules.md) — the behavioral rules contract.
3. [`docs/architecture.md`](docs/architecture.md) — ownership, seams, and the C4 diagram.
4. [`docs/acceptance.md`](docs/acceptance.md) — final verification.
5. [`docs/engineering/workflow.md`](docs/engineering/workflow.md) — delivery governance.
6. [`docs/engineering/code-quality.md`](docs/engineering/code-quality.md) — design rules.
7. [`docs/diagrams/`](docs/diagrams/) — schematic stand-ins for the physical cards. Not a second rules source.
