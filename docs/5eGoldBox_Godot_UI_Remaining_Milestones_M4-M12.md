# 5eGoldBox Godot UI — Remaining Milestones (M4–M12)

**Status:** M0–M3 complete; M4 in progress (a done); eight milestones remain after M4.

This is a concise proposed execution-stage breakdown derived from the creator-approved Godot UI Pre-Integration Governing Plan v1.1. Lettered stages organize implementation; they do not change the governing milestone scope or acceptance gates.

## M4 — Input Router, Focus, and Interaction-State Framework

**Objective:** Make player input deterministic, context-aware, and consistent across the entire UI.

- [x] a. Define the complete player InputMap action inventory and migrate raw player-key handling. `ui/input/PlayerInputActions.cs` registers every fixed player/system/dev-shortcut binding as a named Godot InputMap action in code (compiler-checked `Key` values, not a hand-authored `project.godot` resource block). `ShellInputRouter`, `AppShell.Input.cs`, and `SelectionList.Input.cs` now check `keyEvent.IsActionPressed(...)` against those names instead of comparing raw `Key` values; `ShellInputRouter.Handle` takes the `InputEventKey` itself rather than a decomposed keycode/ctrl/alt triple. Godot's own built-ins (`ui_cancel`, `ui_up`, `ui_down`) are referenced by name rather than reimplemented. Verified: `dotnet build` clean (0 warnings/errors), grep confirms zero remaining raw `Key.*` comparisons in the touched files, all files still ≤250 lines.
  - **Deliberately deferred to c/d, not done here:** `ModalBackdrop`'s Tab-cycle and Escape-dismiss handling still compare raw `Key.Tab`/`Key.Escape` directly. Its focus-traversal and cancel-routing logic is getting restructured in M4c/M4d anyway, so converting only the detection mechanism now would be low-value churn ahead of that redesign.
  - **Deliberately left alone:** the per-command hotkeys in `CommandDefinition`/`HotkeyCommandButton` (e.g. `T` for Travel, `M` for Move) — these are content-driven, differ per active command set, and already ride Godot's own `Shortcut` mechanism correctly; InputMap actions are for fixed global bindings, not per-screen dynamic ones. Their unique-per-active-set validation is M4e's job. Dev-tool `Key.H` checks in `ui/dev/*Demo.cs` (help toggles in QA scaffolding, not player controls) were left as-is too.
- [ ] b. Formalize interaction and focus contexts for commands, movement, lists, targeting, modals, and dialogs.
- [ ] c. Centralize Escape/cancel priority and modal-stack routing.
- [ ] d. Implement deterministic keyboard focus traversal and unmistakable focus presentation.
- [ ] e. Validate active-command hotkey uniqueness and prevent inactive contexts from consuming input.
- [ ] f. Preserve direct MOVE behavior and add optional gamepad-navigation scaffolding.
- [ ] g. Complete keyboard/mouse parity, cancel-priority, context-isolation, and movement-mode verification.

## M5 — Mock Gateway and Scenario Laboratory

**Objective:** Provide deterministic UI data and command responses for every planned screen.

- a. Finalize UI-owned immutable view models and the UiCommandIntent contract.
- b. Implement the UI-local mock gateway for snapshots and command intents.
- c. Build the named deterministic scenario catalog for normal, empty, long, disabled, error, and stress states.
- d. Add the developer scenario picker plus layout and resolution controls.
- e. Add scripted transitions among exploration, regional travel, combat, and modal screens.
- f. Add latency, busy, recoverable-error, and success placeholders.
- g. Verify every major UI state can be reached quickly without code edits or backend types.

## M6 — Exploration Experience Shell

**Objective:** Complete the mock-driven local exploration experience.

- a. Build the reusable ExplorationView presentation scene.
- b. Add building, town, dungeon, and cavern visual mock variants.
- c. Drive the exploration command bar from data rather than fixed scene layout.
- d. Route MOVE arrows/numpad through movement intents; restore commands with Escape or Space.
- e. Add View, Cast, Area, Encamp, Search, Look, and local-interaction shells.
- f. Add facing, compass, and local-status presentation if creator-approved.
- g. Verify the full exploration loop in both layouts, including long-text and six-member stress cases.

## M7 — Regional Travel Experience Shell

**Objective:** Complete the overland map presentation and navigation experience.

