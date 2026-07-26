<!-- Converted from 'Current Game Development Goals.txt'. The .txt remains
     the authored original; this is the tracked, greppable copy.

     NOTE: this document has its own Phase 1-13 numbering, which is a PRODUCT
     roadmap and is NOT the same sequence as the Phase 1-12 engineering
     numbering in priority-1/priority-2-development-plan.md. -->

# Long Term

### Long-term development course: from current engine to a finished game

The central strategic change should be this:

From this point forward, development should be driven by complete playable scenarios, not by implementing 5e rules in isolation.

The existing rules and encounter code remain the foundation. We should not discard it or weaken its standards. But every substantial new rules feature should increasingly be justified by a concrete need in the playable game.

The overall course should move through the following major phases.

---

## Phase 1 — Finish the current combat foundation and establish a stopping point

We are close to a sensible temporary stopping point for isolated tactical-combat work.

The immediate goal is not to complete 5e combat. It is to make the existing combat system complete enough to support the first end-to-end playable scenario.

That means finishing only the most obvious exposed seams, such as:

- applying cover to Dexterity saving throws;
- ensuring basic encounter creation and completion are stable;
- preserving combatant state before and after combat;
- ensuring legal actions can be discovered by a client;
- ensuring the encounter runtime exposes enough information for a UI to render and explain outcomes.

After that, broad combat expansion should pause.

We should not immediately continue into every reaction, spell, condition, class feature, monster ability, environmental hazard, and optional rule. Those systems should be added later when the campaign or vertical slice requires them.

### Long-term purpose of this phase

Create a stable tactical-combat kernel that is:

- deterministic;
- independently testable;
- UI-agnostic;
- capable of supporting a simple battle;
- capable of returning persistent consequences to the campaign.

### Exit condition

A predefined encounter can begin, accept legal player actions, resolve turns, determine victory or defeat, and return surviving character state without relying on a graphical client.

---

## Phase 2 — Build the campaign and application backbone

This is the most important architectural phase.

The project currently contains rules and encounter state, but it does not yet contain the object that represents the complete game being played.

We need an application-level model that coordinates:

- the active party;
- current location;
- campaign time;
- current game mode;
- persistent character state;
- exploration state;
- regional travel state;
- active encounter state;
- world and quest flags;
- save-game state.

Conceptually, this will resemble:

CampaignSession

├── PartyState

├── CampaignClock

├── CurrentMode

├── CurrentLocation

├── WorldState

├── ExplorationState

├── TravelState

└── EncounterState

The exact names should emerge from the code rather than being imposed prematurely.

This layer should own transitions such as:

### Regional travel

→ local exploration

→ tactical encounter

→ encounter resolution

→ local exploration

→ regional travel

It should not contain rendering, terminal formatting, Godot nodes, or input handling.

### Architectural direction

A likely project boundary is:

FiveEGoldBox.Core

### Mechanical rules and domain logic

FiveEGoldBox.Application

### Campaign session, commands, transitions, persistence orchestration

We should avoid creating numerous projects merely for organizational symmetry. New assemblies should appear only when a real dependency boundary becomes useful.

### Exit condition

A campaign session can move between its principal modes while preserving party state, time, resources, and encounter consequences.

---

## Phase 3 — Build a minimal text-only reference client

The first executable version should be a deliberately plain console client.

Its purpose is not to become a polished terminal RPG. It is to prove that the engine can operate as a complete game without relying on presentation-layer logic.

The text client should be able to:

- create or load a campaign;
- show the current mode and state;
- list legal actions;
- accept simple numbered commands;
- send commands to the application layer;
- display returned results;
- save and reload the session.

The first complete loop should be extremely small:

### Start campaign

→ choose one destination

→ enter one small exploration map

→ move and turn

→ trigger one predefined encounter

→ resolve the encounter

→ return to exploration

→ save and reload

This is the first true product milestone.

It proves that:

- the rules engine can support a game;
- state persists between modes;
- encounter results affect campaign state;
- clients can discover and issue legal commands;
- saving and loading preserve deterministic behavior;
- adventure content can create actual gameplay.

### Critical limitation

The console client must remain thin.

It should not contain:

- attack calculations;
- legal-movement logic;
- quest logic;
- encounter construction rules;
- map-event behavior;
- character-state mutation;
- console-specific game mechanics.

Otherwise, we would create a second implementation that Godot could not reuse.

### Exit condition

The entire miniature game loop can be completed through the console, saved, closed, reloaded, and completed again consistently.

---

## Phase 4 — Reproduce the same loop in Godot

Godot should begin shortly after the text walking skeleton works—not after the entire game has been implemented in text.

The first Godot client should be intentionally crude. Its job is to prove the client boundary and identify integration problems early.

Initially, it should provide:

- a campaign start/load screen;
- a simple regional map;
- a basic first-person exploration view;
- a tactical grid;
- selectable legal actions;
- state and result displays;
- basic transitions between modes.

Placeholder graphics, text labels, colored rectangles, and temporary assets are acceptable.

The Godot layer should:

- render authoritative state;
- collect player intent;
- send commands;
- animate results;
- control presentation flow.

It should not decide:

- whether an action is legal;
- whether an attack hits;
- whether movement is permitted;
- whether a quest condition is satisfied;
- how encounter consequences alter the party.

### Why this phase comes early

Some architectural problems will not appear in the console:

- asynchronous animations;
- selection and cancellation;
- movement previews;
- camera state;
- scene transitions;
- UI synchronization;
- displaying legal targets;
- replaying command results;
- distinguishing engine state from temporary presentation state.

Finding these issues early will prevent the application layer from becoming accidentally console-centric.

### Exit condition

The same miniature loop that works in the console also works through Godot using the same application commands and state models.

---

## Phase 5 — Establish the three complete game modes

Once the walking skeleton works in both clients, each principal game mode should be developed into a minimally complete system.

### Regional travel

The regional layer should eventually support:

- named locations;
- routes or traversable terrain;
- travel time;
- party speed;
- destination discovery;
- interruptions;
- scripted and random encounters;
- weather or terrain effects where useful;
- transitions into local maps;
- boats, mounts, or special travel only if required by the campaign.

The underlying model will probably be a graph, tile map, hex map, or a limited combination of these. It should not be forced into the tactical battlefield abstraction.

### First-person local exploration

The exploration system should use a logical map model based on cells and facing.

It should eventually support:

- party position and direction;
- passable and impassable edges;
- doors and door states;
- stairs and transitions;
- discovered areas;
- interactive objects;
- encounter triggers;
- traps and secrets;
- light and visibility;
- local time passage;
- NPCs and merchants;
- contextual actions.

The rendered presentation may mimic the split-screen Gold Box style, but the simulation should remain independent of rendering.

### Tactical combat

Combat should develop from the existing kernel into a complete game system, including:

- movement and pathfinding;
- action economy;
- melee and ranged attacks;
- spell targeting;
- conditions;
- reactions;
- enemy AI;
- areas of effect;
- persistent battlefield effects;
- encounter objectives;
- victory, defeat, retreat, surrender, or morale where appropriate;
- post-combat rewards and consequences.

### Exit condition

Each mode is individually usable and all transitions among them preserve authoritative campaign state.

---

## Phase 6 — Build the content model around one real playable chapter

The next major objective should be a small but genuine chapter of the game—not a collection of disconnected demonstrations.

This chapter should contain:

- a safe settlement or hub;
- several regional destinations;
- one or two exploration locations;
- NPCs;
- shops or services;
- quests;
- dialogue;
- scripted events;
- multiple tactical encounters;
- treasure and rewards;
- resting;
- advancement;
- meaningful choices and consequences;
- a clear chapter conclusion.

This phase will force us to discover what the content system actually needs.

Adventure content should not be represented by hundreds of unique hard-coded C# classes. We will need structured definitions for concepts such as:

- maps;
- locations;
- encounters;
- NPCs;
- dialogue;
- shops;
- quest stages;
- triggers;
- conditional outcomes;
- rewards;
- transitions.

However, we should not attempt to invent a universal game scripting language.

The correct approach is:

1. implement explicit event and content types needed by the first chapter;

2. observe recurring patterns;

3. generalize only those patterns;

4. add validation and tooling as content volume grows.

### Exit condition

A player can begin a campaign, complete a coherent chapter, make meaningful decisions, fight several encounters, use services, save and load, and reach a definite conclusion.

