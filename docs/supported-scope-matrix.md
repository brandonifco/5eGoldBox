<!-- Converted from '5e Gold Box Supported Scope Matrix.docx'. The .docx
     remains the authored original; this is the tracked, greppable copy. -->

# 5e Gold Box Supported Scope Matrix

### Product, Technical Vertical Slice, and First Campaign Scope

Status: Accepted baseline
Rules baseline: 2014 5e unless deliberately revised and versioned
Primary owner: Solo developer
Technology: C#/.NET 8 rules and runtime engine with a Godot presentation client

## 1. Purpose

This document defines what the project is committed to support, what is intentionally deferred, and what is excluded from the initial product.

Its purpose is to prevent uncontrolled scope growth and ensure that development produces a complete playable game rather than a growing collection of disconnected rules and systems.

The project will ship a complete, coherent subset of 5e rather than claim broad support that is only partially implemented. Every selectable option must function through legal gameplay, authoritative state, persistence, user interface, AI where applicable, and appropriate tests.

## 2. Scope Status Definitions

| Status | Meaning |
|---|---|
| Committed | Required for the stated milestone. The milestone is incomplete until the capability works and meets its definition of done. |
| Stretch | Useful but not required. It may be cut without destabilizing committed work. |
| Deferred | Intentionally postponed to a later milestone. It must not appear as misleading selectable content. |
| Excluded | Outside the intended product direction or unjustified by its cost. |
| Pending catalog | The capability is committed, but the exact list of supported content must be enumerated before dependent content production begins. |

A feature is included only when it is necessary to complete the selected game experience or deliberately proves a foundational engine capability.

Familiarity, popularity, iconic status, and possible future usefulness are not sufficient reasons for inclusion.

## 3. Governing Product Decisions

| Area | Accepted decision |
|---|---|
| Rules edition | 2014 5e |
| Game type | Single-player, party-controlled, turn-based tactical CRPG |
| Primary inspiration | Classic Gold Box structure and pacing |
| Rules authority | Deterministic C#/.NET engine |
| Client responsibility | Godot presentation and input only |
| Campaign model | Authored, DM-less campaign with explicit possibilities and consequences |
| Initial product strategy | Narrow, complete supported subset |
| Core development principle | Production-quality engine core with replaceable prototype presentation and scenario content |
| Legal content policy | Mechanics may be implemented for private development; released content must be original or legally distributable |
| Multiplayer | Excluded |
| Real-time combat | Excluded |
| General natural-language DM | Excluded |
| Unlimited procedural campaign generation | Excluded |

The engine, game client, and campaign are separate products within the repository: the engine owns rules and state, Godot presents and submits commands, and the campaign owns authored maps, dialogue, encounters, and consequences.

## 4. Development Platform Scope

| Area | Technical slice | First campaign |
|---|---|---|
| Primary playable build | Windows | Windows |
| Secondary compatibility target | Linux | Linux |
| Development environments | Windows and Linux | Windows and Linux |
| CI expectation | Engine and tests on both where practical | Required supported-platform checks |
| Keyboard and mouse | Committed | Committed |
| Controller | Deferred | Separate later milestone |
| macOS | Deferred | Deferred |
| Consoles | Excluded from initial product |  |
| Mobile | Excluded |  |

Source, content, save files, file paths, and engine behavior must remain cross-platform even while Windows is the primary manually tested build.

## 5. Technical Vertical Slice

### 5.1 Objective

The first technical vertical slice is a private internal proof, not a public demo or production-quality opening chapter.

It must prove this complete loop:

First-person dungeon exploration → authored interaction → tactical combat → reward and persistent consequence → return to exploration → completion → save and reload

The slice uses durable production architecture but placeholder visual presentation and disposable test content.

### 5.2 Technical-Slice Character Scope

| Category | Committed scope |
|---|---|
| Character level | Level 1 only |
| Party size | Four active characters |
| Character creation UI | None |
| Party construction | Four predefined legal characters resolved through the real character pipeline |
| Advancement | Not included |
| Multiclassing | Not included |
| Feats | Not included |
| Ability generation | Predefined |
| Player combat control | Player controls all four characters |

#### Fixed party

