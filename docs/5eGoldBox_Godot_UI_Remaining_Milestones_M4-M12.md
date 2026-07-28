# 5eGoldBox Godot UI — Remaining Milestones (M4–M12)

**Status:** M0–M4 complete; M5 in progress (a, b done); seven milestones remain after M5.

This is a concise proposed execution-stage breakdown derived from the creator-approved Godot UI Pre-Integration Governing Plan v1.1. Lettered stages organize implementation; they do not change the governing milestone scope or acceptance gates.

## M4 — Input Router, Focus, and Interaction-State Framework

**Objective:** Make player input deterministic, context-aware, and consistent across the entire UI.

- [x] a. Define the complete player InputMap action inventory and migrate raw player-key handling. `ui/input/PlayerInputActions.cs` registers every fixed player/system/dev-shortcut binding as a named Godot InputMap action in code (compiler-checked `Key` values, not a hand-authored `project.godot` resource block). `ShellInputRouter`, `AppShell.Input.cs`, and `SelectionList.Input.cs` now check `keyEvent.IsActionPressed(...)` against those names instead of comparing raw `Key` values; `ShellInputRouter.Handle` takes the `InputEventKey` itself rather than a decomposed keycode/ctrl/alt triple. Godot's own built-ins (`ui_cancel`, `ui_up`, `ui_down`) are referenced by name rather than reimplemented. Verified: `dotnet build` clean (0 warnings/errors), grep confirms zero remaining raw `Key.*` comparisons in the touched files, all files still ≤250 lines.
  - **Deliberately deferred to c/d, not done here:** `ModalBackdrop`'s Tab-cycle and Escape-dismiss handling still compare raw `Key.Tab`/`Key.Escape` directly. Its focus-traversal and cancel-routing logic is getting restructured in M4c/M4d anyway, so converting only the detection mechanism now would be low-value churn ahead of that redesign.
  - **Deliberately left alone:** the per-command hotkeys in `CommandDefinition`/`HotkeyCommandButton` (e.g. `T` for Travel, `M` for Move) — these are content-driven, differ per active command set, and already ride Godot's own `Shortcut` mechanism correctly; InputMap actions are for fixed global bindings, not per-screen dynamic ones. Their unique-per-active-set validation is M4e's job. Dev-tool `Key.H` checks in `ui/dev/*Demo.cs` (help toggles in QA scaffolding, not player controls) were left as-is too.