---

## Phase 7 — Expand mechanics only in service of content

After the first chapter establishes the complete product structure, rules development can resume more broadly—but it should be demand-driven.

For example:

- Do not implement every spell because it exists.
- Implement the spells available to the playable characters and enemies in the next content milestone.
- Do not implement every condition at once.
- Implement the conditions required by current spells, monsters, traps, and environments.
- Do not build every class and subclass before the game is playable.
- Build the classes and progression paths included in the initial campaign.

Likely areas of expansion include:

- spellcasting;
- concentration;
- conditions;
- reactions and opportunity attacks;
- Extra Attack and Multiattack;
- class resources;
- rests;
- leveling;
- inventory use;
- consumables;
- equipment changes;
- monster abilities;
- environmental effects;
- skill checks;
- dialogue checks;
- exploration abilities;
- travel abilities.

### Governing principle

The game should aim for:

A faithful and internally consistent subset of 5e sufficient for the campaign.

It should not initially aim for:

A universal implementation of every official 5e rule, class, spell, monster, and edge case.

Broad rules coverage can become a later engine goal after the first complete game exists.

### Exit condition

All player-facing mechanics required by the planned campaign are implemented, integrated, tested, and exposed through both the application layer and Godot client.

---

## Phase 8 — Develop enemy AI and encounter design as product systems

Enemy AI should not be treated as a single late feature.

It will need several layers:

- legal-action discovery;
- target evaluation;
- movement planning;
- threat and risk evaluation;
- ability selection;
- tactical roles;
- retreat or surrender;
- encounter-specific behavior;
- difficulty controls.

The deterministic runtime is well suited to this because AI can evaluate the same legal commands available to players.

The first AI should be simple and explainable:

- attack reachable targets;
- prefer vulnerable targets;
- move toward useful positions;
- avoid obviously illegal or pointless actions;
- use a small set of explicit priorities.

Later AI can add:

- archetypes;
- coordinated behavior;
- spell selection;
- area targeting;
- defensive decisions;
- morale;
- scripted boss behavior.

Encounter design will also require supporting tools and validation. A technically correct combat system is not sufficient if encounters are slow, repetitive, or unreadable.

### Exit condition

Enemies can reliably complete encounters without manual intervention, display recognizable tactical behavior, and support varied encounter design.

---

## Phase 9 — Establish a sustainable content-production pipeline

Once the engine and first chapter are stable, the project will shift increasingly from foundational engineering to content production.

At this point, the bottleneck will become:

- authoring maps;
- writing dialogue;
- configuring encounters;
- placing NPCs;
- defining quests;
- balancing rewards;
- producing art;
- testing progression;
- revising pacing.

We will likely need lightweight internal tools for:

- map editing;
- encounter configuration;
- content validation;
- dialogue and event authoring;
- quest-state inspection;
- save-game inspection;
- combat simulation;
- content dependency checking.

These tools should be created only after the underlying content formats have proven themselves through real use.

A major long-term goal should be to make content creation cheaper than engine modification.

### Exit condition

New locations, encounters, NPCs, and quests can usually be added through validated content definitions rather than changes to core engine code.

---

## Phase 10 — Define and enforce the version-one scope

Before full production begins, version one must have a firm product contract.

That contract should define:

- campaign length;
- number of major regions;
- number of towns and dungeons;
- supported character levels;
- playable classes and subclasses;
- spell selection;
- enemy roster;
- party size;
- exploration mechanics;
- travel complexity;
- supported platforms;
- art and audio expectations;
- accessibility requirements;
- save-game behavior;
- modding or extensibility expectations, if any.

Without this boundary, the game could expand indefinitely under the banner of “supporting 5e.”

For a realistic first release, I recommend:

- one complete campaign;
- a bounded level range;
- a carefully selected subset of classes and spells;
- enough enemy and encounter variety to sustain the campaign;
- robust versions of the three main game modes;
- a reusable architecture that permits future campaigns and expanded rules.

The reusable engine should emerge from successfully supporting the game. It should not delay the game while attempting to anticipate every future campaign.

### Exit condition

Every planned version-one feature is explicitly classified as required, optional, deferred, or rejected.

---

## Phase 11 — Alpha: complete game, incomplete polish

The alpha milestone should mean the entire campaign is playable from beginning to end.

At alpha:

- all major content exists;
- all game modes work;
- all required player mechanics exist;
- enemy AI is functional;
- save/load is reliable;
- campaign progression is complete;
- no placeholder system blocks completion.

It may still contain:

- placeholder art;
- rough animations;
- balance problems;
- weak tutorials;
- awkward UI;
- incomplete sound;
- performance issues;
- minor missing secondary content.

Alpha should be about completeness and correctness, not presentation.

### Exit condition

A fresh player can complete the entire campaign without developer intervention.

---

## Phase 12 — Beta: stabilize, balance, and improve usability

Beta should primarily reduce risk rather than add scope.

The focus should be:

- bug fixing;
- balancing;
- performance;
- UI clarity;
- input consistency;
- save compatibility;
- tutorials and onboarding;
- encounter pacing;
- campaign pacing;
- accessibility;
- controller and keyboard support;
- resolution and scaling;
- platform testing;
- content proofreading;
- exploit prevention;
- failure recovery.

Large systems should not be introduced during beta unless the game cannot ship without them.

Automated tests will remain valuable, but beta will require extensive human playtesting because many failures will concern comprehension, pacing, frustration, and presentation rather than deterministic correctness.

### Exit condition

The game is feature-complete, content-complete, stable, understandable, and suitable for external release testing.

---

## Phase 13 — Release candidate and launch

The release-candidate phase should involve:

- final regression testing;
- packaging;
- platform-specific validation;
- clean-install testing;
- save migration testing;
- credits and licensing review;
- crash handling;
- logging and diagnostic support;
- storefront materials;
- documentation;
- launch build verification.

At this stage, changes should be conservative.

The release should represent a complete, bounded campaign—not merely an engine showcase.

### Exit condition

The selected release build passes all technical, gameplay, content, licensing, and packaging checks and is accepted as version one.

---

### Long-term architectural principles

Several principles should govern the entire course.

1. One authoritative simulation

The console client and Godot client must operate on the same application and rules code.

There should never be separate console and graphical implementations of combat, exploration, travel, or campaign logic.

2. Commands in, results out

Clients should express player intent through commands and receive explicit results.

For example:

MoveForwardCommand

→ ExplorationMovementResult

UseWeaponAttackCommand

→ EncounterWeaponAttackResult

This makes behavior testable, replayable, inspectable, and suitable for AI.

3. State must survive mode transitions

Characters should not be recreated independently for combat.

The same persistent party should carry:

- hit points;
- resources;
- conditions;
- inventory;
- ammunition;
- spell use;
- time-dependent effects;
- experience;
- death or injury;

through travel, exploration, combat, rest, and saving.

4. Content should consume systems

Rules exist to support content.

When a dungeon requires poison, we implement poison. When a wizard enemy requires concentration, we implement concentration. When regional travel requires boats, we add boats.

This keeps implementation aligned with actual gameplay value.

5. Generalize after evidence

We should prefer a clear implementation for one real use case over an elaborate abstraction designed for hypothetical future content.

After two or three use cases reveal a stable pattern, then we generalize.

6. Keep vertical slices continuously playable

After the first walking skeleton, main should increasingly remain a runnable game rather than merely a passing library.

Each major development cycle should leave behind a more complete playable experience.

7. Preserve the existing engineering discipline

The move toward product breadth should not mean abandoning:

- small branches;
- clean builds;
- passing tests;
- explicit domain types;
- deterministic behavior;
- reviewable commits;
- separation of rules and UI.

The discipline is an asset. The priority of what we build with it is what must change.

---

### Overall destination

The intended finished architecture should approximately support this flow:

### Launch game

→ create or load party

→ regional travel

→ enter towns, wilderness sites, and dungeons

→ explore in first person

→ interact with NPCs, objects, shops, and events

→ transition into tactical combat

→ resolve persistent consequences

→ advance quests and campaign time

→ return to exploration or travel

→ save and continue

→ complete the campaign

The recent external analysis correctly identifies that this complete loop—not comprehensive rule coverage—is the spine of the product.

The long-term course is therefore:

Finish the minimum combat foundation, create the campaign backbone, prove the whole loop in text, reproduce it in Godot, build one real chapter, expand mechanics in response to content, scale the content pipeline, and then complete and polish a deliberately bounded campaign.

### Medium Term

### Medium-term development progression

For planning purposes, the medium term should begin with the current repository and end when the project has a small but genuine playable game segment running in Godot.

The medium-term goal is not a finished campaign, complete 5e coverage, polished artwork, or production-ready content tools. It is to transform the project from a rules-and-combat library into a functioning RPG architecture with proven transitions among:

### Regional travel

→ first-person exploration

→ interaction

→ tactical combat

→ persistent consequences

→ exploration or travel

That complete loop is the key product milestone identified by the earlier analysis.

---

### Medium-term Phase 1 — Close the current combat milestone

We should finish the small tactical-combat seam already exposed by the cover feature: applying cover bonuses to relevant Dexterity saving throws.

After that, isolated combat expansion should pause.

Before leaving the combat runtime temporarily, we should confirm that it can support the first playable scenario:

- an encounter can be created from external campaign data;
- participants can enter with persistent character state;
- legal player actions can be discovered;
- simple enemy turns can be executed;
- victory and defeat can be determined;
- surviving state can be returned to the campaign;
- spent ammunition, hit points, death, and similar consequences persist.

This does not require completing reactions, every condition, a comprehensive spell system, elevation, flying, grappling, or every action available in 5e.

### Result of this phase

The combat runtime becomes a stable subsystem that is sufficient for one simple tactical encounter. It is not considered “complete,” but it is complete enough to integrate.

---

### Medium-term Phase 2 — Introduce the application and campaign layer

The next major addition should be the layer that represents an actual game in progress.

A likely project boundary would be:

src/FiveEGoldBox.Application

The exact name can be decided when we begin, but its responsibility should be clear: coordinate the rules engine and the complete campaign session without depending on any user interface.

The initial application layer should introduce only the essential concepts:

CampaignSession

PartyState

CampaignClock

CurrentGameMode

CurrentLocation

ExplorationState

TravelState

ActiveEncounter

WorldFlags

This layer should control transitions between modes.

For example:

### Travel command

→ arrival at destination

→ exploration map loaded

→ map trigger activates encounter

→ encounter state created

→ encounter completed

→ consequences applied to party

→ exploration resumes

The application layer should also establish a common command/result pattern for clients.

Examples might conceptually include:

TravelToLocationCommand

MoveExplorationPartyCommand

TurnExplorationPartyCommand

InteractCommand

BeginEncounterCommand

PerformEncounterActionCommand

SaveCampaignCommand

The console and Godot clients should issue these commands rather than directly mutating state.

### Important restraint

We should not attempt to model the entire future campaign at this stage.

The first version does not need:

- a comprehensive quest engine;
- factions;
- reputation;
- calendar schedules;
- world simulation;
- procedural encounters;
- dynamic economies;
- universal dialogue scripting.

It needs enough campaign state to support one real loop.

### Result of this phase

The repository stops being a collection of independent calculators. It gains one authoritative model that owns the party, current mode, location, time, and transitions.

---

Medium-term Phase 3 — Build minimal regional travel and exploration models

Once the campaign layer exists, we should implement deliberately narrow versions of the two missing game modes.

### Regional travel

The first regional system should be a small location-and-route graph rather than a complete open-world simulation.

It should support:

- a few named locations;
- connections between those locations;
- travel duration;
- destination availability;
- arrival events;
- transitions into local exploration;
- one predefined travel encounter or interruption.

It should not initially include:

- weather simulation;
- navigation failure;
- supplies and starvation;
- mounts;
- boats;
- terrain-specific travel rules;
- dynamic random encounter tables;
- day/night encounter variations.

Those can be introduced later when content requires them.

### First-person exploration

The first exploration system should use logical cells and facing directions.

It should support:

- current map;
- current cell;
- current facing;
- turning left and right;
- forward movement;
- passable and impassable boundaries;
- one door;
- one interactive object or NPC;
- one encounter trigger;
- leaving the map.

It should have its own state model rather than reusing the tactical battlefield model.

Shared primitives may be reasonable, but the semantics are different:

Tactical position:

### Who occupies this square and what combat actions are legal?

Exploration cell:

What can the party see, cross, discover, or activate from this location and facing?

### Result of this phase

A campaign can travel to a destination, enter a local map, move through it, and activate a predefined event.

---

### Medium-term Phase 4 — Build the text-only reference client

After the campaign, travel, and exploration foundations exist, we should create a minimal executable console project.

A likely project would be:

src/FiveEGoldBox.Console

Its interface should be intentionally plain:

### Mode: Exploration

### Location: Ruined Keep

### Position: 3, 5

### Facing: North

1. Move forward

2. Turn left

3. Turn right

4. Interact

5. Inspect party

6. Save game

The console project should only:

- display authoritative state;
- display available commands;
- collect simple input;
- send commands to the application layer;
- print command results.

It should not contain game rules or campaign logic.

The first console walking skeleton should include:

### Start a campaign

→ use a pre-generated party

→ choose a regional destination

→ travel there

→ enter a small exploration map

→ move and turn

→ interact with one object or NPC

→ trigger one tactical encounter

→ resolve the encounter

→ apply its consequences

→ return to exploration

→ leave the location

→ save and reload

### Basic enemy behavior

A very small enemy decision system will probably be required here so the player can complete encounters.

The first AI should be intentionally simple:

- discover legal actions;
- attack a reachable target;
- move toward a target when necessary;
- end its turn when no productive action exists.

It does not need sophisticated tactics yet. Its purpose is to make the complete game loop playable without manually controlling both sides.

### Result of this phase

The game can be played from beginning to end in text form, even though the content is tiny and the presentation is crude.

This is the first major medium-term decision gate. We should review the architecture here before adding Godot.

---

Medium-term Phase 5 — Establish save and load as an architectural feature

Save and load should be added before the Godot client, not deferred until late production.

The saved state should include enough information to reconstruct:

- campaign identity;
- party members and persistent statistics;
- hit points and spent resources;
- inventory and ammunition;
- campaign time;
- current mode;
- current location;
- exploration map position and facing;
- world flags;
- active encounter, if saving during combat is supported;
- content/version identifiers.

The first save format does not need indefinite backward compatibility, but it should be versioned from the beginning.

Save/load testing should prove that:

1. a session can be saved;

2. the process can be restarted;

3. the session can be loaded;

4. the same legal actions are available;

5. subsequent commands produce coherent results.

### Result of this phase

The walking skeleton is persistent rather than merely an in-memory demonstration.

---

### Medium-term Phase 6 — Reproduce the walking skeleton in Godot

Once the console version proves the complete loop, we should create the Godot client.

The first Godot implementation should reproduce the same scenario, not introduce a different or broader game.

It should initially include:

### Regional view

- a simple regional map;
- selectable locations;
- a party marker;
- basic travel-result messages.

### Exploration view

- split-screen or Gold Box-inspired presentation;
- first-person scene panel;
- movement and turning controls;
- contextual interaction options;
- party/status information.

The rendered first-person environment can initially use crude walls, flat textures, placeholders, or generated panels.

### Tactical view

- a bird’s-eye grid;
- combatant markers;
- selectable legal actions;
- target selection;
- movement visualization;
- combat-result display.

### General interface

- start/load campaign;
- save game;
- transition between modes;
- basic party inspection;
- error and unavailable-action feedback.

The Godot client must use the same application commands as the console client.

No combat calculation, map legality, quest decision, or state mutation should be reimplemented in GDScript or presentation code.

### Result of this phase

The complete loop works graphically:

### Regional map

→ exploration view

→ interaction

→ tactical combat

→ persistent aftermath

→ exploration

This is the second major decision gate. At this point we should evaluate whether the application API, state models, and result objects are actually convenient for a graphical client.

---

Medium-term Phase 7 — Convert the demonstration into a true vertical slice

Once both clients can run the walking skeleton, the next task is to turn the technical demonstration into a small but recognizable game segment.

The vertical slice should contain approximately:

- one small safe settlement or starting location;
- one compact regional map;
- two or three destinations;
- one small dungeon or hostile site;
- one NPC conversation;
- one shop, inn, or service;
- one simple quest;
- one meaningful world-state change;
- several tactical encounters;
- basic enemy AI;
- melee and ranged attacks;
- one offensive spell or supernatural ability;
- one healing action;
- one meaningful condition;
- resting;
- rewards;
- save/load;
- victory and defeat outcomes.