| Character | Class | Species | Background | Specialization |
|---|---|---|---|---|
| Fighter | Fighter 1 | Human | Soldier | Fixed level-one build |
| Rogue | Rogue 1 | Lightfoot halfling | Criminal | Fixed level-one build |
| Cleric | Cleric 1 | Hill dwarf | Acolyte | Life Domain |
| Wizard | Wizard 1 | High elf | Sage | Fixed level-one build |

Only features required by these specific builds are committed for the technical slice. Their presence does not imply complete support for every class, species, background, or alternate build.

### 5.3 Technical-Slice Spell Scope

The slice supports exactly these six spells:

| Spell | Primary system proven |
|---|---|
| Fire Bolt | Ranged spell attack |
| Sacred Flame | Saving-throw damage |
| Cure Wounds | Touch-range healing |
| Healing Word | Ranged bonus-action healing |
| Magic Missile | Automatic hit and multiple damage instances |
| Bless | Concentration, multiple targets, ongoing roll modifier |

All other spells are deferred.

### 5.4 Technical-Slice Exploration Scope

| Feature | Status and boundary |
|---|---|
| Exploration style | Minimal first-person cardinal-grid exploration |
| View | Classic 2D pseudo-3D rendering |
| Dungeon size | Approximately 12–20 traversable cells |
| Party location | One shared party cell and facing |
| Facing | North, east, south, west |
| Movement | Turn left, turn right, move forward |
| Doors | Simple open and closed state |
| Automap | Discovered cells, walls, doors, position, and facing |
| Map floors | One floor |
| Authored interaction | One interaction with a skill check |
| Combat trigger | One fixed authored trigger |
| Game time | Only substantial actions advance time |
| Lighting | Deferred |
| Darkvision effects | Deferred |
| Searching | Deferred |
| Traps | Deferred |
| Secret doors | Deferred |
| Locks and keys | Deferred |
| Wandering encounters | Deferred |
| Party splitting | Excluded from slice |
| Overland travel | Deferred |
| Boat travel | Deferred |

The exploration position is authoritative application or runtime state. Godot camera state, sprites, and rendered wall segments do not define party location.

### 5.5 Technical-Slice Authored Interaction

The slice includes one noncombat interaction with:

- a stable interaction ID;

- one or more explicit authored approaches;

- one ability or skill check;

- defined success and failure results;

- persistent campaign-state consequences;

- a modest but visible effect on the upcoming encounter.

The default consequence is advance warning. Success reveals useful encounter information and prevents an authored surprise disadvantage. It does not remove enemies or require two fundamentally different encounter definitions.

The slice also includes:

- one required quest objective;

- one optional objective or favorable outcome;

- journal-state changes;

- a persistent completion result.

### 5.6 Technical-Slice Tactical Combat Scope

| Feature | Committed scope |
|---|---|
| Grid | Five-foot square grid |
| Approximate map size | 12×12 squares |
| Creature size | Small and Medium only |
| Occupancy | One square per creature |
| Diagonal movement | Five feet |
| Combat facing | Not used |
| Elevation | Deferred |
| Flight | Deferred |
| Squeezing | Deferred |
| Large creatures | Deferred |
| Movement control | Select destination, preview proposed path, confirm |
| Difficult terrain | One small authored area |
| Line of sight | Supported |
| Cover | Deferred |
| Terrain visibility | Entire terrain shown |
| Enemy visibility | Enemies shown only when in line of sight |
| Tactical map source | Separate authored combat map |
| Party deployment | Fixed authored formation |
| Initiative | Individual initiative |
| Tie-breaking | Initiative total, Dexterity modifier, Dexterity score, stable combatant ID |
| Surprise | Basic authored surprise state |
| Retreat | Not supported in slice |
| Victory | All enemies defeated or incapacitated |
| Defeat | Party can no longer continue |

#### Supported actions

- Move

- Attack

- Cast a Spell

- Dash

- Disengage

- Dodge

- Supported bonus actions

- Opportunity attack

- End Turn

#### Deferred actions

- Ready

- Grapple

- Shove

- Two-weapon fighting

- Generalized Help

- Improvised weapons

- Generalized object interactions

- Mounted actions

- Broad environmental improvisation

#### Conditions and health states

| Type | Slice scope |
|---|---|
| Reusable combat condition | Prone |
| Health/lifecycle states | Conscious, unconscious, dying, stable, dead |
| Player-character death saves | Full basic 5e death-save process |
| Ordinary enemy death handling | Defeated immediately at 0 HP |
| General enemy death policy | Content-controlled for future use |