- [x] b. Formalize interaction and focus contexts for commands, movement, lists, targeting, modals, and dialogs. `ShellInteractionContext` (renamed from the two-value `InteractionMode`) now names all five: `CommandMenu`, `ExplorationMovement`, `ListSelection`, `Targeting`, `Modal`. `ShellInteractionController` holds them on a real `Stack<ShellInteractionContext>` instead of a flat field reassigned from four call sites — entering exploration movement pushes, exiting pops back to whatever was underneath, and switching top-level screens (`ShowExploration`/`ShowRegionalMap`/`ShowCombat`) hard-resets the stack to `CommandMenu`. `IShellInteractionState.CurrentContext` (renamed from the `CurrentMode` name that collided in meaning with `ShellPresentationController.CurrentMode`, a different concept — which screen is showing, not what input context governs it) exposes the top of the stack read-only, which is all `ShellInputRouter` needs. Verified: build clean, grep confirms zero leftover `InteractionMode` references, all touched files ≤250 lines.
  - **Declared, not yet wired:** `SelectionList`, `Targeting`, `Confirmation`, and `ModalScreen` have no producer yet — nothing in the live `AppShell` tree opens a modal or shows a selection list today (those only exist in the M3 dev-component-gallery demos, which aren't part of the shell). Per this repo's own economy-of-code discipline ("generalize after evidence," "avoid a general effect engine" — see backend `CLAUDE.md`), no `PushContext`/`PopContext` public API was added for them ahead of a real caller; each gets pushed/popped by whichever milestone builds its first real modal (M6+) or list screen (M9) or target picker (M8), which is a small addition to the now-existing stack, not a redesign.
  - **Correction (found starting M5, applied retroactively):** the governing plan's §7.1 "Primary interaction" state machine — read in full for the first time while designing M5's view models — names these states `CommandMenu, DirectMovement, SelectionList, Targeting, Confirmation, ModalScreen` (six), not the five this entry originally shipped with (`ExplorationMovement`, `ListSelection`, a single collapsed `Modal`). Renamed `ExplorationMovement`→`DirectMovement` and `ListSelection`→`SelectionList` (legal next to the `SelectionList` *class* — always dot-qualified as `ShellInteractionContext.SelectionList`, no actual ambiguity), and split the single `Modal` value into `Confirmation`/`ModalScreen` to match. Method names (`EnterExplorationMovementMode` etc.) were left alone — they're descriptive, not the state-machine contract. Verified: build clean, headless boot exit 0, grep confirms the enum itself has zero remaining old names (only descriptive method/field names still say "ExplorationMovement", which is correct and unchanged). Lesson taken: read the full governing-plan docx before naming a new taxonomy, not just the milestone-summary markdown derived from it.
- [x] c. Centralize Escape/cancel priority and modal-stack routing. Two parts, matched to what's actually live today:
  - **`ModalBackdrop`'s Tab-cycle and Escape-dismiss** (the deferral flagged in M4a) now go through named actions instead of raw `Key.Tab`/`Key.Escape`: Tab-cycle checks `ui_focus_next`/`ui_focus_prev` with `exactMatch: true` (so a bare Tab doesn't also satisfy `ui_focus_prev`, and Shift+Tab doesn't also satisfy `ui_focus_next` — verified by hand-tracing Godot's modifier-matching rules, since there's no display here to click through it), keeping the existing `ShiftPressed` check for direction unchanged. Escape-dismiss checks the new `PlayerInputActions.UiCancel` constant. `ModalBackdrop` is the sole owner of modal cancel/focus-cycle handling — confirmed by grep that none of the dev demos (`ConfirmationDialogDemo`, `ModalBackdropDemo`, `ComponentGallery`) duplicate it.
  - **Priority itself was already correct, and is now documented rather than left implicit.** `ModalBackdrop._UnhandledKeyInput` unconditionally calls `SetInputAsHandled()` while visible, and Godot delivers unhandled input deepest-node-first — so a modal already wins over `ShellInputRouter`'s exploration-movement Escape handling by construction, not by luck. What was missing was making that fact legible: `ShellInteractionController` now carries a comment next to `PushContext`/`PopContext` spelling out the rule and the obligation it puts on future callers — anything that captures a cancel key this way (a future modal, target picker, or list) must also push/pop its `ShellInteractionContext` there, or `CurrentContext` goes stale while it holds input.
  - **No modal is wired into the live `AppShell` tree yet** (still only the M3 dev-gallery demos), so there's no real "modal open during exploration movement" scenario to exercise today — consistent with M4b, no dead `AttachModal`-style API was added ahead of a real caller. The first real modal (M6+) is what exercises this contract for the first time.
  - Verified: build clean, grep confirms zero remaining `Key.Tab`/`Key.Escape` literals in `ModalBackdrop.cs`, file stays at 238 lines (≤250).
- [x] d. Implement deterministic keyboard focus traversal and unmistakable focus presentation. One real gap found and closed; the rest was already built in M3 and is verified rather than redone:
  - **The gap:** `ShellCommandBarController.RenderCommands` created command buttons but never focused any of them, so after switching screens, exiting movement mode, or toggling immersive layout (F11), a keyboard-only player had no visible focus anywhere until they pressed Tab once, unprompted. It now grabs focus (`CallDeferred`, matching the pattern `ModalBackdrop.ShowModal` already established) on the first button of whichever bar just had `enableShortcuts: true` — the same boolean that already meant "this is the currently-visible layout's bar," so the hidden layout's bar never steals focus. `Refresh()` only actually runs on an explicit command-set change or an F11 toggle (traced through `ShellLayoutController` — nothing calls it on resize or on a timer), so this doesn't fight the player by repeatedly yanking focus back.
  - **Already correct, verified not rebuilt:** `ModalBackdrop.ShowModal`/`HideModal` already capture and restore previous focus and trap Tab-cycling within modal content (M3). `SelectionList` already grabs focus on `SelectIndex(..., grabFocus: true)` and exposes `FocusSelected()` (M3). Exploration movement mode deliberately has no focusable control in the command bar — it's a direct-keyboard mode, not button-driven, so there is nothing to focus, not a gap.
  - **"Unmistakable focus presentation" was already built in M3, not added here:** `ui/themes/GameUiTheme.tres` and `GameUiHighContrastTheme.tres` both define a `styles/focus` StyleBoxFlat, applied to `ShellCommandButton`, `ShellDangerousButton`, `ShellConfirmButton`, and `ShellSelectionRowButton` (grep-verified, 4/4 parity between the two themes). Visual sufficiency of that style was part of the M3 product-owner runtime acceptance ledger already — not re-litigated here without a display to check it against.
  - **Not attempted:** custom `FocusNeighbor`/explicit Tab-order wiring within a command bar row. The buttons are a single `HBoxContainer` row built at runtime; Godot's default spatial/sibling focus traversal already gives left-to-right order, and there's no multi-row or non-linear layout yet to make that ambiguous. Revisit if a future screen's layout makes the default order wrong, not before.
  - Verified: build clean, file stays at 129 lines (≤250).
- [x] e. Validate active-command hotkey uniqueness and prevent inactive contexts from consuming input. Two halves — one real new check, one audit of what M4b-d already guarantee:
  - **Hotkey uniqueness:** `ShellCommandBarController.ShowCommands` — the single choke point all three live command sets (`ShowExploration`/`ShowRegionalMap`/`ShowCombat` in `ShellInteractionController`) already funnel through — now throws `ArgumentException` if two `CommandDefinition`s in the same call share a `ShortcutKey`. Checked once per `ShowCommands` call, not per bar, since both the standard and immersive bar render from the same `_currentCommands`. This guards every future command set M6-M9 add, not just today's three (which were already accidentally unique — Exploration: M/V/C/A/E/S/L, RegionalMap: T/S/E/C/I/J, Combat: M/A/C/U/D/E — verified by enumeration, and confirmed by an actual headless run below).
  - **Context isolation was audited, not rebuilt — nothing currently races for the same keypress.** Walked every live input path: `ShellInputRouter`'s exploration-movement handling is gated to `CurrentContext == ExplorationMovement` (M4b); the command bar has zero buttons alive during movement mode (`ShowMovementPrompt` clears them, and Godot's `Shortcut` system can't fire on a freed node), so command hotkeys can't leak into movement mode without new code, not because of a check — there's nothing there to leak. `SelectionList`'s input only ever fires on a focused list item, and nothing currently puts a `SelectionList` in the live `AppShell` tree (still demo-only), so it can't race with shell-level contexts today. A modal, once one exists, already wins over everything via `SetInputAsHandled()` (M4c). Toggle-immersive and dev shortcuts are deliberately global/ungated — that's existing, unchanged M0-M3 behavior, not a gap. Nothing here needed new code; each holds by construction of what M4a-M4d already built.
  - **New this step: a real headless runtime check, not just a compile check.** `godot --headless --path . --quit-after 10` (a `godot` 4.7 mono binary is available in this environment) boots the actual project, runs `AppShell._Ready()` end to end — `PlayerInputActions.EnsureRegistered()`, all controller construction, `ShowExplorationView()` → `ShowCommands(7 commands)` through the new validator — and exits 0 with empty stderr. This is stronger evidence than the static enumeration alone and will be used for the rest of M4 going forward, alongside `dotnet build`.
  - Verified: build clean, headless run exit 0 with no errors, file stays at 149 lines (≤250).
- [x] f. Preserve direct MOVE behavior and add optional gamepad-navigation scaffolding.
  - **Gamepad scaffolding, D-pad only, deliberately.** `MoveForward`/`MoveBackward`/`TurnLeft`/`TurnRight` each gained an `InputEventJoypadButton` (`JoyButton.DpadUp/Down/Left/Right`) alongside their existing keyboard events — same action name, so `ShellInputRouter` needed no new branches. Analog-stick movement was deliberately left out: it needs deadzone tuning and continuous-vs-discrete handling, which is exactly the "final controller bindings" the milestone says not to require yet. `ToggleImmersiveMode` and the dev shortcuts stay keyboard-only — desktop/debug concepts, not navigation. `ExitMovementMode`'s gamepad case rides Godot's built-in `ui_cancel`, which already ships a default joypad binding — nothing to add there.
  - **This needed a real architecture change to actually reach the router, not just registering the bindings.** `AppShell` only listened on `_UnhandledKeyInput`, which Godot never calls for joypad events. Rather than fold everything into `_UnhandledInput` (which fires for the *entire tree* before *any* node's `_UnhandledKeyInput` does — that would have run `AppShell`'s own handling before `ModalBackdrop`'s Escape/Tab capture and inverted the M4c cancel-priority order), `AppShell` now has a second, additive `_UnhandledInput` override that explicitly ignores everything except `InputEventJoypadButton`, leaving the existing `_UnhandledKeyInput` path for keyboard completely untouched. `ShellInputRouter.Handle` was widened from `InputEventKey` to the base `InputEvent` — it only ever called `.IsActionPressed(...)`, which exists on the base type, so this was a pure signature widen with no internal logic change.
  - **Known gap, left open on purpose:** `ModalBackdrop` still only captures keyboard input (`_Input`/`_UnhandledKeyInput`), not `_UnhandledInput` — so a joypad button press would currently reach `ShellInputRouter` even while a modal is open, the same way keyboard-vs-modal did before M4c existed. This isn't a new problem so much as the pre-existing "no modal is wired into the live shell yet" caveat extending to a second input device — there is still nothing live to actually leak into. Whoever wires the first real modal (M6+) needs to extend `ModalBackdrop` to capture `_UnhandledInput` too, or joypad users will be able to interact with whatever's behind the modal. Recorded here so it isn't rediscovered as a surprise later.
  - **"Preserve direct MOVE behavior"** held throughout by construction — every M4a-e change to the movement path was a mechanism swap verified behavior-identical at each step (see those entries), and this step only *adds* a second event per action, never removes or reorders the keyboard ones.
  - Verified: `dotnet build` clean, headless run (`godot --headless --path . --quit-after 10`) exit 0 with empty stderr, touched files stay at 105/139/38 lines (all ≤250).
- [x] g. Complete keyboard/mouse parity, cancel-priority, context-isolation, and movement-mode verification. Final closeout pass — full re-verification, not just the last letter's diff:
  - **Keyboard/mouse parity:** `HotkeyCommandButton.Configure` wires `Pressed += _handler` unconditionally — mouse click always works — and only conditionally attaches a keyboard `Shortcut` when `enableShortcut` is true (i.e. only on the currently-visible layout's bar). Confirmed by re-reading the method, not just asserted.
  - **Escape/cancel priority:** re-confirmed as documented in c — `ModalBackdrop` wins by construction (deepest-node unhandled-input order + unconditional `SetInputAsHandled`), `ui_cancel` exits movement mode when no modal is capturing it first.
  - **Context isolation:** re-confirmed as audited in e — nothing live races for the same keypress today; `ShellInteractionContext` accurately reflects `CommandMenu`/`ExplorationMovement` at all times because both real producers push/pop it (b).
  - **Movement-mode suppression:** during `ExplorationMovement`, `ShowMovementPrompt()` clears every `HotkeyCommandButton`, so ordinary domain commands (Move/View/Cast/Area/Encamp/Search/Look, etc.) have no live node to fire from — not merely gated, structurally absent. Worth being explicit about one thing that could look like a violation but isn't: `ToggleImmersiveMode` (F11) and the dev shortcuts still work during movement mode, by design — those are shell-chrome/system-level, not "ordinary commands" in the sense this gate means, and suppressing F11 during movement would be a real usability regression, not a fix.
  - **Whole-milestone regression sweep, not just this step's diff:** grep across the full repo confirms zero remaining raw `Key.*` comparisons in any player-facing input file (`ShellInputRouter`, `AppShell.Input.cs`, `SelectionList.Input.cs`, `ModalBackdrop.cs`) and zero remaining references to the old `InteractionMode` name anywhere, in `.cs` or `.tscn`. All 52 UI C# scripts (51 at the M3 close, +1 for the new `PlayerInputActions.cs`) are still ≤250 lines — the M3 gate, re-run as part of this milestone's own close. `git diff --stat` across the whole M4 range touches zero `.tscn`/`.tres` files — M4 was pure C#, no scene/theme/visual risk introduced.
  - Verified: `dotnet build` clean, headless boot (`godot --headless --path . --quit-after 15`) exit 0 with empty stderr.

**M4 COMPLETE — READY FOR M5.** Every letter has a real, live producer today except the parts that explicitly can't (no modal/list/target-picker exists in the shell yet — M6/M8/M9's job); those are declared, documented, and contracted for rather than built ahead of evidence, consistently across a-g. No `.tscn`/`.tres` file was touched in this milestone. No product-owner runtime click-through was possible in this environment (no display); what stands in for it here is a real `godot --headless` boot plus the `dotnet build` gate, run after every letter, not only at the end.

## M5 — Mock Gateway and Scenario Laboratory

**Objective:** Provide deterministic UI data and command responses for every planned screen.

- [x] a. Finalize UI-owned immutable view models and the UiCommandIntent contract. Read the full governing plan docx for the first time (§7.2/§7.3 — via LibreOffice text conversion, `pandoc` isn't installed here) rather than inventing shapes, since "finalize" implied a design already existed. That read is also what surfaced M4b's naming gap, corrected in the commit just before this one.
  - **19 new files under `ui/models/viewmodels/` (plus `UiCommandIntent.cs`/`IUiCommandPayload.cs` at `ui/models/` root)**, one type per file matching this codebase's existing convention. All 11 models §7.2 names exist: `ShellViewModel`, `HeaderViewModel`, `PartyViewModel`/`PartyMemberViewModel`, `CommandSetViewModel`/`CommandViewModel`, `MessageLogViewModel`(+`MessageLogEntryViewModel`), `ExplorationViewModel`, `RegionalMapViewModel`(+`RegionalMapMarkerViewModel`/`RegionalMapPointViewModel`), `CombatViewModel`(+`CombatantMarkerViewModel`/`CombatHighlightViewModel`), `ModalViewModel`. Every one is a `sealed record`, matching the doc's "immutable records where practical." Grep-verified: zero `using Godot`/`Godot.` anywhere in the new files — genuinely engine-agnostic pure C#, not just declared as such.
  - **Reused existing types instead of duplicating them where they already matched the doc exactly:** `ShellViewModel.Mode` is the existing `PresentationMode` enum (already exactly `Exploration/RegionalMap/Combat`, the doc's own "Presentation" state machine); `CommandSetViewModel.InteractionMode` is the just-corrected `ShellInteractionContext` (already exactly the doc's "Primary interaction" state machine).
  - **`ShellCancelBehavior` is new, and deliberately traceable to source, not invented:** its seven values are the governing plan's §8.2 Escape/cancel-priority list verbatim, in priority order, plus `None` for contexts nothing applies to (today's `CommandMenu` with no modal/movement active — confirmed a real gap, not filler: M4g's audit found Escape is currently a no-op there).
  - **`UiCommandIntent.Payload` is `IUiCommandPayload?`, an empty marker interface, not `object?`.** The doc explicitly says "rather than a generic object or dictionary" — a marker interface lets each command family add its own small record later (e.g. a future `TextEntryPayload` for naming a save slot) without ever widening the contract itself. No concrete payload type exists yet because no command in the three live command sets (Exploration/RegionalMap/Combat) needs free-text or structured input — every one of today's 19 commands is a plain button-press. Consistent with the discipline held since M4b: no dead type ahead of a real need.
  - **`CommandViewModel.Label` is plain text, not BBCode**, unlike the existing Godot-coupled `CommandDefinition.FormattedLabel` (`"[b]M[/b]ove"`). Bolding the hotkey letter is a presentation concern for whatever future controller turns a `CommandViewModel` into an actual scene node — a supposedly engine-agnostic model shouldn't carry Godot-flavored markup.
  - **Deliberately loose where the doc's own "representative content" wording is loose, not tightened without evidence:** `CombatHighlightViewModel.Kind` and `MessageLogEntryViewModel.Category` are plain strings, not enums — tactical combat's real highlight vocabulary and message categories are M8/M6 territory to discover from real screens, not to guess at three milestones early.
  - **Scope boundary held on purpose:** this is the 11 models + the intent contract only — §7.2/§7.3, the parts of the doc explicitly finalized. `UiSnapshot`/`IGameUiGateway` (§15.2) were **not** built; the doc itself says their exact method shape "is not authorized by this document; it must be finalized only after the UI flows and Application public surface are both stable." M5b needs *something* snapshot-shaped for the mock gateway, but it'll be built and labeled as pre-integration/provisional there, not as the doc's reserved-for-later integration port.
  - **Nothing existing was touched or rewired.** `AppShell`, `ShellInteractionController`, `CommandDefinition`, and the three live command sets are completely untouched — these are new, additive types with zero callers yet. Retrofitting the live shell onto them is explicitly M6's job (its own plan entry: "Drive the exploration command bar from data rather than fixed scene layout"), not M5's.
  - Verified: build clean, headless boot exit 0, grep confirms zero Godot references in any new file, 158 total lines across 19 files.
- [x] b. Implement the UI-local mock gateway for snapshots and command intents. `ui/mocks/` (the folder name §6.2's directory sketch itself specifies): `MockUiGateway` holds a `ShellViewModel` snapshot, accepts `UiCommandIntent` submissions dispatched by `CommandId` to registered handlers, and fires `SnapshotChanged` when a handler returns an updated snapshot.
  - **Named `Mock*` throughout on purpose, not `IGameUiGateway`/`UiSnapshot`/`UiCommandResponse`.** §15.2 explicitly reserves those exact names for the real integration port and says its method shape "is not authorized by this document." `MockCommandResult`/`MockCommandOutcome` reuse §15.2's four outcome categories (`Accepted`/`RejectedForCurrentState`/`Busy`/`Error`) verbatim — those are what a submission can result in, not a gateway-shape decision, so borrowing them isn't the same as preempting the reserved names.
  - **Deliberately left out, and why:** no revision/sequence counter on the snapshot (§15.2 mentions one, "used only to reject stale UI responses") — the mock gateway is synchronous and single-threaded, so nothing can race a snapshot today; a real candidate for M5f, when latency simulation introduces an actual async gap a stale submit could land in. No wiring into the live `AppShell` — same boundary as M5a, M6's job.
  - **Real behavioral verification, not just a build check** — this is logic, not inert records like M5a. Copied the pure-C# files (gateway + the view models it depends on, zero Godot references in any of them) into an isolated scratch console project outside the repo and ran 12 checks: rejection when no handler is registered, accepted submit both returning `Accepted` and updating `CurrentSnapshot`, `SnapshotChanged` firing exactly once per accepted submit and zero times for a rejection or a `Busy` result, nested view-model data (a party member's `Selected` flag) surviving the round-trip, re-registering a handler correctly overwriting the previous one, and `ReplaceSnapshot` updating state and firing the event directly. All 12 passed.
  - Verified: build clean, headless boot exit 0, zero Godot references in `ui/mocks/`, 102 lines across 3 files, 12/12 scratch behavioral checks passed.
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