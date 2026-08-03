# Priority 1 Development Plan

The work should proceed as a sequence of small, independently reviewed phases. Each implementation branch should remain buildable, testable, and mergeable on its own. The public API and persistence boundaries should be stabilized before larger scenario and orchestration refactors.

## Phase 1 — Public API Inventory and Contract Definition

Goal: Decide what the project actually intends to expose before changing visibility.

- Generate a complete inventory of public types and members in:
  - FiveEGoldBox.Core
  - FiveEGoldBox.Application
- Record for each public item:
  - assembly and namespace;
  - current production consumers;
  - current test consumers;
  - serialization involvement;
  - whether callers construct it directly;
  - whether it is generic, application-specific, or Watchtower-specific;
  - expected future consumers;
  - recommended visibility.
- Classify each public item as:
  - stable public contract;
  - application-client contract;
  - cross-assembly internal contract;
  - implementation detail;
  - scenario-specific detail;
  - persistence-only shape;
  - unresolved architectural decision.
- Identify Console dependencies on deep Application or Core implementation types.
- Identify types that are public only because tests currently access them.
- Define the intended public API principles:
  - which project owns commands;
  - which project owns identifiers;
  - which results are client-facing;
  - whether callers may construct state records directly;
  - where public factories or facades should replace direct construction.
- Produce an authoritative API inventory and recommendation report.
- Make no production visibility changes in this phase.

**Phase completion gate:**
- Every existing public type has an explicit classification.
- Ambiguous types are escalated for a deliberate decision.
- The first small visibility-reduction cohort is identified.
- No implementation change has yet created compatibility risk.

## Phase 2 — Bounded Public API Reduction

Goal: Reduce the most clearly accidental public surface without attempting one large breaking refactor.

### Step 2.1 — Internalize obvious implementation details

- Select a small first cohort, such as:
  - prerequisite evaluation intermediates;
  - normalization objects;
  - internal lookup/index types;
  - private composition fragments;
  - scenario-specific policy helpers;
  - persistence implementation envelopes.
- Confirm that no legitimate external client depends on each selected type.
- Change only that bounded cohort from public to internal or private.
- Add InternalsVisibleTo only when internal tests genuinely need direct access.
- Prefer testing through public behavior instead of preserving public visibility for tests.

### Step 2.2 — Add or strengthen stable facades

- Identify places where Console currently traverses deep internal object graphs.
- Introduce or clarify narrow Application entry points for:
  - session commands;
  - state queries;
  - save/load operations;
  - combat commands;
  - scenario transitions.
- Keep Core's public surface focused on reusable rules rather than application orchestration.

### Step 2.3 — Continue in small cohorts

- Repeat the process for additional categories.
- Use one branch per coherent API cohort.
- Do not combine unrelated visibility changes.
- Record all intentional source or binary compatibility effects.

**Phase completion gate:**
- The remaining public API is intentional and documented.
- Scenario-specific implementation types are no longer external contracts.
- Console primarily consumes Application-level commands and views.
- No public type remains public solely for test convenience.
- The full solution builds and all tests pass after each cohort.

## Phase 3 — Versioned Persistence Boundary

Goal: Separate the permanent save-game schema from the runtime object model.

### Step 3.1 — Inventory the current serialized graph

- Identify every runtime type and property currently serialized.
- Record: required fields; optional fields; enum values; identifiers; random-state representation; collection ordering; derived or cached data; scenario-specific state.
- Determine which current JSON details must remain compatible.

### Step 3.2 — Define the V1 save schema

- Create persistence-specific DTOs such as: SaveGameV1; SaveSessionV1; SavePartyMemberV1; SaveScenarioProgressV1; SaveEncounterV1; SaveRandomStateV1.
- Keep DTOs focused only on durable state.
- Exclude: runtime indexes; caches; derived totals; policy objects; orchestration helpers; temporary command state.
- Use stable string or numeric identifiers rather than class or namespace names.