#### Reactions

Opportunity attacks are the only committed reaction.

Defensive post-roll reactions, counterspells, readied reactions, and broad reaction selection are deferred.

### 5.7 Technical-Slice Enemy Scope

The encounter contains three original humanoid enemies:

- two melee raiders;

- one ranged raider.

They use straightforward weapon attacks and no unique monster subsystem.

The two AI roles must behave differently:

| AI role | Required behavior |
|---|---|
| Melee raider | Close distance, select a legal target, make melee attacks |
| Ranged raider | Seek useful range, maintain distance where practical, make ranged attacks |

AI must use engine-provided legal commands and must not mutate state directly.

### 5.8 Technical-Slice Inventory and Reward Scope

#### Inventory

- equipped weapons;

- armor;

- shields where applicable;

- per-character carried-item lists;

- safe-time item transfers;

- safe-time equip and unequip;

- shared party currency;

- one usable healing potion.

#### Reward

Winning grants:

- a fixed amount of gold;

- one healing potion;

- quest and campaign-state progress.

#### Deferred

- encumbrance;

- item durability;

- crafting;

- repairs;

- complex containers;

- world-item dropping;

- ammunition tracking;

- extensive item stacking;

- identification systems.

### 5.9 Technical-Slice Rest and Recovery

| Feature | Status |
|---|---|
| Short rest | Not supported |
| Long rest | Not supported |
| Starting resources | Full |
| Healing | Supported spells and healing potion |
| Defeat outcome | Defeat screen |
| Defeat recovery | Load most recent save or restart slice |

### 5.10 Technical-Slice Save Scope

Saving is allowed:

- outside combat;

- after a completed turn boundary;

- after combat;

- after the authored interaction.

Saving is not supported:

- during a partially resolved command;

- during a pending reaction;

- during an unresolved player decision;

- during an animation;

- during an uncommitted transition.

Save compatibility is preserved within the active milestone. Major milestone boundaries may intentionally invalidate private development saves, but every save must include versions and fail clearly when incompatible.

### 5.11 Technical-Slice Presentation

| Area | Requirement |
|---|---|
| Visual quality | Functional placeholders |
| Dungeon art | Reusable wall, floor, corridor, and door images |
| Tactical characters | Basic tokens |
| Animation | Minimal |
| Audio | Optional; not completion-critical |
| Music | Not required |
| Voice acting | Excluded |
| Combat log | Explainable player-facing log |
| Developer diagnostics | Separate deeper trace |
| Accessibility | Architecture must permit later support; full pass is not slice-blocking |

The explainable combat log shows rolls, modifiers, target values, damage, resource use, and resulting state without exposing unnecessary internal implementation detail.

### 5.12 Technical-Slice Content Authoring

A hybrid development approach is permitted:

- typed C# fixtures may be used temporarily while systems are developed;

- before milestone completion, all playable content must load from validated external files.

External content must use:

- stable namespace-qualified IDs;

- explicit schemas;

- validated references;

- version fields;

- source and license metadata;

- typed executable behaviors;

- deterministic loading.

Content must describe facts and parameters. It must not hide executable mechanics in prose or opaque strings.

## 6. First Complete Campaign

### 6.1 Campaign Product Target

| Area | Accepted scope |
|---|---|
| Estimated length | Approximately 10–15 hours |
| Level range | Levels 1–5 |
| Structure | Hub-and-spoke open campaign |
| Active party size | Four |
| Reserve roster | Up to four additional characters |
| World hub | One central town or base |
| Major adventure locations | Four |
| Compact optional sites | Two |
| Ending | One main ending with meaningful epilogue variations |
| Quest structure | Mostly convergent branching |
| XP system | Traditional shared party XP |
| Difficulty settings | Story, Standard, Veteran |

### 6.2 Campaign Character Creation

Players create all four active party members from a deliberately limited supported catalog.

Every offered option must function completely through level 5.

#### Supported classes

- Fighter

- Rogue

- Cleric

- Wizard

Duplicate classes are allowed.

#### Supported subclasses

| Class | Subclasses |
|---|---|
| Fighter | Champion, Battle Master |
| Rogue | Thief, Arcane Trickster |
| Cleric | Life, War |
| Wizard | Evocation, Abjuration |