This should still be far smaller than a complete chapter.

The purpose is to prove that the game can support variation—not merely one hard-coded path.

### Content should begin separating from engine code

During this phase, maps, locations, encounters, NPCs, shops, and simple events should begin moving into structured content definitions.

However, the content system should remain narrow and explicit.

For example, the first event system may support only a handful of event types:

DisplayInteraction

SetWorldFlag

RequireWorldFlag

StartEncounter

MoveParty

GrantItem

AdvanceTime

OpenDoor

ChangeLocation

That is preferable to prematurely creating a general scripting language.

### Result of this phase

The project becomes a genuine early game rather than a technical sample.

A new player should be able to understand the objective, explore, make at least one decision, fight several encounters, use a service, and reach a conclusion.

---

### Medium-term Phase 8 — Stabilize the architecture based on real usage

After the vertical slice works, we should pause expansion and examine where the architecture resisted real development.

Likely review areas include:

- whether CampaignSession owns too much;
- whether commands and results are too granular or too broad;
- whether party state is duplicated;
- whether encounter transitions lose information;
- whether content definitions require too much C#;
- whether the Godot client must inspect internal engine details;
- whether saves contain derived data that should be recalculated;
- whether AI can consume legal-action discovery cleanly;
- whether map triggers and world flags are understandable;
- whether tests cover complete workflows in addition to isolated rules.

This is the right time for measured refactoring because we will have evidence from:

- the console client;
- the Godot client;
- travel;
- exploration;
- combat;
- persistence;
- real content.

### Result of this phase

The architecture becomes a proven foundation for larger-scale campaign development rather than a design based mainly on predictions.

---

### Medium-term priority order

When priorities conflict, we should use this ordering:

1. Complete end-to-end flow

2. Persistent and authoritative state

3. Correct transitions between modes

4. Clean separation between engine and clients

5. Save/load reliability

6. Content authoring efficiency

7. Basic enemy competence

8. Additional mechanical breadth

9. Presentation polish

10. Advanced tooling and extensibility

This means a complete loop with limited mechanics is more valuable than a sophisticated combat subsystem that still cannot return the party to exploration.

---

### Features to defer during the medium term

Unless the vertical slice directly requires them, we should defer:

- complete 5e class and subclass coverage;
- hundreds of spells;
- comprehensive monster support;
- advanced tactical AI;
- procedural world generation;
- full weather and survival simulation;
- a universal dialogue language;
- extensive modding APIs;
- multiplayer;
- cinematic systems;
- polished art and animation pipelines;
- elaborate internal editors;
- broad campaign creation tools;
- platform-specific release work.

Some of these may eventually be desirable, but they should not delay proving the game’s primary loop.

---

The medium-term endpoint

The medium-term progression is complete when the repository contains:

FiveEGoldBox.Core

### Stable mechanical and encounter rules

FiveEGoldBox.Application

### Campaign state, commands, transitions, and persistence

FiveEGoldBox.Console

### Thin reference client

FiveEGoldBox.Godot

### Playable graphical client

And the game supports:

### Create or load a campaign

→ manage a persistent party

→ travel across a small regional map

→ enter and navigate a first-person location

→ interact with an NPC or object

→ begin and resolve tactical combat

→ preserve damage, resources, rewards, and world changes

→ return to exploration and travel

→ save and continue later

→ complete a small vertical-slice objective

At that point, the project will no longer primarily be a rules-engine prototype.

It will be an early but authentic version of the intended Gold Box-style RPG, ready for the long production phase of expanding mechanics, content, tools, presentation, and campaign scope.

### Short Term

### Short-term development plan

The short term should begin with the repository exactly as it exists now and end with the first complete text-only walking skeleton.

That means the immediate objective is no longer “add more combat rules.” It is:

Prove that the existing rules engine can participate in a persistent game loop spanning travel, exploration, combat, consequences, and saving.

Godot should remain just beyond the short-term endpoint. Once the text loop works and its architecture has been reviewed, the medium-term Godot phase can begin with much less uncertainty.

---

### Short-term Phase 1 — Close the current tactical-combat seam

Our first branch should finish the cover work by applying the new half-cover and three-quarters-cover bonuses to applicable Dexterity saving throws.

That is worth completing because:

- the public cover result already exposes the bonus;
- leaving it unused creates a misleading or incomplete API;
- it should be a small integration rather than a new subsystem;
- it gives the tactical-cover milestone a clean stopping point.

After that branch, we should impose a temporary combat feature freeze.

The freeze does not mean combat is finished. It means we stop adding broad new mechanics unless the first playable loop cannot function without them.

### Confirm before moving on

We should verify that the encounter runtime already supports—or can support with only narrow integration work—the following:

- creating an encounter from externally supplied participants and battlefield data;
- discovering legal player actions;
- processing movement and basic weapon attacks;
- progressing initiative and turns;
- determining encounter completion;
- preserving participant hit points, ammunition, lifecycle state, and other persistent consequences;
- returning a result that a campaign layer can apply.

Any missing item should be addressed narrowly. We should not turn this into a comprehensive combat-completion phase.

### Exit condition

One simple encounter can be created, played to completion, and summarized in a form usable by a future campaign session.

---

Short-term Phase 2 — Define the first playable scenario before designing more architecture

Before creating new projects and abstractions, we should define the exact miniature scenario the architecture must support.

The first scenario should be deliberately constrained:

### Begin at a safe regional location

→ travel to one hostile destination

→ enter a tiny exploration map

→ move and turn through several cells

→ interact with one door or object

→ trigger one predefined encounter

→ defeat or lose to the enemies

→ apply persistent consequences

→ return to exploration

→ leave or finish the scenario

→ save and reload

We should identify only the content required for that scenario:

- one pre-generated party;
- two regional locations;
- one route;
- one exploration map;
- one door or interactive feature;
- one encounter trigger;
- one tactical battlefield;
- one enemy group;
- one victory outcome;
- one defeat outcome.

This becomes our architectural test case.

### Why this comes first

Without a concrete scenario, we risk designing generic campaign, travel, event, and content systems based on imagined future requirements. The external analysis correctly warns against continuing to build systems without proving the complete game loop.

### Exit condition

We have a written, bounded walking-skeleton specification that tells us exactly what every new system must accomplish.

---

### Short-term Phase 3 — Introduce the application-layer boundary

The first significant structural change should be a new application project, probably:

src/FiveEGoldBox.Application

Its dependency direction should initially be simple:

FiveEGoldBox.Application

→ FiveEGoldBox.Core

The core project should not reference the application project.

This new layer should coordinate gameplay without knowing whether the client is a console, Godot, tests, or some future tool.

Its first responsibilities should be limited to:

- owning the active campaign session;
- receiving player-intent commands;
- invoking core rules;
- applying results to persistent state;
- controlling transitions between game modes;
- exposing enough state for a client to display.

### Initial concepts

We will likely need small versions of:

CampaignSession

PartyState

CampaignClock

GameMode

CurrentLocation

TravelState

ExplorationState

ActiveEncounter

WorldState

These names are provisional. We should allow the code to refine them.

### Avoid premature infrastructure

We should not begin with:

- a mediator framework;
- an event bus;
- dependency-injection infrastructure;
- event sourcing;
- a universal command dispatcher;
- dozens of projects;
- repository patterns for in-memory objects;
- generalized plugin systems.

Direct, explicit application services and typed commands are preferable until repetition proves a stronger abstraction is needed.

### Exit condition

A test can create a minimal campaign session and observe an authoritative current mode, party, location, and campaign clock.

---

### Short-term Phase 4 — Establish persistent party and campaign state

The existing character resolver produces useful calculated character information, but the game now needs persistent characters that survive across modes.

We should establish a distinction between:

- relatively stable character definitions or resolved capabilities;
- mutable campaign state.

The mutable state will eventually include things such as:

- current and maximum hit points;
- ammunition;
- inventory;
- currency;
- temporary conditions;
- expended resources;
- death or incapacitation;
- experience;
- prepared abilities;
- equipment state.

The first implementation should contain only what the walking skeleton actually uses.

We also need clear rules for moving character data into and out of encounters:

### Persistent party member

→ encounter participant

→ encounter changes

→ persistent party member updated

This transition must not accidentally reset damage, ammunition, or death state after combat.