### Step 3.3 — Add explicit runtime-to-save mapping

- Map validated runtime state into SaveGameV1.
- Ensure mapping is deterministic.
- Ensure collection ordering is intentional and stable.
- Validate that every persisted field is required for reconstruction.

### Step 3.4 — Add explicit save-to-runtime mapping

- Deserialize JSON into SaveGameV1.
- Validate the DTO before runtime construction.
- Map DTOs into current runtime state.
- Rebuild derived indexes and values rather than persisting them.
- Reject: unsupported versions; malformed identifiers; invalid enums; contradictory state; impossible random-state values.

### Step 3.5 — Add historical fixtures

- Preserve representative V1 JSON files in the test project.
- Include fixtures for: normal outpost state; active travel or exploration; active combat; completed scenario; deterministic continuation after load.
- Prove: existing V1 fixtures load; load-save-load preserves semantic state; runtime refactors do not alter V1 JSON unintentionally; malformed fixtures fail deterministically.

### Step 3.6 — Prepare migration architecture

- Add a version-based loader dispatcher.
- Do not implement speculative V2 migration yet.
- Establish the future path: `JSON → version-specific DTO → migration when needed → current runtime state`.

**Phase completion gate:**
- Runtime records are no longer serialized directly.
- V1 has an explicit schema and loader.
- Historical fixtures are permanent regression assets.
- Runtime properties can be reorganized without silently changing save format.
- Atomic write behavior remains intact.

## Phase 4 — Session Validation by Lifecycle Mode

Goal: Localize state invariants before decomposing more application behavior.

### Step 4.1 — Inventory existing session invariants

- List all current ApplicationSessionState validation rules.
- Classify each rule as: universal; outpost-specific; travel-specific; exploration-specific; combat-specific; completion-specific; Watchtower-specific.
- Identify contradictory-state combinations currently prevented only by nested conditional logic.

### Step 4.2 — Extract universal validation

- Keep shared validation for: party identity uniqueness; persistent health; location identifiers; scenario identifiers; random state; collection ownership; general numeric ranges.

### Step 4.3 — Introduce mode-specific validators

- Add focused validators such as: OutpostSessionValidator; TravelSessionValidator; ExplorationSessionValidator; CombatSessionValidator; CompletedScenarioValidator.
- Add a small dispatcher based on session mode.
- Give each validator authority over only its mode's invariants.

### Step 4.4 — Strengthen invalid-state tests

- Add tests for: required substate missing; forbidden substate present; incompatible mode and location; active combat without encounter state; completed scenario with active encounter; mode-specific scenario progress contradictions.
- Keep error identities deterministic.

### Step 4.5 — Defer state-type redesign

- Do not immediately replace ApplicationSessionState with a class hierarchy or discriminated union.
- First prove that separated validation reduces complexity.
- Reassess mode-specific state types after later scenario work.

**Phase completion gate:**
- Each lifecycle mode has localized invariants.
- The central validator acts primarily as a dispatcher.
- Adding a new mode no longer requires expanding one large conditional validator.
- Combat orchestration can rely on a clearly validated combat-session contract.

## Phase 5 — Watchtower Combat Orchestrator Decomposition

Goal: Separate coordination, policy, movement, attacks, and outcome mapping without changing behavior.

### Step 5.1 — Create a responsibility map

- Inventory every method and responsibility in WatchtowerCombatOrchestrator.
- Classify logic as: command validation; player action resolution; enemy policy; target selection; movement/pathfinding; attack resolution; deterministic random consumption; turn completion; encounter outcome; persistent session update; Watchtower-authored content.

### Step 5.2 — Lock behavior with characterization tests

- Preserve deterministic examples for: target selection; path tie-breaking; movement toward attack range; player command resolution; enemy turns; attack resolution; ammunition; unconscious targets; victory and defeat; random-state advancement; save/load continuation.
- Do not begin extraction until current behavior is demonstrably protected.

### Step 5.3 — Extract pure policy components