- a. Build RegionalMapView with map, party marker, locations, cursor, and selection.
- b. Implement keyboard/mouse pan, zoom, selection, and inspection behavior.
- c. Display supplied visual route previews without calculating authoritative paths.
- d. Add enter, travel, search, camp, inventory, and journal command shells.
- e. Add deterministic transitions between local exploration and regional travel.
- f. Add location and route tooltips/inspection panels.
- g. Verify readability, navigation, and layout parity across approved resolutions.

## M8 — Tactical Combat Experience Shell

**Objective:** Complete tactical combat presentation and interaction without implementing combat rules.

- a. Build CombatView with a bird's-eye tactical grid and camera controls.
- b. Render supplied combatants, active actor, status indicators, and result messages.
- c. Display supplied movement ranges, target highlights, and path previews.
- d. Add configurable Move, Attack, Cast, Use, Defend/Guard, and End Turn command sets.
- e. Implement selection, targeting, confirmation, cancel, and target-cycling flows.
- f. Add completed-combat presentation states.
- g. Verify all major combat states in standard and immersive layouts without inferring legality.

## M9 — Secondary Screens and Location Interactions

**Objective:** Complete the menu-driven screens required for a full Gold Box-style skeleton walkthrough.

- a. Establish a shared secondary-screen shell, navigation pattern, and cancel/back behavior.
- b. Build Character/Party and Inventory/Equipment shells.
- c. Build Spellbook/Casting and Area Map shells.
- d. Build Encamp/Rest and Journal/Objective shells.
- e. Build Options/Help/Controls and mock Save/Load shells.
- f. Build the reusable location-interaction screen for inns, shops, temples, training, dialogue, rewards, and services.
- g. Verify shared dialogs, both layouts, and empty/short/long/disabled/selected/overflowing list states.

## M10 — Standard/Immersive Equivalence and Presentation Polish

**Objective:** Make both layouts complete, synchronized, readable, and production-intentional.

- a. Audit every presentation mode, secondary screen, and major modal in both layouts.
- b. Eliminate duplicated state and ensure shared models/presenters drive both layouts.
- c. Finalize overlay opacity, safe zones, visibility, auto-hide, and focus rules.
- d. Clarify immersive MOVE, selection, and targeting prompts.
- e. Add restrained layout and presentation transitions with reduced-motion alternatives.
- f. Review reparenting, focus, signal, lifecycle, and container recalculation risks.
- g. Verify exact interaction-state preservation across F11 and readability over bright/dark backgrounds.

## M11 — Accessibility, Localization Readiness, and User Preferences

**Objective:** Make the client adaptable before backend and content complexity arrive.

- a. Implement separate UI-only preference storage.
- b. Add font scaling and validate large-text layouts at the minimum resolution.
- c. Complete high-contrast and reduced-motion behavior.
- d. Replace color-only communication with text and/or icon alternatives.
- e. Centralize player-facing strings and add pseudo-localized long-text scenarios.
- f. Add control-remapping data structures and at least the screen/framework needed to manage them.
- g. Verify core flows with large text, non-color cues, preferences, and pseudo-localized content.

## M12 — UI-Only Verification, Performance, and Integration Readiness

**Objective:** Close the decoupled UI program with objective evidence and a stable integration seam.

- a. Add pure C# tests for state machines, command validation, cancel priority, transitions, and mock behavior.
- b. Add Godot scene smoke tests or a deterministic automated launch harness.
- c. Capture the required scenario, interaction-state, layout, and resolution screenshot matrix.
- d. Measure idle and interaction performance on the integrated GPU and RTX workstation when available.
- e. Define and approve the UI-owned integration port and mapping responsibilities.
- f. Produce the final scene inventory, public UI-model inventory, known-deferred list, and pre-integration handoff.
- g. Run final acceptance, confirm zero backend coupling, and create the pre-integration baseline.

## Dependency Sequence

- **Behavior platform:** M4 → M5
- **Primary modes:** M6, M7, M8
- **Supporting experience:** M9 → M10
- **Hardening and integration readiness:** M11 → M12

Throughout M4–M12, the Godot project remains decoupled from FiveEGoldBox.Application and FiveEGoldBox.Core. The UI displays supplied state and emits UI command intent; it does not calculate gameplay legality or outcomes.