### Exit condition

A party member can enter an encounter, take damage or spend ammunition, leave the encounter, and retain those changes in the campaign session.

---

### Short-term Phase 5 — Implement the minimal regional travel model

The first travel system should be intentionally small and deterministic.

A location-and-route graph is the strongest starting model:

### Safe Camp

↓ one route

### Ruined Keep

It should support:

- identified locations;
- available routes;
- destination selection;
- travel duration;
- advancing the campaign clock;
- arrival at the destination;
- transitioning into an associated exploration map.

One predefined interruption can be added later if needed to prove encounter triggering during travel, but it is not required for the very first end-to-end path.

We should not yet implement:

- hex movement;
- dynamic weather;
- supplies;
- navigation checks;
- terrain simulation;
- procedural encounters;
- discovery systems;
- boats or mounts.

### Exit condition

The campaign can legally travel from one location to another, advance time, and arrive in the correct state.

---

Short-term Phase 6 — Implement the minimal first-person exploration model

Exploration should receive its own domain model rather than reusing the encounter battlefield.

The initial map should contain:

- a small rectangular cell layout;
- a starting cell;
- a starting facing;
- passable and impassable edges;
- one door or interactive boundary;
- one encounter-trigger cell;
- one exit or completion location.

The application layer should support commands equivalent to:

MoveForward

TurnLeft

TurnRight

Interact

The exploration result should explain:

- whether the action succeeded;
- the new position or facing;
- why movement failed;
- whether an interaction occurred;
- whether a transition or encounter was triggered;
- how much campaign time passed.

### Important design rule

The map should be modeled according to exploration semantics:

### Cell + facing + directional boundaries + triggers

It should not merely be a tactical grid with the camera pointed forward.

### Exit condition

Tests can move the party through the complete tiny map, reject illegal movement, operate the interactive feature, and activate the encounter trigger.

---

### Short-term Phase 7 — Build the exploration-to-combat bridge

This is the most important short-term integration milestone.

When the party activates the encounter trigger, the application layer should:

1. record the exploration context;

2. create the tactical encounter from content;

3. translate persistent party members into encounter participants;

4. change the campaign mode to tactical combat;

5. expose the encounter’s legal actions;

6. play the encounter to completion;

7. apply participant consequences back to the persistent party;

8. apply encounter rewards or defeat consequences;

9. return to the correct exploration state or end the scenario.

The encounter runtime should not know about dungeon cells, quests, campaign locations, or UI transitions.

The exploration layer should not calculate combat.

The application layer owns the bridge.

### Encounter completion result

We will likely need an application-facing result that communicates facts such as:

- winning side;
- surviving participants;
- defeated participants;
- persistent hit-point changes;
- expended ammunition or resources;
- rewards;
- world flags to set;
- the mode and location to resume.

This should be narrow and explicit rather than a generic dictionary of arbitrary changes.

### Exit condition

A fully automated integration test can begin in exploration, trigger combat, resolve it, and verify that the party returns with the correct persistent state.

---

### Short-term Phase 8 — Add the minimum enemy-turn policy

The text game cannot be meaningfully playable if the user must manually control enemies.

The first AI should be extremely limited and deterministic.

It should:

- obtain legal actions through the same discovery system used for players;
- attack a valid target when possible;
- move toward an appropriate target when no attack is available;
- end its turn when no useful action exists.

It does not need:

- threat maps;
- coordinated tactics;
- personality;
- spell strategy;
- cover-seeking;
- retreat behavior;
- difficulty profiles;
- learning or planning trees.

The objective is not intelligent opposition. It is an autonomous encounter that can reach a conclusion.

### Architectural principle

The AI should choose among legal commands. It should not bypass the rules by mutating encounter state directly.

### Exit condition

A basic hostile group can complete all of its turns without player intervention or illegal actions.

---

### Short-term Phase 9 — Create the thin console reference client

Once the application tests can exercise the whole loop, we should add:

src/FiveEGoldBox.Console

The console client should be a simple adapter.

It should:

- start a predefined campaign;
- display the current mode;
- display relevant state;
- list currently available actions;
- accept numbered input;
- send one application command;
- print the result;
- repeat.

A typical flow might appear as:

### Mode: Regional Travel

### Location: Safe Camp

1. Travel to Ruined Keep

2. Inspect party

3. Save

Then:

### Mode: Exploration

### Location: Ruined Keep

### Position: 1,2

### Facing: North

1. Move forward

2. Turn left

3. Turn right

4. Interact

5. Inspect party

6. Save

Then combat actions should come from authoritative action discovery rather than a separately maintained menu.

### What not to build

The console does not need:

- an ASCII first-person renderer;
- command-line parsing beyond simple choices;
- animations;
- elaborate colors;
- a menu framework;
- console-specific rules;
- a second campaign model.

### Exit condition

A person can manually complete the walking skeleton from the terminal without editing state or invoking tests.

---

### Short-term Phase 10 — Add save and load for the walking skeleton

Once the loop works in memory, we should serialize it.

The first save format should include:

- save-format version;
- campaign/content identity;
- party state;
- campaign clock;
- current mode;
- current location;
- exploration position and facing;
- relevant world flags;
- encounter state if mid-combat saving is included.

We should make an explicit early decision about mid-combat saves.

My recommendation for the first implementation is:

Support saves in travel and exploration first; defer saving during an active encounter unless it proves straightforward.

This reduces complexity while still testing persistence across the primary campaign state.

The save system should separate:

- authoritative persisted state;
- definitions loaded from content;
- values that can be recalculated.

We should avoid serializing duplicated derived state merely because it is convenient.

### Exit condition

The user can save during the walking skeleton, close the program, reload, and continue with the same party, location, time, world state, and available actions.

---

### Short-term Phase 11 — Perform an architectural review before Godot

Once the text-only loop works, we should stop and evaluate what we actually learned.

Questions to answer include:

- Is the application layer truly independent of the console?
- Does CampaignSession own the right responsibilities?
- Are mode transitions explicit and testable?
- Is party state duplicated between campaign and encounter models?
- Can a graphical client obtain legal actions without understanding internal rules?
- Are command results descriptive enough for animations and messages?
- Does exploration state expose what a first-person renderer will need?
- Are content definitions separate enough from runtime state?
- Can saving reconstruct the session without hidden process state?
- Did any game logic accidentally leak into the console?

We should correct meaningful architectural problems here before introducing Godot, because Godot will make every poor client boundary more expensive.

### Exit condition

The console can be conceptually removed and replaced by another client without changing campaign, exploration, travel, or encounter logic.

---

### Recommended short-term order

The overall order should be:

1. Apply cover to Dexterity saves

2. Freeze broad combat expansion

3. Define the walking-skeleton scenario

4. Add the application project

5. Establish campaign and persistent party state

6. Add minimal travel

7. Add minimal exploration

8. Connect exploration to combat and back

9. Add basic enemy turns

10. Build the console client

11. Add save/load

12. Review and stabilize the architecture

13. Begin Godot in the medium-term phase

---

### What we should deliberately avoid in the short term

The following would distract from the critical path:

- completing the full 5e combat rules;
- broad spell implementation;
- full character advancement;
- extensive inventory interfaces;
- sophisticated enemy AI;
- procedural travel;
- weather and survival systems;
- a generalized quest framework;
- a dialogue scripting language;
- comprehensive content tooling;
- polished terminal presentation;
- Godot scene development before the headless loop works;
- premature engine modularization into many assemblies;
- designing the complete campaign.

These features are not rejected. They are deferred until the walking skeleton gives us evidence about how they should fit.

---

### Short-term success criteria

The short-term plan is complete when all of the following are true:

- main still builds and passes all tests;
- the combat runtime can support one complete basic encounter;
- a campaign session owns a persistent party, clock, location, and current mode;
- the party can travel between two locations;
- the party can enter and navigate one exploration map;
- exploration can trigger tactical combat;
- enemy turns can execute automatically;
- combat consequences persist after returning to exploration;
- the complete loop is manually playable through a thin console client;
- the campaign can be saved and reloaded;
- no game rules live in the console project;
- the architecture has been reviewed specifically for future Godot integration.

The short-term destination is therefore:

A small, ugly, text-only game that proves the full architecture of the intended graphical RPG.

That will be far more strategically valuable than another large collection of isolated mechanical rules.

### Next Step

