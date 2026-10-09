# Slice plan — Pack power timing

## Strategic source

- **Active build pack:** [`docs/build-brief.md`](../../build-brief.md), [`docs/rules.md`](../../rules.md), [`docs/architecture.md`](../../architecture.md), [`docs/acceptance.md`](../../acceptance.md), [`docs/engineering/workflow.md`](../../engineering/workflow.md), [`docs/engineering/code-quality.md`](../../engineering/code-quality.md)
- **Human approval:** Approved in session on 2026-10-09, after the maintainer walked the physical game turn by turn against the shipped rules. Rulings given in that session: rotate and bounce are used before the drawn tile is placed, one use at a time, on tiles already on the board; any board tile is a target and the drawn tile never is; a bounce may empty the board, and an empty board accepts the drawn tile only at the origin; stack is the placement on an occupied position; only the placed tile's written cells complete a mission; a tile showing two different powers is a ruleset question deferred until a deck has one; the match accepts one action type per ruleset, never a string; the powers in play are setup data; the Unity player is setup, input, and render only.
- **Final acceptance advanced:** None passes as a result of this slice. It corrects the contract the later slices are judged against: the rotate, bounce, and stack lines in [`docs/acceptance.md`](../../acceptance.md), a new empty-board check, and one event per power use.

## Outcome

The strategic pack describes the game the maintainer actually plays. `rules.md` states the corrected turn order and power rulings. `acceptance.md` checks them. `build-brief.md` and `architecture.md` name the match/rules-engine seam inside Rules, place the session, turn driver, and deck content in App, and reduce the Unity player to setup, input, and render, with an illustrative pseudocode sketch of the agreed contract. No code changes.

## Why this slice is next

The shipped `rules.md` Turn step 3 and the `Game` commands from the rotate (#16) and bounce (#18) slices encode power use after placement. The physical game uses powers before placement, which the maintainer confirmed with a first-turn bounce that empties the board. Three shipped rulings follow from the wrong order — self-bounce rejection, "a bounce can never empty the board", and rotate's surrounded check counting the unplaced tile — and the open "claim by stack or bounce" ruling resolves once the order is right. The workflow forbids designing against an open gate, so the pack is corrected before any code encodes it. Rotate and bounce were never wired into App or Unity, so the code correction that follows is confined to Rules and its tests.

## Scope

### In scope

- `rules.md`: the turn order; rotate and bounce as pre-placement powers on any board tile; the drawn tile never a target; board-emptying allowed and the origin rule for an empty board; stack restated as the placement on an occupied position; one use per power cell, chosen one at a time; claims from the placed tile's written cells only; powers in play as setup data with an absent symbol acting as a blank; the open list updated.
- `acceptance.md`: the rotate, bounce, and stack lines restated for the new timing; an empty-board origin check; one event per power use.
- `build-brief.md`: the architecture boundaries name the rules engine and the match inside Rules, the session, turn driver, and content inside App, and the Unity player as setup, input, and render; the board view comes from the match; one action type per ruleset. The assumptions list drops the self-bounce reasoning.
- `architecture.md`: the roles, the action/event/query contract, the rule that the GUI never decides legality, the composability direction, the illustrative pseudocode, and a shipped-state note naming the lags later slices remove.

### Out of scope

- Any code, test, or Unity asset.
- The historical slice plans and their delivery records. They stay as written.
- The census, deck exhaustion, and a drawn tile with no legal placement. Still open.
- Choosing between preshipped rulesets and per-power toggles. Recorded as a direction, decided when a second ruleset is wanted.
- A `PowersInPlay` query. Belongs to the session or GUI slice.

## Strategic traceability

| Strategic requirement or criterion | How this slice advances it |
| --- | --- |
| Rules in `rules.md` are the behavioral contract | The Turn, Powers, Placement, and Claim sections say what the physical game does. |
| Claim is placement-owned | Restated unchanged, and extended: a power reshapes the board but never claims on its own. |
| Rules stay engine-free; a server can judge a move | The match/rules-engine seam is named inside the Rules library, where a server would reference it. |
| Pass-and-play is local | The turn driver that hands the device between seats is placed in App, not the Unity player, so any GUI gets it. |
| A later room service is a seam, not a deliverable | One action type and one event list per ruleset are the recorded contract; `RemoteSession` is a second adapter behind `ISession`. |
| Acceptance is the definition of done | The power lines are corrected before any slice is judged by them. |

## Gates and dependencies

### Hard gates

- The maintainer's rulings of 2026-10-09, listed under Human approval. Nothing beyond them is invented here; a question they do not answer stops the slice.

### Sequencing recommendations

- This slice first. The corrected Rules commands, the Rules split, the App session and turn driver, and the power UI depend on the contract it records, in that advisory order.

## Architecture and contracts

- **Affected seams:** Documentation of every seam. Rules gains a named inner division: a stateless rules engine that judges one action against the board, the drawn tile, and the acting hand, and a match that owns seats, hands, decks, claims, turn, and turn phase. App gains the turn driver and the deck content. The Unity player loses both.
- **Public contracts:** Recorded as the agreed target, to be made true by later slices: the match accepts one `Apply(seat, action)` with a closed action set and returns events; `ISession` submits that action and exposes the legal-move queries; `IPlayer` is for automated seats; the board view comes from the match. `Place(int, int, int)` and the existing event types are not changed by this slice.
- **Data and migration considerations:** None. There is no saved game.

## High-level approach

Write the rulings into `rules.md` as plain sentences in the sections that already own them, replacing the after-placement wording rather than adding exceptions beside it. Restate the three acceptance lines so each automated check names the pre-placement order. Amend the build brief's boundary list, which is the authoritative statement, and let `architecture.md` carry the roles, contract, and pseudocode as illustrative guidance with a short note of what the shipped code has not yet reached. Obtain a fresh-context read of the four documents for contradictions before the PR is declared ready.

## Verification

- Every amended sentence traces to a ruling listed under Human approval.
- No sentence in `rules.md` contradicts another, and `acceptance.md`, `build-brief.md`, and `architecture.md` agree with it.
- Relative links in the amended documents resolve.
- Repository definition-of-done commands remain mandatory; they are run for the record though no code changes.

## Risks and stop conditions

- Stop if drafting exposes a sequencing or power question the maintainer's rulings do not answer. Ask; do not invent.
- Stop if the amended boundaries would require changing a non-negotiable in `AGENTS.md`.
- The closure of the "claim by stack or bounce" ruling is a derivation, not a direct maintainer sentence; it is flagged for confirmation on the PR.

## Execution issues

GitHub issues are the WIP tracker and source of task-level detail. Every issue for this slice carries the `slice:pack-power-timing` label; this plan links the query, not the issues: https://github.com/cblack34/mission-splat/issues?q=label%3Aslice%3Apack-power-timing

## Delivery shape

- **Topology:** Direct PRs to `main`.
- **Human merge gate:** Only the human may physically merge any PR whose base is `main`. Agents must stop when it is ready.

### Direct PRs

- **Branch:** `docs/power-before-placement`, cut from `origin/main`.

## Amendments

None.

## Delivery record

Complete once when the final PR is ready for human merge. Do not use this section for WIP status.

- **Outcome:**
- **Verification:**
- **Deviations:**
- **Unresolved gates or risks:**
- **Refactor and handoff receipt:**
- **Final PR:**
