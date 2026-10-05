# Code quality

Write code a strict senior engineer would approve on the first read. Two named foundations, treated as
**one spine** (they overlap ~80%, so don't juggle multiple vocabularies): **Robert C. Martin** (Clean
Code, SOLID, Clean Architecture) and **ArjanCodes** (cohesion/coupling, program-to-abstractions,
composition over inheritance, separate creation from use, data-first, simplicity/YAGNI). Apply as
judgment tools **only when you touch the code** — one focused change at a time, never a big-bang
refactor of code you did not touch.

These rules apply at two moments: continuous cleanup while you write, and the mandatory
refactor-before-handoff pass in [`workflow.md`](workflow.md), which reviews a delivery unit's complete
changed surface once its behavior passes. Passing tests end the first moment; they do not skip the second.

## Design (module / type level)

- **High cohesion, one reason to change** (SRP): a module/component/function does one job. If you can
  extract another well-named function, it was doing more than one thing.
- **Low coupling, depend on abstractions** (DIP): depend on interfaces/protocols, not concretions;
  inject external dependencies (clock, storage, logger, network) so they can be swapped and tested.
- **Open for extension, not modification** (OCP): adding a variant should touch a known, small set of
  places the compiler or type-checker can flag — never a scattered hunt. Reach for runtime registries
  only for genuinely open-ended, plugin-style extension points.
- **Composition over inheritance**; **separate creation from use** (build objects in one spot, use them
  elsewhere); **data-first** — a single in-memory source of truth, with UI and derived values computed
  from it.
- **Dependency rule / IO at the edges:** pure domain logic imports no UI, framework, or IO code; side
  effects (storage, clock, network, DOM) live in a thin outer ring. This is what makes the fast
  unit-test self-check possible.

## Clean code (unit level)

Intention-revealing names that encode the domain **and** unit where units exist (`widthInches`,
`isRetryEnabled`); booleans read as questions. Small functions that do one thing at one level of
abstraction, few params (group data clumps into typed objects). Queries return and don't mutate;
commands mutate and return nothing meaningful. One home per concept (**DRY**). Comments explain
**why**, not what — refactor confusing code instead of narrating it. No `TODO`/`FIXME` comments: open
the issue and reference its key, or leave the comment out. Delete dead and commented-out code; don't
add generality for a hypothetical second case.

## Project-specific rules (these earn their place)

- Rules and App do not reference `UnityEngine`, `UnityEditor`, or a UI toolkit. Do put Unity types only in the player. Why: the same rules DLL must run under `dotnet test` and, later, in a server.
- Call the session boundary `ISession` and its implementations adapters. Do not add a parallel "port" vocabulary in code. Why: one word for the seam.
- Validate a command at the rules boundary and return a rejected event or result. Do not let the view decide legality. Why: the claim rule is the product.
- Do not put source photographs or the physical product's trade dress in the repository or in Addressables. Do use the schematic diagrams. Why: those assets are not licensed for this project.
- Nullable reference types stay enabled on the rules and app projects. Do not silence a warning with a blanket pragma. Why: illegal placements should be types or results, not nulls discovered in Unity.

## Smells to hunt (in self-review)

- **Universal:** long function, God component / large class, duplicated code, primitive obsession,
  long parameter list / data clumps, feature envy, shotgun surgery (one change touching many files →
  centralize the knowledge), message chains (walking `a.b.c.d` instead of asking the source of truth),
  unclear names, dead / speculative code.
- **This stack:** a `MonoBehaviour` that matches patterns; a rules type that takes a `GameObject`; duplicated claim logic in the AI; a scene that is the source of truth for the board.

## Testing

Unit-test the **pure logic** where it pays off — domain rules, conversions, serialization round-trips,
validation. The final acceptance doc remains the definition of done even when the tactical plan
changes. Verify UI and feel by running the app, not by building heavy component-test scaffolding. If
something is hard to test, that's a design smell — fix the coupling, don't test around it.