Next development phase: close and prove the tactical-combat foundation

The next phase should be a small, deliberate closure phase before we begin campaign, travel, and exploration work.

Its purpose is not to make combat comprehensive. Its purpose is to answer one practical question:

Can the existing encounter runtime cleanly accept persistent characters, run one basic battle through public APIs, and return enough authoritative information for a future campaign layer to preserve the consequences?

This phase should require roughly two planned branches and, at most, one corrective branch. It should end with a temporary freeze on broad combat expansion.

---

1. Phase objective

By the end of this phase, the engine should be able to support this headless sequence:

### Create a basic encounter

→ supply participants and battlefield data

→ discover legal actions

→ execute movement and weapon attacks

→ advance turns

→ resolve enemy turns through legal actions

→ determine victory or defeat

→ expose surviving participant state

→ preserve damage, ammunition, death, and other relevant consequences

This is not yet the campaign loop. It is proof that combat is ready to be inserted into that loop.

The phase should not introduce:

- the application project;
- campaign sessions;
- exploration;
- travel;
- Godot;
- a spell system;
- advanced AI;
- broad new combat mechanics.

Those belong to later phases.

---

### Workstream 1 — Correctly integrate cover with Dexterity saves

### Why this comes first

The tactical-cover feature currently exposes:

ArmorClassBonus

DexteritySavingThrowBonus

The armor-class bonus is consumed by weapon attacks. The Dexterity-saving-throw bonus is currently informational only.

Leaving a public result value unused creates an incomplete contract. Either the engine should use it authoritatively or it should not expose it yet. Since cover legitimately affects appropriate Dexterity saves, the correct course is to complete the integration.

### Architectural rule

Battlefield geometry must remain in the encounter layer.

Generic saving-throw rules should not learn about:

- grid positions;
- battlefields;
- line of sight;
- cover positions;
- encounter participants.

The generic rules should continue to resolve something equivalent to:

d20 roll

+ character saving-throw modifier

+ applicable situational modifier

against a difficulty class

The encounter layer should determine the situational modifier.

Conceptually:

### Encounter context

↓

### Determine whether cover is applicable

↓

### Evaluate cover between effect origin and target

↓

### Pass resulting modifier into generic saving-throw resolution

This prevents the core saving-throw calculation from becoming coupled to tactical maps.

---

### Cover must not apply to every Dexterity save

This is the most important correctness constraint.

A combatant should not automatically receive cover on every Dexterity saving throw merely because cover exists between two positions.

Examples where battlefield cover might apply:

- a directional magical effect;
- a projectile-like effect that calls for a Dexterity save;
- an effect originating from another battlefield position.

Examples where it should not automatically apply:

- poison already affecting the combatant;
- a trap beneath the combatant;
- an internal or self-originating effect;
- an effect whose rules explicitly ignore cover;
- an environmental save with no meaningful source position.

Therefore, the encounter-level operation must know:

1. the effect’s origin;

2. the target;

3. whether the effect permits cover;

4. the saving-throw ability;

5. the save difficulty class.

We should not infer all of that from “this is a Dexterity save.”

---

### Minimal implementation shape

We should first inspect the existing saving-throw API and encounter runtime rather than inventing new types prematurely.

The preferred implementation is one of these, in order:

### Preferred: extend an existing encounter saving-throw path

If an encounter-specific saving-throw resolver already exists, extend it so that it can accept:

- effect origin;
- target participant;
- cover applicability;
- saving-throw ability;
- difficulty class.

It should then reuse EncounterCoverRules.

### Acceptable: add a narrow encounter adapter

If only generic saving-throw rules exist, add a small encounter-level wrapper such as:

EncounterSavingThrowRules

Its responsibility would be orchestration, not duplicate dice or modifier calculation.

It should:

- validate the encounter;
- locate the target;
- resolve the target position;
- evaluate cover when applicable;
- supply the total situational modifier to the generic rule;
- return both the saving-throw result and cover evaluation.

We should not create a generalized spell-effect framework merely to support this integration.

---

### Result transparency

The returned result should make the calculation explainable to future clients.

A Godot or console client may eventually need to display:

### Dexterity save: 11

### Character modifier: +3

### Half-cover bonus: +2

### Total: 16

### Difficulty class: 15

Success

The engine should expose enough structured information to produce that message without forcing the client to reconstruct the rules.

That does not necessarily mean adding many new types. It means preserving the meaningful components of the calculation rather than returning only true or false.

---

### Required tests

The branch should prove at least these cases:

- no cover gives no bonus;
- half cover gives +2;
- three-quarters cover gives +5;
- strongest intersecting cover applies;
- cover outside the source-to-target path does not apply;
- an effect that does not permit cover receives no bonus;
- a non-Dexterity saving throw receives no cover bonus;
- total obstruction is handled consistently with the existing targeting or line-of-sight policy;
- invalid source or target information is rejected;
- the returned calculation exposes the applied cover bonus.

We should avoid testing every cover-validation case again if those are already thoroughly tested by EncounterCoverRules. The saving-throw tests should focus on integration, not duplicate the cover subsystem’s entire unit-test suite.

---

### First branch

feature/apply-cover-to-dexterity-saves

This branch should contain only:

- encounter-level save integration;
- any narrowly required result changes;
- focused tests.

It should not contain unrelated refactoring.

---

### Workstream 2 — Prove one complete encounter lifecycle

After cover integration is merged, we should stop adding combat mechanics and write one meaningful end-to-end encounter test.

This test should use the public runtime APIs in approximately the way the future application layer will use them.

### What the test should represent

Use a very small deterministic encounter:

- two player-side combatants;
- one or two hostile combatants;
- a simple battlefield;
- basic movement;
- melee or ranged weapon attacks;
- deterministic initiative and rolls;
- at least one combatant taking damage;
- at least one expended persistent resource, preferably ammunition;
- encounter completion.

The exact participants are unimportant. What matters is exercising the actual lifecycle.

### What it must prove

### Encounter creation

External code can construct the encounter from supplied:

- participants;
- sides;
- positions;
- initiative information;
- battlefield data;
- combat profiles.

The test should not rely on hidden test-only mutation.

### Legal-action discovery

The driver should obtain available actions from the runtime wherever practical rather than hard-coding actions that might be illegal.

This proves that a future console client, Godot client, and enemy AI can use the same discovery mechanisms.

### State transitions

The encounter should proceed through:

Start

→ active turn

→ actions

→ end turn

→ next participant

→ completion

No direct manipulation should be used merely to make the test finish faster.

### Persistent consequences

When the encounter ends, the final authoritative state must expose enough information to determine:

- current hit points;
- lifecycle or death state;
- ammunition spent;
- remaining relevant turn-independent resources;
- winning side;
- encounter completion reason, if one exists.

At this point, we do not need a campaign party model. We need to prove that a future party model could receive these facts without scraping logs or re-running combat calculations.

---

### What this test should not become

It should not become:

- a full campaign simulation;
- a console program inside the test project;
- a scripted twenty-round combat transcript;
- a broad test of every combat rule;
- a second encounter engine;
- a test helper framework larger than the scenario itself.

The test should use small helpers only where they reduce noise without hiding the important sequence.

---

### Second branch

test/prove-encounter-lifecycle

This branch should ideally add tests only.

That is valuable because the test is intended to reveal whether the existing public API is actually usable. We should not preemptively alter production code before the test demonstrates a specific problem.

---

### Workstream 3 — Correct only integration-blocking gaps

The lifecycle test may expose a missing seam. That is expected.

Examples of legitimate blockers include:

- encounter completion does not expose the winning side clearly;
- final participant state is inaccessible;
- ammunition expenditure cannot be projected back to a persistent character;
- participant identity is lost during encounter construction;
- legal-action discovery cannot drive a basic turn;
- encounter creation requires inappropriate internal knowledge;
- completion state cannot distinguish victory from another termination;
- final state contains only derived combat copies with no stable identity mapping.

If such a blocker appears, we should open one narrow corrective branch.

Possible branch:

feature/complete-encounter-handoff

The name should ultimately describe the actual defect found, not use this generic name unless the change genuinely concerns the entire handoff.

### Restraint rule

We should fix only what prevents the future application layer from consuming the encounter.

For example, if stable participant identity is missing, add stable identity. Do not simultaneously create:

- campaign characters;
- reward processing;
- quest consequences;
- save-game objects;
- generalized domain events.