#### Supported species

- Human

- Dwarf

- Elf

- Halfling

#### Supported variants

| Species | Options |
|---|---|
| Human | Standard human |
| Dwarf | Hill dwarf, mountain dwarf |
| Elf | High elf, wood elf |
| Halfling | Lightfoot halfling, stout halfling |

Variant human is deferred because it requires a meaningful level-one feat catalog.

#### Backgrounds

Initial supported backgrounds:

- Soldier

- Criminal

- Acolyte

- Sage

Additional backgrounds may be added only through explicit scope review and full mechanical integration.

#### Ability generation

- standard array;

- point buy.

Random ability rolling is deferred.

#### Advancement rules

- traditional shared party XP;

- no individual kill-credit XP;

- level cap of 5;

- no multiclassing;

- Ability Score Improvement only at level 4;

- feats deferred.

### 6.3 Campaign XP Policy

XP may be awarded for:

- combat victories;

- quests;

- discoveries;

- negotiation;

- avoidance of threats;

- other meaningful accomplishments.

Policy requirements:

- noncombat resolution should usually provide XP comparable to defeating the same obstacle;

- most authored encounters are finite;

- repeatable encounters must not provide efficient unlimited XP;

- the critical path provides enough XP to complete the campaign;

- optional content allows earlier advancement and improved preparation;

- enemies do not automatically scale to the party;

- adventure locations have authored difficulty bands;

- dangerous content must be signaled;

- players may enter difficult locations early and retreat.

### 6.4 Campaign Spell Scope

The first campaign uses a curated spell catalog, not every cleric and wizard spell through third level.

The exact catalog is a required companion document before class and campaign content production scales.

Every selectable spell must be:

- fully executable;

- correctly targeted;

- integrated with action economy and resources;

- persisted;

- available to AI where appropriate;

- represented accurately in the UI;

- tested in normal and important edge cases.

The spell catalog may be expanded only after the full campaign works end to end.

### 6.5 Campaign Exploration Modes

#### First-person exploration

Used for:

- towns;

- dungeons;

- local interiors;

- other grid-based adventure sites.

The town uses first-person streets with simplified service interfaces rather than fully explorable interiors for every building.

Story-critical interiors may use dedicated first-person maps.

#### Tactical combat

Combat expands into a separate bird’s-eye tactical map.

The finished campaign supports:

- authored encounter maps;

- tactical retreat through designated exits;

- persistent encounter consequences;

- combat outcomes including victory, defeat, and retreat.

#### World travel

Overland travel uses a route-based world map.

The player chooses among known destinations and routes. Routes may differ by:

- travel time;

- safety;

- terrain;

- faction control;

- required discoveries;

- unlocked shortcuts;

- required vehicle or vessel.

#### Boat travel

Boat travel uses the same route framework with:

- ports;

- vessels;

- water-only routes;

- sea hazards;

- sea encounter tables;

- travel time.

Free ship movement across water tiles is deferred.

### 6.6 Campaign Travel Encounters

Overland and boat routes support bounded random encounters.

Requirements:

- each route has an explicit danger rating;

- encounter selection is deterministic under a fixed seed;

- encounter tables contain combat and noncombat events;

- dangerous routes are signaled;

- some events are one-time or state-dependent;

- repeat encounters do not provide unlimited full XP;

- avoidance, negotiation, and retreat may be supported;

- event outcomes persist.

### 6.7 Town Scope

The central town functions as the expedition and party-management hub.

#### Committed services

- inn and resting;

- shops for basic equipment;

- buying and selling;

- temple healing;

- revival;

- quest or council location;

- recruitment and replacement;

- reserve roster management;

- item storage;

- identification of unusual items;

- spell preparation;

- character advancement;

- rumors;

- known-destination review.

#### Deferred services

- crafting;

- repairs;

- item durability;

- banking;

- property ownership;

- theft simulation;

- extensive NPC schedules;

- complex reputation-based pricing;

- multiple competing vendors for every category.

### 6.8 Campaign Rest Policy

The campaign uses hybrid resting.

#### Short rests

May be attempted in reasonably secure locations.

#### Long rests

Generally require:

- an inn;

- an established camp;

- a cleared refuge;

- another explicitly safe location.

