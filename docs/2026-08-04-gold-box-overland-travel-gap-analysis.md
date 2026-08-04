# Gold Box overland travel design reference — gap analysis

**Date:** 2026-08-04. Source document: `gold-box-style-overland-travel-system-design-reference.docx` (user-supplied, "Designing a Gold Box-Style Overland Travel Screen and Engine," v1.0, August 2026) — a 28-section behavioral spec plus appendices, synthesizing Pool of Radiance/Gateway to the Savage Frontier/Treasures of the Savage Frontier/FRUA manuals into a C#/Godot architecture blueprint.

**Method:** every claim below about the *current* engine is sourced to an actual file:line, read directly (`RegionalTravelRules.cs`, `RegionalTravelState.cs`, `TravelRouteDefinition.cs`, `WatchtowerRegionalRoute.cs`, `TravelSessionValidator.cs`, `SessionView.cs`, all three scenarios' `Routes` data, `RegionalMapViewModel.cs`), not assumed from memory or naming. Every claim about the design doc is the doc's own section/FR number.

## Verdict up front — this is a much thinner system than combat was

The combat gap analysis (`docs/2026-08-04-gold-box-combat-design-gap-analysis.md`) found a solid, tested foundation with real-but-nameable gaps layered on top. Regional travel is different in kind: **almost none of the doc's "six jobs" of an overland layer (§1) exist yet.** What's here is a genuinely minimal placeholder — a named route between two locations, walked in a fixed number of clicks, with no time, no risk, and no state beyond "which step am I on." It works, it's honestly scoped (not broken), and it's fully tested for what it does (38 cases in `RegionalTravelRulesTests.cs`) — but it is not yet an overland travel *system* by this doc's definition. It's the "destination reached" half of one command.

Concretely: **regional travel today can never produce an encounter, never advances any clock, and never differs in outcome between two playthroughs.** Every actual danger and every scripted event in this engine happens after arrival, during exploration (a location's own `Triggers`) — never during the journey itself. The doc's central claim (§1.1) that "movement advances meaningful world time" and "travel may be interrupted by encounters, discoveries, hazards, arrivals, deadlines, or scripted choices" describes none of what `RegionalTravelRules.Advance` currently does.

## Part 1 — Already correct, validate and move on

| Doc topic | Doc's guidance | Current state |
|---|---|---|
| §17 domain architecture / FR-001/002/012/014 | Pure C# domain, deterministic, headless-testable, serializable at command boundaries | `RegionalTravelRules`/`RegionalTravelState` have zero Godot dependency, no randomness anywhere in the `Travel` folder (confirmed by direct search), and `SaveRegionalTravelV1` already serializes travel state cleanly. Fully in the doc's "pure C#, Godot only renders" mold — the one thing this thin system gets unambiguously right |
| §11.4 hybrid grid-and-route model (the route-edge half) | "A route edge can contain duration, fare, schedule, prerequisites, intermediate event checks, and arrival target" | `TravelRouteDefinition` is exactly a bare route-edge — `RouteId`/`Origin`/`Destination`/`FinalStepIndex`/`RequiredProgressIds` — the doc's own recommended shape, just missing every field past step count (see Tier 1 below). The engine correctly never modeled a Pool-style cell grid — it went straight to the Gateway/Treasures "location-to-location" school (§2.2, §14 table), which the doc itself frames as the more legible modern default |
| §5.5 route choice, player's to make | "A location with more than one road out... offers more than one, and it is the party's choice which to take, not the engine's" | Already real, not hypothetical: Hollow Mill authors two routes from the same origin (`route.village-to-mill`, 3 steps; `route.village-to-mill.shortcut`, 2 steps), and `RegionalTravelRules.ResolveRoute` requires a `routeId` the moment more than one is open rather than guessing |
| §13.1 conditional world presentation (routes half) | "Locations and routes can appear, disappear, open, close... according to campaign flags" | Every route is gated by `RequiredProgressIds`, checked not just at departure but continuously — `TravelSessionValidator`/`IsActiveRouteOpen` re-check the *specific active route* on every `Advance`, so a route that closes mid-journey is a real, already-handled case, not a gap |
| §18.4 atomic segment resolution | "The application layer may repeatedly call AdvanceOneSegment... revalidate after each" | `RegionalTravelRules.Advance` is exactly one atomic step (`CurrentStepIndex + 1`) per call, gated by `CanAdvance`/`GetAdvanceAvailability` re-evaluating mode, route-open state, and completion every time — matches FR-004 precisely, just with nothing yet for that revalidation to actually catch besides "route closed" |
| §17.4 commands-as-intent shape (loosely) | Discrete command → new state, not ad hoc mutation | `BeginJourney`/`Advance` both take a session and return a new one (`RegionalTravelAdvanceResult`), consistent with how every other subsystem in this engine already works (`ExplorationMoveResult`, `CombatResolutionResult`) — no formal `TravelCommand`/`TravelEvent` type hierarchy exists, but the operational shape the doc actually cares about is already the house style |

## Part 2 — Real gaps, tiered by size and value

### Tier 0 — foundational, and bigger than "travel": no campaign clock exists anywhere in this engine

**Confirmed by direct search: there is no `CampaignTime`, no clock, no day/night, no calendar concept anywhere in `Core` or `Application` — not stubbed, not travel-scoped, just absent.** This is the doc's single most load-bearing requirement (FR-003, §6.1: *"All views must use the same time service... separate 'town time' and 'wilderness time' counters create impossible bugs in deadlines, spell durations, shop schedules, and healing"*). It isn't only a travel gap — spell durations, rest, and any future shop-hours/deadline content all sit downstream of it too. Almost every other item below either needs this first or is meaningfully weaker without it (a "3-step journey" can't become "took two days" without a clock to advance). Scoping this is the actual prerequisite decision, the same way stepwise movement was the prerequisite for combat's reactions gap.

### Tier 1 — real, self-contained gaps in travel itself, buildable once (or even partly before) a clock exists

**1. Travel can never be interrupted — no encounters, no discoveries, no events of any kind.** `Advance` does exactly one thing: increment a counter and, on arrival, change the current location. No random check, no scripted trigger, no RNG draw (confirmed: zero `Random`/seed usage in the whole `Travel` folder). This is the doc's most identifiably "Gold Box" feature (§9, FR-006, FR-007, FR-010) and the biggest single gap for *feel* — right now the overland map is a progress bar with scenery, not a place anything can happen. Building this doesn't require the full clock: even a simple step-counted encounter check (§9.2's "step event" model — fire after N steps) could land against the current step-based `Advance` before a clock exists, with the richer hazard-clock/zone-scoped version (§9.2's `TravelClockState`) following once zones do.

**2. No interruption/stop-condition policy exists — every `Advance` behaves identically.** There's no distinction anywhere between "silently continue" and "something happened, stop and tell the player" (§5.5's table, FR-007) because nothing currently *can* happen mid-route to distinguish. Falls out for free once #1 exists, but is worth naming separately since the doc treats "never skip a mandatory event" as its own invariant (§27.5), not an afterthought of encounter-rolling.

**3. Route cost is step-count only — no terrain, weather, pace, or party-state effect on duration.** `TravelRouteDefinition.FinalStepIndex` is a flat author-picked integer; nothing about difficult terrain, forced march, or travel mode can lengthen or shorten a journey (§5, FR-005). This is real but genuinely secondary until there's a clock for "duration" to mean something more than "number of button presses."

### Tier 2 — real features, larger, content-gated (matches this project's own stated "mechanics only where content demands" rule)

Consistent with how the combat analysis treated AoE templates/persistent hazards/reinforcement waves — these are correctly-sized doc features that shouldn't be built ahead of content that needs them:

- **Travel modes** (§11, FR-009) — boats, mounts, passenger routes. Zero infrastructure; every route today is implicitly "walking." Only worth building once a scenario actually wants a boat/mount to matter (Gateway's own signature feature — the doc calls out that abandoning a river cell without the boat should be a real, prompted event, §11.1).
- **Location visibility states** (§8.2, FR-008) — Unknown/Rumored/Revealed/Visited/Closed/Destroyed/Expired. Today a location is simply reachable-or-not via scenario progress; there's no partial-knowledge or discovery model at all. The current progress-gating is a real, working instance of the doc's broader "conditional world presentation" idea (§13.1) — just one state (open/closed) out of the doc's seven.
- **Zones** (§7, FR-010) — regional behavioral regions selecting encounter tables, combat backdrop, and messages. Doesn't exist for travel at all; combat's own art (`CombatFloorTileCatalog`) is keyed off the *encounter's monster roster*, not any travel concept, since travel-triggered combat doesn't currently exist (see Tier 0/1).
- **Rest/Encamp during travel** (§10) — no mechanism. Worth noting this isn't travel-specific: `Encamp` doesn't exist for real sessions anywhere in this engine at all today (mock-content-only, per CLAUDE.md's own prior notes), so this is really "build rest," not "wire rest into travel."
- **Deadlines and scheduled world events** (§13.2, §6.5) — no mechanism; downstream of Tier 0.

### Explicitly out of scope for now — matches the doc's own framing or an already-implicit project choice

- **A Pool-style logical grid with binary cell passability** (§4). The doc itself frames FRUA's 38×15 grid as a historical editor constraint, not a timeless recommendation, and explicitly endorses the route-graph model this engine already uses instead (§11.4, §14 table). Nothing to fix here — this is a correct existing choice, not a missing feature.
- **Simulation/balancing tooling, editor overlays, validators** (§26). Real doc recommendations, but tooling for a system that doesn't have encounters or a clock yet has nothing to validate. Revisit once Tier 0/1 exist.
- **5e-specific travel rules — pace, foraging, navigation checks, marching-order roles** (§25). All genuinely 5e-flavored and all explicitly said by the doc itself to come *after* the core transaction order is stable (§28, step 12 of 14). Correctly not started.

## Recommended next step

This isn't a menu of independent nice-to-haves the way combat's tier 2/3 list was — **Tier 0 (a campaign clock) is a real prerequisite decision that shapes almost everything else**, the same role stepwise movement played for combat's reactions gap. Two honest starting points:

1. **Scope the campaign clock first** (Tier 0) — smaller in code than it sounds (the doc's own `CampaignTime`/ordered-resolution model, §6.3, §17.3, is modest), and it's the one piece nothing else can be built correctly without.
2. **Or ship Tier 1, item #1 (step-counted encounter checks) against the clock-less system as it stands today**, accepting that "duration" stays "number of clicks" a while longer — gets travel actually interruptible sooner, at the cost of redoing the trigger mechanism once a real clock exists.

Not yet decided which to start with — flag for the user, the same way the combat analysis left "initiative strip vs. reactions rework" open rather than picking for them.