Those are application-layer concerns.

---

### Workstream 4 — Establish the combat/application boundary

At the end of the phase, we should be able to describe the future integration contract clearly.

### Information entering combat

The future application layer will provide:

- stable participant identities;
- combat-ready participant profiles;
- mutable starting state such as hit points and ammunition;
- sides;
- initial positions;
- battlefield definition;
- encounter configuration.

### Information owned during combat

The encounter runtime owns:

- initiative;
- current turn;
- legal actions;
- movement;
- attacks;
- damage;
- death saves;
- battlefield occupancy;
- encounter completion.

### Information leaving combat

The future application layer should be able to obtain:

- completion status;
- winning or surviving side;
- each participant’s stable identity;
- final hit points;
- lifecycle state;
- expended ammunition;
- other explicitly persistent resources used by the scenario.

### Information combat should not own

The encounter runtime should not decide:

- experience rewards;
- treasure;
- quest flags;
- campaign time after the battle;
- where the party resumes;
- whether defeated enemies remain dead in the world;
- whether a location becomes safe;
- narrative outcomes.

Those decisions belong in the future application layer.

This separation is central to avoiding an oversized combat engine.

---

### Procedural plan

Each branch should follow the existing disciplined workflow:

1. Begin from synchronized main.

2. Create one narrowly named branch.

3. Inspect the relevant code before proposing changes.

4. Add or modify the smallest coherent behavior.

5. Add focused tests.

6. Run the complete suite.

7. Review the full diff, including new files.

8. Remove temporary review artifacts.

9. Stage only intended files.

10. Commit once the staged diff is clean.

11. Push, create a pull request, and wait for CI.

12. Merge only after CI passes.

13. update local main;

14. run the full suite again.

We should preserve the current standards:

- one concern per branch;
- behaviorally explicit tests;
- no unrelated cleanup;
- no speculative abstractions;
- no UI dependencies in core;
- no weakening validation merely to simplify integration.

---

### Economy-of-code principles

### Reuse the existing cover evaluator

There should be one authoritative mapping:

### No cover → 0

### Half cover → 2

### Three-quarters cover → 5

Saving throws should consume EncounterCoverEvaluation. They should not reproduce that switch elsewhere.

### Keep dice resolution generic

Encounter-specific logic should calculate modifiers and then call existing saving-throw rules.

It should not duplicate d20 rolling, modifier addition, difficulty-class comparison, or critical behavior.

### Avoid a general effect engine

This phase does not justify abstractions such as:

ICombatEffect

IEffectOrigin

IEffectTargetingStrategy

ICoverAwareEffect

ISavingThrowEffectPipeline

Those may eventually become useful, but not until several real spells, traps, and environmental effects reveal stable common behavior.

### Prefer explicit parameters over speculative frameworks

A clear method with source, target, ability, DC, and cover applicability is currently better than a configurable effect-processing framework.

### Let the lifecycle test expose the boundary

Do not create an EncounterOutcome, CampaignCombatResult, or projection framework merely because one might be useful.

First write the test. Add the smallest production type only if the current state cannot support a clean handoff.

---

### Explicit non-goals

This phase does not include:

- offensive spells;
- areas of effect;
- concentration;
- reactions;
- opportunity attacks;
- conditions expansion;
- Extra Attack;
- Multiattack;
- pathfinding;
- tactical previews;
- advanced enemy AI;
- experience or treasure;
- campaign parties;
- travel or exploration;
- save/load;
- Godot;
- console UI.

Even if one of those becomes tempting while touching related code, it should be added to the backlog rather than absorbed into this phase.

---

### Exit criteria

This phase is complete when all of the following are true:

- cover bonuses are authoritatively applied to eligible Dexterity saves;
- ineligible saves do not receive cover accidentally;
- generic saving-throw rules remain independent of battlefield geometry;
- one deterministic encounter can be driven through public APIs from creation to completion;
- legal-action discovery participates in that flow;
- final participant state exposes the persistent consequences required by a future campaign layer;
- any integration blocker discovered by the lifecycle test has been corrected narrowly;
- all tests pass on merged main;
- no console, Godot, campaign, travel, or exploration code has been added;
- broad combat feature development is formally paused.

---

### What comes immediately afterward

Only after this phase is complete should we define the exact walking-skeleton scenario and create:

FiveEGoldBox.Application

That next phase will use the proven encounter boundary rather than guessing how campaign state should interact with combat.

The immediate development course is therefore:

### Complete cover-aware Dexterity saves

→ prove one complete encounter lifecycle

→ correct only demonstrated handoff gaps

→ freeze broad combat expansion

→ begin the campaign/application backbone

This is the smallest responsible bridge between the strong rules foundation we already have and the complete game architecture we now need.

### First Workflow

### Next workflow: close and prove the combat foundation

This workflow should contain two required pull requests and no more than one conditional corrective pull request.

Its purpose is to finish the cover seam, prove that a complete encounter can be driven through public APIs, and then deliberately stop expanding combat until the campaign layer exists.

### PR 1 — Cover-aware Dexterity saves

↓

### PR 2 — Complete encounter lifecycle proof

↓

Optional corrective PR, only if the lifecycle proof exposes a real API gap

↓

### Combat-foundation review and temporary feature freeze

↓

### Begin FiveEGoldBox.Application

---

### Workflow rule zero: lock the scope

Before touching code, we should explicitly treat the following as out of scope:

- new spells;
- areas of effect;
- concentration;
- reactions;
- opportunity attacks;
- broader condition support;
- Extra Attack or Multiattack;
- pathfinding;
- advanced AI;
- campaign models;
- exploration;
- travel;
- console UI;
- Godot;
- save/load.

We should record tempting discoveries as later work rather than allowing them into these branches.

The only acceptable production changes during this workflow are:

1. cover-aware encounter saving throws;

2. narrow corrections required for an external caller to run and consume one encounter.

---

### PR 1 — Apply cover to eligible Dexterity saving throws

Proposed branch:

feature/apply-cover-to-dexterity-saves

### Step 1 — Investigate the existing saving-throw path

The first work should be read-only.

We should identify:

- the generic saving-throw resolver;
- its input and result models;
- whether it already accepts situational modifiers;
- any encounter-specific saving-throw code;
- how encounter participants expose saving-throw bonuses;
- how source and target positions are represented;
- whether line of sight is already calculated for save-based effects;
- which existing tests establish saving-throw conventions.

We should not design a new API until we know whether an appropriate seam already exists.

### Investigation decision

After inspection, choose one of two implementations:

Preferred: extend an existing encounter-level saving-throw operation.

Fallback: add one narrow EncounterSavingThrowRules adapter that calculates encounter-specific modifiers and delegates the actual d20 resolution to the existing generic rules.

We should not change generic saving-throw rules to understand battlefields or cover positions.

---

### Step 2 — Define the exact behavior contract

Before production code, define the behavior in tests.

Cover applies only when all of these are true:

- the saving throw uses Dexterity;
- the effect has a meaningful source position;
- the effect permits cover;
- line of sight from source to target is not totally blocked;
- partial cover intersects the source-to-target path.

The expected bonuses remain:

### No cover             +0

### Half cover           +2

### Three-quarters cover +5

Cover must not apply merely because the target is making a Dexterity save.

The operation should explicitly distinguish:

- Dexterity saves that permit cover;
- Dexterity saves that ignore cover;
- non-Dexterity saves;
- saves with no external origin.

That distinction should be represented by an explicit input, not inferred from unrelated details.

---

### Step 3 — Add focused failing tests

The branch should begin by proving the integration behavior rather than retesting the entire cover subsystem.

Minimum focused cases:

1. Eligible Dexterity save with no cover receives +0.

2. Half cover contributes +2.

3. Three-quarters cover contributes +5.

4. Cover outside the relevant path contributes nothing.

5. A save that explicitly disallows cover receives nothing.

6. A Strength, Constitution, Intelligence, Wisdom, or Charisma save receives nothing.

7. Invalid source or target information is rejected.

8. The result exposes the actual applied situational bonus.

9. The bonus affects success or failure, not merely descriptive output.

Existing EncounterCoverRules tests already prove cover-position validation and strongest-cover selection. Those cases should not be duplicated unnecessarily.

---

### Step 4 — Implement the narrowest integration

The encounter-level operation should:

1. validate the encounter and target;