Recommended initial components: WatchtowerTargetSelectionPolicy; WatchtowerMovementPlanner; WatchtowerRaiderTurnPlanner.

Keep them: internal; deterministic; side-effect free; explicit about inputs and outputs.

### Step 5.4 — Extract action-resolution components

- WatchtowerPlayerCommandResolver; WatchtowerAttackResolver; any bounded movement or action application helper.
- Ensure Core still resolves universal combat mechanics.

### Step 5.5 — Extract outcome and session mapping

- Add WatchtowerCombatOutcomeMapper.
- Keep session persistence and scenario transitions outside low-level attack logic.
- Ensure victory, defeat, and continuation map into valid mode-specific session states.

### Step 5.6 — Reduce the top-level orchestrator

The remaining orchestrator should primarily: validate the current session; delegate the requested operation; coordinate deterministic random consumption; assemble the updated session; return the public application result.

**Phase completion gate:**
- The top-level orchestrator is a coordinator rather than a policy container.
- Generic combat mechanics remain in Core.
- Watchtower-specific policy is clearly identified.
- Deterministic behavior remains byte-for-byte or semantically equivalent.
- A future encounter can reuse execution components without copying the full orchestrator.

## Phase 6 — Scenario Content Boundary

Goal: Separate authored Watchtower data from the application code that executes it.

### Step 6.1 — Inventory hard-coded content

- Identify hard-coded: scenario IDs; location IDs; maps; starting positions; enemies; objectives; rewards; narrative strings; transitions; encounter parameters.
- Distinguish content from execution policy.

### Step 6.2 — Define an in-memory scenario model

Create immutable definitions for concepts such as: ScenarioDefinition; LocationDefinition; EncounterDefinition; CombatantDefinition; ObjectiveDefinition; TransitionDefinition; RewardDefinition.

Do not externalize to JSON yet.

### Step 6.3 — Add scenario-definition validation

Validate content when loaded: unique identifiers; valid references; supported dice; reachable transitions; valid maps and coordinates; valid combatant definitions; valid starting state; valid victory and failure destinations.

### Step 6.4 — Convert Watchtower into data

- Move authored Watchtower constants into a definition factory or provider.
- Remove scenario-name checks from execution code.
- Keep scenario execution generic where behavior is truly common.
- Keep genuinely unique Watchtower policy explicitly scenario-specific.

### Step 6.5 — Prove the boundary with a minimal second definition

- Create a small test-only or minimal alternate scenario. It does not need full narrative or balance.
- Use it to prove: identifiers are not hard-coded; execution can consume another definition; scenario transitions are data-driven; Watchtower assumptions are not embedded in generic flow.

### Step 6.6 — Consider external content later

- Reassess JSON or other external content formats only after the in-memory model is stable.
- Avoid freezing an immature content schema prematurely.

**Phase completion gate:**
- Watchtower content is supplied through a defined content boundary.
- Execution code does not depend on scattered Watchtower constants.
- A second minimal scenario can use the same execution architecture.
- Scenario definitions are validated before play begins.

## Phase 7 — Cross-Layer Die Capability Alignment

Goal: Ensure every die accepted by Core can be resolved by Application, or is rejected before runtime play.

### Step 7.1 — Inventory die capabilities

Compare support across: Core rule definitions; weapons and damage dice; scenario definitions; deterministic random source; Application command resolution; serialization.

### Step 7.2 — Implement the preferred policy

Support all Core dice in Application: D4, D6, D8, D10, D12, D20.

- Preserve deterministic sequence semantics.
- Define how random state advances for each roll.
- Reject undefined DieType values explicitly.

### Step 7.3 — Add capability validation

- Scenario content validation should reject unsupported dice.
- This remains valuable even when all current Core dice are supported.
- Future die additions must fail at content-load time until Application supports them.

### Step 7.4 — Add tests

