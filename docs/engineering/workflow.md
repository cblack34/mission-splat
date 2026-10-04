# Workflow — strategic handoff and delivery

The build pack is the durable strategic contract. It defines the outcome, scope, non-negotiables, architecture boundaries, research conclusions and gates, risks, dependencies, final acceptance, and a high-level suggested implementation approach. The implementation lead owns tactical decomposition with the user.

Treat suggested order as informed guidance, not a command. Preserve hard causal dependencies, but revise advisory sequencing when current code, tests, or unforeseen constraints justify a better route.

## Authority model

- **Strategic lead:** sets outcomes, guardrails, company-wide delivery governance, and final acceptance.
- **Implementation lead:** proposes and manages slices, actual order, integration, replanning, and delegation.
- **Execution agents:** complete bounded assignments and surface surprises; they do not change wider scope or architecture.
- **Human:** approves each tactical slice and is the only authority that physically merges to `main` in GitHub. This authority cannot be delegated.

## Before implementation

1. Read `AGENTS.md` and every active strategic document in order.
2. Inspect current code, tests, manifests, CI, branch state, and repository rules.
3. Resolve required research gates or return them to the user; do not adopt an irreversible design while its gate is open.
4. Evaluate the suggested implementation approach against repository reality.
5. Select and propose only the single best next slice: its outcome, boundaries, rationale, checks, risks, delivery shape, and useful delegation.
6. Discuss and revise that slice with the user before coding or creating its plan, issues, branches, PRs, or agent assignments. Do not build an upfront execution backlog for the full feature.

When evidence invalidates the plan, stop affected work, explain the impact, and propose a revision. Do not force reality to match the original suggestion.

## Delivery topology

Active topology: **direct PRs to `main`**.

Branch naming: `type/short-topic`, with `type` of `feat`, `fix`, `test`, or `docs`. Branches are owned by the agent that opened them until the human merges the PR.

1. Create a branch for the next user-approved tactical unit from current `main`.
2. Implement the unit, tests, and affected docs; run self-verification, the refactor-before-handoff gate, and self-review.
3. Sync with `main`; stop on a non-trivial conflict rather than guessing or force-pushing.
4. Open a PR to `main`, run the review loop, and require green CI.
5. Stop for the human to merge. Confirm the resulting state before starting dependent work.

The agent never merges a PR whose base is `main`, enables auto-merge, uses a merge queue, calls a merge API, automates the GitHub merge UI, pushes directly to `main`, or delegates any of those actions. When the PR is ready, the human physically pushes the merge button in GitHub.

## Delegation

The implementation lead retains slice selection, plan, issue, ordering, integration, and verification ownership. Delegate production code and test writing as concrete bounded assignments after the slice is approved. Size every assignment independently and use the least expensive capable model and reasoning effort; do not inherit an expensive primary configuration by default. Give each execution agent its scope, exclusions, constraints, interfaces, expected result, checks, branch ownership, and stop conditions. Require a concise return receipt with changed files, verification, decisions, surprises, and a commit or diff reference. Review delegated work before integration; a green receipt makes the assignment draft-complete, not handoff-ready. If capable delegation is unavailable, stop and tell the human instead of silently moving implementation into the primary context.

## Build and self-verify

For each tactical unit:

1. Research unfamiliar or drift-prone APIs in current official documentation.
2. Implement only approved active scope and preserve strategic invariants.
3. Add or update tests and affected docs in the same change.
4. Run every command in the `AGENTS.md` definition of done until green. Green proves the draft works; it does not make the unit handoff-ready.
5. Review the diff against [`code-quality.md`](code-quality.md), strategic acceptance, and the unit's derived checks.
6. Complete the refactor-before-handoff gate below.

## Refactor before handoff

A behaviorally passing implementation is a working draft. Implementation may begin rough and become clearer as behavior is discovered; the defect to prevent is handing off the rough draft because its tests pass. Before a unit's PR is declared ready:

1. Read every changed production file in full, not just its diff hunks, and the owning abstractions and callers needed to judge responsibility, against [`code-quality.md`](code-quality.md): cohesion and reasons to change, dependency direction and boundary placement, creation versus use, IO at the edges, avoidable complexity, duplication, package structure, and testability. No line-count threshold decides what is reviewed.
2. Apply the behavior-preserving refactors that review justifies. Do not split files, add abstractions, or restructure packages for their own sake; the exact refactors are implementation decisions.
3. When packages or subpackages were added or moved and the stack produces distributable artifacts, verify manifests, built-artifact contents, and a clean-install import.
4. Obtain a fresh-context, read-only design review of the complete changed surface from a context that did not author it, reporting explicit changed-file coverage. This is separate from author self-review and from the PR review below, which remains the correctness, security, and test gate. Use the `build-pack` plugin's `design-reviewer` agent when it is installed; otherwise a fresh read-only sub-agent given `code-quality.md`, the changed-file list, and the receipt below; otherwise stop for an explicit human design review.
5. Validate every finding against the code before acting. Apply correct findings; reject weak ones with evidence.
6. After every structural edit run the cheapest focused executable check first, then the complete `AGENTS.md` definition of done before handoff.
7. Record the receipt in the unit's issue and PR:

   ```markdown
   ## Refactor and handoff receipt
   - Changed production files reviewed:
   - Owning abstractions/callers reviewed:
   - Cohesion/SRP findings:
   - Structural changes made:
   - Findings rejected and evidence:
   - Package/artifact verification:
   - Post-refactor focused checks:
   - Complete repository checks:
   - Fresh-context design review:
   - Remaining risks or justified debt:
   ```