2. identify the source and target positions;

3. determine whether cover is eligible;

4. call EncounterCoverRules.Evaluate when appropriate;

5. take its DexteritySavingThrowBonus;

6. pass that as a situational modifier into the generic saving-throw resolver;

7. return the saving-throw result together with the applied cover evaluation or modifier.

The implementation should reuse existing:

- dice rolling;
- saving-throw modifier calculation;
- difficulty-class comparison;
- cover evaluation;
- battlefield validation.

It should not duplicate any of those calculations.

---

### Step 5 — Review the public result

The result should allow a future client to explain:

### Natural roll:             11

### Character saving bonus:   +3

### Half-cover bonus:         +2

### Final total:              16

### Difficulty class:         15

### Outcome:                  Success

The client should not need to recalculate the total or rediscover why the bonus applied.

We should expose structured values, but avoid creating an elaborate effect-resolution hierarchy.

---

### Step 6 — Complete the branch normally

The branch completion procedure should remain:

1. targeted tests;

2. full dotnet test;

3. git diff --check;

4. complete diff review, including untracked files;

5. stage only intended files;

6. inspect staged statistics and diff;

7. commit;

8. push;

9. create PR;

10. wait for CI;

11. merge;

12. update local main;

13. rerun the complete suite.

### PR 1 exit gate

Do not proceed until:

- eligible Dexterity saves receive cover correctly;
- ineligible saves do not;
- generic saving-throw rules remain battlefield-independent;
- the calculation is transparent to clients;
- all tests pass on merged main.

---

### PR 2 — Prove a complete encounter lifecycle

Proposed branch:

test/prove-encounter-lifecycle

This should begin as a test-only branch.

Its job is not to add more mechanics. Its job is to use the encounter runtime the way the future application layer will use it.

---

### Step 1 — Define one deterministic encounter scenario

Use a deliberately small battle:

- two allied participants;
- one or two hostile participants;
- a compact battlefield;
- simple initiative;
- one melee-capable participant;
- one ranged participant with ammunition;
- deterministic rolls;
- movement;
- attacks;
- damage;
- at least one resource expenditure;
- a clear winning side.

The encounter should be short enough to understand during code review.

Avoid a long scripted transcript with many rounds.

---

### Step 2 — Drive it through public APIs

The test should perform the real sequence:

### Construct encounter

→ validate initial state

→ inspect current participant

→ discover legal actions

→ choose a legal action

→ execute it

→ inspect returned state and result

→ end or advance the turn

→ repeat

→ observe encounter completion

The test must not:

- modify participant hit points directly;
- alter initiative order directly;
- remove enemies manually;
- bypass action prerequisites;
- mutate ammunition merely to simulate its expenditure;
- force completion through test-only hooks.

It should act like an external application client.

---

### Step 3 — Use action discovery meaningfully

Where discovery APIs exist, the test should confirm that intended actions are discoverable before using them.

It does not need to test every discovery detail again, but it should demonstrate that an external caller can:

- determine who may act;
- determine what broad actions are legal;
- select a legal target or destination;
- submit the action.

This is critical because the future console client, Godot client, and enemy AI must not maintain independent legality rules.

---

### Step 4 — Prove persistent consequences are available

At completion, the test should verify that final authoritative state provides:

- encounter completion status;
- winning side or equivalent outcome;
- every participant’s stable identity;
- final hit points;
- lifecycle or death state;
- ammunition remaining;
- any other resource actually consumed by the scenario;
- enough participant information to map the result back to a persistent party member.

The test does not yet apply those facts to PartyState. That belongs to the application layer.

It proves that such an application layer could do so cleanly.

---

### Step 5 — Keep the test readable

A small scenario helper may be appropriate for:

- creating known combatants;
- supplying deterministic dice;
- constructing the battlefield;
- selecting a discovered action by a stable identifier.

Helpers should not conceal the lifecycle.

The main test should still visibly show:

discover

→ execute

→ advance

→ complete

→ inspect consequences

A reader should be able to understand the encounter without opening a large test framework.

---

Conditional workflow — What happens if the lifecycle test exposes a gap?

This is the most important procedural safeguard.

If the test passes using current public APIs

Complete PR 2 as a tests-only pull request.

That is the ideal outcome.

If the test exposes a genuine production gap

Do not casually add production changes to the test branch.

Instead:

1. identify the exact blocker;

2. describe the smallest missing contract;

3. preserve the incomplete lifecycle test locally or as a temporary patch;

4. return to clean, updated main;

5. create a narrowly named feature branch;

6. add a focused unit or integration test for that specific gap;

7. implement the smallest correction;

8. merge that corrective PR;

9. recreate or update the lifecycle-test branch from the new main;

10. finish the lifecycle proof.

This keeps each PR coherent.

---

### Examples of legitimate corrective branches

The actual branch name must describe the discovered defect.

Possible examples:

feature/expose-encounter-winning-side

feature/preserve-encounter-participant-identity

feature/expose-final-ammunition-state

feature/complete-encounter-action-discovery

feature/expose-encounter-completion-reason

These would be legitimate only if the lifecycle test proves that the information is unavailable or unusable.

---

### Examples of changes that should be rejected during this workflow

Suppose the lifecycle test reveals that:

- enemies do not choose their own actions;
- rewards are not granted;
- no campaign clock advances;
- combat cannot return to an exploration cell.

Those are not combat-runtime defects in this phase.

They belong later in:

- the basic enemy policy;
- the application layer;
- encounter consequence processing;
- exploration-to-combat transitions.

We should not enlarge the encounter engine to own those responsibilities.

---

### Final phase review — Declare the combat boundary

After PR 2 and any necessary corrective PR are merged, perform a short architectural review.

No new feature branch should be opened until we can state the boundary clearly.

### Inputs the future application layer supplies

The application layer should provide:

- stable participant identities;
- starting mutable participant state;
- combat-ready profiles;
- sides;
- positions;
- battlefield definition;
- encounter-specific configuration.

### Responsibilities the encounter runtime owns

The encounter runtime should own:

- initiative;
- turns;
- action legality;
- movement;
- attacks;
- saving throws during combat;
- damage and healing;
- death saves;
- battlefield state;
- completion determination.

### Outputs available after combat

The application layer must be able to retrieve:

- completion status;
- winning side;
- final participant state;
- persistent resource expenditure;
- participant identity mapping.

### Responsibilities explicitly outside combat

The encounter runtime must not decide:

- treasure;
- experience;
- campaign flags;
- quest progression;
- campaign time;
- where the party resumes;
- whether a location changes state;
- narrative consequences;
- save-game behavior.

This review should produce a clear internal conclusion, not a large speculative design document.

---

### Phase-close decision

Once the lifecycle proof passes, broad combat development pauses.

New combat work will then require one of these justifications:

1. the walking skeleton cannot function without it;

2. the first vertical slice contains content that requires it;

3. Godot integration reveals that an existing result is insufficient for presentation;

4. a correctness defect exists in already supported behavior.

“5e contains this rule” will no longer be sufficient by itself.

---

### Expected deliverables

At the end of this workflow, main should contain:

- cover-aware encounter Dexterity saving throws;
- focused integration tests for cover eligibility;
- one complete deterministic encounter lifecycle test;
- any narrowly necessary handoff correction;
- no campaign, travel, exploration, console, Godot, or save/load code.

The repository should conclusively demonstrate:

### External caller

→ creates encounter

→ discovers and executes legal actions

→ advances through turns

→ reaches completion

→ reads persistent consequences

---

### Expected branch count

Best case:

1. feature/apply-cover-to-dexterity-saves

2. test/prove-encounter-lifecycle

Acceptable case:

1. feature/apply-cover-to-dexterity-saves

2. narrowly named corrective branch

3. test/prove-encounter-lifecycle

Anything beyond that suggests the scope is expanding or the encounter boundary is less mature than expected. At that point, we should stop and reassess rather than accumulating opportunistic fixes.

---

### First action when implementation begins

The first actual implementation step should be a read-only inventory of the current saving-throw architecture.

We should inspect:

- saving-throw rule and result types;
- encounter APIs that currently invoke saving throws;
- situational modifier support;
- participant-to-character saving-throw data;
- related tests.

Only after seeing that code should we decide whether to extend an existing encounter path or introduce a narrow adapter.

That prevents us from starting the next branch with a guessed architecture rather than the architecture already present.