- One deterministic test per supported die.
- Mixed-die sequence tests.
- Save/load continuation tests involving newly supported dice.
- Unsupported or undefined die rejection tests.

**Phase completion gate:**
- No Core-valid die fails unexpectedly during action resolution.
- Unsupported future dice are rejected when content is loaded.
- Deterministic continuation remains stable across save/load.

## Phase 8 — Incremental Test Infrastructure Improvement

Goal: Reduce repetitive fixtures without hiding meaningful setup. This should run throughout Phases 2–7 rather than waiting until the end.

### Step 8.1 — Identify repeated setup

Prioritize repeated construction of: rulesets; characters; encounter states; application sessions; Watchtower scenarios; combat commands; save states.

### Step 8.2 — Add narrowly scoped builders

Potential builders: RulesetBuilder; CharacterDefinitionBuilder; EncounterStateBuilder; ApplicationSessionBuilder; WatchtowerScenarioFixture; CombatCommandFactory; SaveGameFixture.

### Step 8.3 — Add focused assertions

Potential assertions: AssertSessionEquivalent; AssertDeterministicContinuation; AssertUnavailableBecause; AssertCollectionsAreProtected; AssertValidSession; AssertSaveRoundTrips.

### Step 8.4 — Refactor only related tests

- Introduce a builder when a production branch needs it.
- Convert a representative subset first.
- Avoid rewriting hundreds of unaffected tests in one branch.
- Keep relevant values visible in each test.
- Avoid hidden, magical defaults.

### Step 8.5 — Split oversized test files

Split by behavior: validation; command handling; target selection; movement; attack resolution; persistence; deterministic continuation; ownership and mutation resistance.

**Phase completion gate:**
- Repetitive construction is reduced.
- Tests remain readable without opening the builder implementation.
- Required-property changes no longer break unrelated fixtures broadly.
- No general-purpose "test language" has replaced straightforward test setup.

## Review Workflow for Every Implementation Phase

See [CLAUDE.md](../CLAUDE.md) for how this workflow is actually run when working with Claude Code — it supersedes the disconnected-session ceremony described below for that context.

Each production phase should use the same controlled workflow:

- Confirm exact main commit and clean tracked tree.
- Create one narrowly named branch.
- Produce an implementation assignment packet.
- Have the implementation specialist inspect the exact baseline before editing.
- Require: focused tests; complete affected-project tests; full solution tests; Release build; git diff --check; exact patch hash; implementation handoff.
- Perform independent test review.
- Perform independent architecture and public API review.
- Apply corrections only when demonstrated.
- Commit the exact reviewed artifact.
- Push and open a pull request.
- Require CI success.
- Merge and synchronize main.
- Record the new authoritative baseline.

## Recommended Branch Sequence

- review/inventory-public-api
- refactor/internalize-core-evaluation-types
- refactor/internalize-application-implementation-types
- feature/add-versioned-save-v1-dtos
- test/add-v1-save-compatibility-fixtures
- refactor/split-session-validation-by-mode
- refactor/extract-watchtower-target-and-movement-policy
- refactor/extract-watchtower-action-resolution
- refactor/extract-watchtower-outcome-mapping
- feature/add-scenario-definition-boundary
- refactor/move-watchtower-content-to-definition
- test/prove-second-scenario-definition
- fix/unify-application-die-support

Test builders should be included only where each branch actually needs them.

## Overall Priority 1 Completion Standard

Priority 1 should be considered complete when:

- the intended public API is documented and substantially smaller;
- remaining public types are deliberate contracts;
- save files use explicit versioned DTOs;
- historical V1 saves remain loadable;
- session validation is localized by mode;
- Watchtower combat responsibilities are clearly separated;
- scenario content is independent from scenario execution;
- a second scenario definition can use the architecture without copying Watchtower;
- all Core dice are supported or rejected during content validation;
- repetitive tests use restrained shared infrastructure;
- the solution builds with zero warnings;
- all tests and CI checks pass;
- no Priority 0 protections have regressed.