Unsafe rest attempts may be:

- unavailable;

- interrupted;

- subject to authored risk;

- affected by time-sensitive campaign conditions.

The system must prevent unrestricted long-rest recovery after every encounter.

### 6.9 Campaign Equipment and Economy

The first campaign supports a curated practical equipment system.

#### Committed capabilities

- weapons;

- armor;

- shields;

- ammunition;

- consumables;

- selected unusual or magical items;

- buying;

- selling;

- equipping;

- unequipping;

- transferring;

- storing;

- identifying;

- carrying capacity where required by the selected policy.

#### Deferred capabilities

- durability;

- repairs;

- crafting;

- variable-quality equipment;

- extensive container simulation;

- deep merchant economics.

The exact supported equipment catalog must be enumerated before content alpha.

### 6.10 Party Death, Revival, and Replacement

#### Death policy

The campaign uses a hybrid system:

- dead characters may usually be revived through temple services;

- revival is costly or limited;

- some deaths may be irreversible;

- replacements remain available.

#### Replacement characters

Replacement characters:

- are created from the supported character catalog;

- join one level below the rounded average party level;

- receive basic functional equipment;

- can use stored party equipment and gold.

#### Reserve roster

- four active characters;

- up to four inactive reserve characters;

- roster changes occur in appropriate safe locations.

### 6.11 Campaign Combat Failure and Recovery

Party defeat may produce authored continuation when appropriate.

Possible outcomes include:

- capture;

- imprisonment;

- robbery;

- loss of supplies;

- displacement;

- rescue;

- faction consequences;

- quest changes.

Some encounters remain lethal and require loading a previous save.

The game is not required to invent a continuation state where survival would be implausible.

### 6.12 Campaign Quest and Ending Scope

#### Quest structure

Quests support:

- different approaches;

- combat and noncombat solutions;

- local consequences;

- reputation changes;

- reward differences;

- altered encounters;

- changed dialogue;

- optional objectives.

Most major paths reconverge to keep authoring and testing manageable.

#### Ending

The campaign has one primary final resolution.

The epilogue changes based on:

- optional objectives;

- faction relationships;

- surviving NPCs;

- major quest decisions;

- discovered information;

- prior failures and recoveries.

Multiple completely separate final campaigns are deferred.

### 6.13 Campaign Difficulty Modes

#### Story

May adjust:

- enemy composition;

- AI decision quality;

- recovery costs;

- resource pressure;

- encounter frequency.

#### Standard

Intended baseline experience.

#### Veteran

May increase:

- enemy composition difficulty;

- tactical AI quality;

- resource pressure;

- recovery cost;

- travel danger.

Difficulty settings must not:

- grant illegal enemy actions;

- bypass costs;

- reveal hidden information to AI;

- secretly change core rules;

- directly falsify player-visible calculations.

## 7. Release and Compatibility Scope

### Save compatibility

| Development phase | Policy |
|---|---|
| Inside one milestone | Preserve compatibility where practical |
| Major private milestone boundary | Compatibility may be intentionally broken |
| Public preview and later | Migrations or explicit supported incompatibility policy required |
| Stable release | Strong compatibility and migration expectations |

All saves must include:

- save-schema version;

- engine/build identity;

- rules version;

- campaign/content versions;

- stable IDs;

- compatibility diagnostics.

### Mod and customization policy

The first public release does not include official mod support.

External content files may remain readable and editable, but the project does not initially promise:

- stable public schemas;

- a mod loader;

- mod package discovery;

- compatibility guarantees;

- migration guarantees for unofficial content;

- authoring tools;

- mod documentation;

- security isolation;

- technical support for modifications.

Official mod support requires a separate approved milestone.

### Legal distribution policy

Released content must be:

- original;

- openly licensed;

- separately licensed;

- or otherwise legally distributable.

Ownership of a rulebook is not treated as permission to reproduce its protected text or content.

Source, license, attribution, version, and modification data must be tracked for distributed content and assets.

## 8. Major Deferred Capabilities

The following are intentionally deferred beyond the first complete campaign unless separately approved:

- levels above 5;

- classes beyond fighter, rogue, cleric, and wizard;

- more than two subclasses per supported class;

- multiclassing;

- feats;

- variant human;

- full spell-list coverage;

- all core species;

- broad condition coverage not needed by campaign content;