`Structural changes made: none` is valid only when the receipt identifies the reviewed surface and explains why current responsibilities remain cohesive. A unit without this receipt is not PR-ready, regardless of CI or review state.

## Pull request mechanics

- Use one PR for a complete, reviewable delivery unit chosen by the implementation lead; do not let this rule predetermine feature slicing.
- Follow repository branch naming and use a Conventional Commits PR title.
- Do not write a person's name, handle, or email into PR bodies, receipts, issues, review replies, or commit messages. Refer to roles or omit attribution; Git author metadata and GitHub login are the only attribution. Leave names already present as they are. Exceptions: the request asked for a name, or a required owner or contact field has no role that fits.
- In the PR body, state scope, verification evidence, the refactor and handoff receipt, material risks or deviations, documentation changes, and related issues.
- Use `Closes #N` only on a PR whose base is currently `main`.
- Re-sync the PR base and re-run relevant checks before review. Stop on non-trivial conflicts and never force-push a protected/shared branch.

## PR review loop

Use this reviewer precedence:

1. **GitHub Copilot code review first** when it is available for the repository. It is the preferred external reviewer because it adds no per-review model cost to this workflow. Request it from the PR review menu. Availability on this private repository has not been verified; the first code PR records whether the request produced a review.
2. **`pr-review` skill fallback** when Copilot review is unavailable, including private repositories where it is not enabled, or when Copilot cannot produce a usable review. If `address-pr-review` is available, use it to address the resulting comments; otherwise follow the inline mechanics below. Both skills live in the maintainer's skills catalog and are optional here.
3. **Fresh review sub-agent fallback** only when neither Copilot nor `pr-review` is available. Delegate an independent inline adversarial review with bounded context: the PR diff, relevant strategic constraints, acceptance criteria, and verification evidence. Use the least expensive capable model/effort for the bounded review. If sub-agent delegation is unavailable, stop for human review; the author's self-review is not an independent review.

The selected reviewer changes the review source, not the quality gate. The clean-HEAD and address/reply/resolve requirements below always apply.

The **address → reply → resolve** flow is mandatory regardless of reviewer:

- **Settle and trust the right snapshot.** Wait briefly after requesting because a review can arrive as a partial snapshot. Count **all** unresolved review threads. Trust a review only when its commit id matches PR **HEAD**; a stale review does not count.
- **Evaluate every comment.** Fix in-scope issues in the branch. Return scope-changing feedback to the user. For valid deferred work, open a follow-up issue only when issue creation is authorized; otherwise record it in the PR handoff.
- **Reply in every thread, then resolve it.** Push the fix or explain the disposition before resolving—even when pushing back. Resolve before re-requesting so the next pass begins clean.
- **Re-run self-verification and CI** after every code change prompted by review.
- **A base change invalidates review.** After a PR is synchronized with its base or retargeted, a review against the previous base/head relationship is stale; re-run affected checks and re-request review.
- **Re-request review and wait for a zero-new-comment, HEAD-matched pass** before any permitted merge. Resolving the first batch alone is not a clean review.
- **Bound the loop:** at most three request → address cycles total per stable base/head relationship, with a reasonable wait each. Ordinary pushes that address review comments do not reset this count; only a base synchronization or retarget establishes a new base/head relationship and starts a fresh bounded loop. If Copilot is unavailable or does not post a usable HEAD review within a reasonable wait, switch to `pr-review`; changing reviewers does not reset the bound. If neither reviewer can complete, delegate the fresh review sub-agent, note that fallback in the PR, and never merge with a genuine unresolved issue.

## CI bootstrap

CI must run the `AGENTS.md` verification commands on every PR. If the repository lacks CI, the implementation lead must propose CI bootstrap as the first code-bearing delivery unit. That initial PR is gated by complete local verification and review because CI does not yet exist; every later merge requires green CI. Keep CI minimal and do not spend CI time on artifacts nobody consumes. Required-status enforcement may be unavailable on the repository plan, so "never integrate on red CI" remains mandatory agent discipline even without a server-side gate.

This repository has no CI yet. The intended workflow is GitHub Actions running the two `dotnet test` commands from `AGENTS.md`. Unity player builds are not required in CI for the POC.

## Final verification

Slice checks prove progress but never replace [`../acceptance.md`](../acceptance.md). Before the human `main` merge, run every final command and acceptance check on the direct PR branch. Reconcile shipped behavior with strategic and descriptive docs. Report unresolved gates, accepted risks, deviations from suggested order, and deferred scope.

## Stop and return to the user

Stop affected work when:

- a directive, non-negotiable, or final criterion conflicts with implementation;
- a research gate is unresolved;
- current code requires an unapproved public-contract, architecture-boundary, scope, or material risk change;
- a paid service, incompatible license, destructive action, secret, production write, or a refactor beyond the unit's changed surface is required;
- two consecutive PRs fail, or the same test flakes across two runs;
- review finds a genuine unresolved issue or exhausts the fallback;
- integration needs a non-trivial conflict resolution or force-push;
- an external action exceeds the recorded authority;
- a bad merge already landed on `main`; open a revert PR, then stop and report;
- a rules edge listed as open in [`../rules.md`](../rules.md) would have to be invented to proceed.

Everything else within the approved plan: keep going until the current delivery gate is reached.