- unrestricted improvised actions;

- complete grapple, shove, and Ready support if not required earlier;

- full lighting and darkness simulation;

- advanced stealth;

- secret-door and trap systems beyond campaign needs;

- free-grid wilderness travel;

- free-moving boat navigation;

- mounted combat;

- flying tactical combat;

- complex elevation;

- broad crafting;

- durability and repairs;

- formal public modding;

- controller support;

- macOS release;

- multiplayer;

- live service features;

- procedural campaign replacement for authored content.

Deferred does not mean rejected. It means the capability must not delay the current committed milestones.

## 9. Definition of Supported

A feature is supported only when:

- it can be legally selected or invoked;

- its prerequisites and invalid cases are modeled;

- required choices are exposed;

- timing and resources are correct;

- it changes authoritative state;

- it works through the real command pipeline;

- it persists where required;

- it appears accurately in UI and logs;

- AI can use or respond to it where appropriate;

- content validation prevents unsupported use;

- tests cover its supported behavior;

- known limitations are recorded.

A class name, content field, test file, or isolated calculation is not proof that the feature is supported. The rules coverage ledger must distinguish not modeled, data only, calculation primitive, partially integrated, and fully integrated behavior.

## 10. Definition of Technical-Slice Completion

The technical slice is complete only when:

- the four fixed characters load through the real character pipeline;

- first-person dungeon navigation works;

- position, facing, doors, and automap discovery persist;

- the authored interaction resolves and changes state;

- tactical combat starts from the authored trigger;

- all seven combatants receive stable identities;

- legal actions are queryable;

- movement and path previews are validated;

- AI enemies use legal commands;

- the encounter reaches victory or defeat;

- death saves work for player characters;

- reward and quest state are applied;

- the party returns to exploration after victory;

- saving and loading work at declared boundaries;

- the same state and random inputs reproduce the same outcomes;

- all playable content loads from validated external files;

- Godot contains no authoritative rules logic;

- the explainable combat log matches actual results;

- Release build and full tests pass;

- the rules coverage ledger reflects actual support.

## 11. Definition of First-Campaign Completion

The first campaign is complete only when:

- a new player can create a legal four-character party;

- every offered class, subclass, species, background, spell, item, and feature works through level 5;

- every advertised location can be entered and completed;

- the campaign can be finished through every required route;

- retreat, rest, defeat, recovery, revival, and replacement paths work;

- optional quests and side locations have complete outcomes;

- all three difficulty modes work;

- the main ending and epilogue variations resolve correctly;

- saves migrate or fail safely according to release policy;

- content validates completely;

- no required mechanic exists only as a label, TODO, placeholder, or developer instruction;

- Windows release packaging works;

- Linux compatibility has been verified;

- automated tests, scenario tests, save/load tests, and smoke tests pass;

- all known adaptations and exclusions are documented.

A complete release must cover the entire advertised product, not merely the critical path.

## 12. Scope Change Rule

No new feature enters committed scope merely because it is desirable.

Every proposed addition must state:

- player value;

- affected milestone;

- displaced priority;

- rules dependencies;

- content dependencies;

- architectural impact;

- persistence impact;

- UI impact;

- AI impact;

- test burden;

- estimated development cost;

- reason it cannot wait.

A proposed feature is accepted only when:

- an existing priority is removed or delayed;

- additional development capacity becomes available;

- or the milestone is explicitly expanded.

Small low-impact implementation choices may be made by the developer or engineering assistant without reopening this document. Decisions affecting long-term architecture, progression, player experience, campaign structure, or substantial workload require explicit approval.

## 13. Immediate Implementation Priority

The current project priority is not broader 5e coverage.

The next major delivery target is:

Produce a deterministic headless encounter that starts, exposes legal player and AI commands, reaches a declared outcome, and can be saved and restored.

The recommended order is:

- finish the current CharacterResolver modularization;

- establish runtime combatant state;

- establish encounter state and lifecycle;

- implement legal-action discovery;

- implement movement and attacks;

- implement turn advancement and completion;

- add basic enemy AI;

- add encounter save/load;

- load the technical-slice content externally;

- add the thin Godot client;

- implement first-person dungeon exploration;

- connect the complete vertical-slice loop.

No additional broad rule subsystem should interrupt this sequence unless the technical slice directly requires it.
