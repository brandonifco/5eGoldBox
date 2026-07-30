# 5eGoldBox

A from-scratch C#/.NET 8 implementation of Dungeons & Dragons 5th edition (2014 ruleset) combat and character mechanics, wrapped in a Gold Box–inspired turn-based CRPG.

> **Project status: engine and both clients are playable, production polish is ongoing.**
> Three deterministic, save/loadable scenarios ("Watchtower," "The Sunken Chapel," "The Hollow Mill") play end to end — the engine is scenario-agnostic, proven by authoring a second and third scenario that share no code with the first. Two real clients exist: a text console client, and a Godot desktop UI wired to the same backend (outpost decisions, exploration, regional travel, tactical combat, spellcasting, and a real top-down area map). See [Roadmap](#roadmap) for what's still open.

## What works today

A full playthrough loop, covered by 2,105 tests:

1. **Party** — a four-member active roster (Fighter, Rogue, Cleric, Wizard — Barbarian and Ranger held in reserve) with real 5e character resolution: ability scores, proficiencies, hit points, equipment, currency, carrying capacity.
2. **Outpost** — an N-way scenario-declared decision (not a hardcoded accept/decline), resolved by content-authored option ID.
3. **Regional travel** — a step-indexed, resumable route to the destination, with support for more than one route out of a location.
4. **Exploration** — grid movement across floors with facing and stairs, up to a scenario-declared trigger; a real top-down area map (Godot) shows the floor's walkable cells, stairs, and the party's live position/facing, navigable while open.
5. **Encounter** — turn-based tactical combat: movement and pathfinding, weapon attacks, single-target spellcasting (attack-roll, saving-throw, and automatic resolution), ammunition, cover, line of sight, concentration checks, death saving throws, enemy AI, and win/loss resolution.
6. **Conclusion** — scenario wrap-up, both victory and defeat paths.
7. **Save / load** — a versioned JSON format with validated, tamper-resistant loading and atomic writes.

## Architecture

```
FiveEGoldBox.Core         Pure 5e rules engine. No randomness, no I/O, no orchestration.
      ↑
FiveEGoldBox.Application  Session lifecycle, scenario orchestration, combat, persistence,
                          deterministic randomness.
      ↑
FiveEGoldBox.Console      Text reference client, wired to the real engine end to end.

FiveEGoldBox.Godot        Godot 4 desktop client, wired to the real engine end to end.
                          Separate solution, not part of the main build/CI (see below).
```

Two design decisions shape most of the codebase:

- **Core is pure.** Every rule function takes already-rolled dice values as parameters. `Core` contains no RNG at all, which is what makes the rules trivially testable without mocks.
- **Randomness is deterministic and replayable.** Rolls derive from `(seed, cursor, sides)` via SHA-256 with unbiased rejection sampling. The cursor is part of session state, so a save file reproduces the exact same sequence on reload.

Domain state is immutable throughout — `sealed record` with `required`/`init` properties — and live encounter state is revision-tracked for optimistic concurrency.

## Build and test

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download).

```bash
dotnet build 5eGoldBox.sln -c Debug
```

```bash
dotnet test 5eGoldBox.sln -c Debug
```

```bash
dotnet build 5eGoldBox.sln -c Release
```

Zero warnings are expected in both configurations — `Directory.Build.props` sets `TreatWarningsAsErrors`, `EnforceCodeStyleInBuild`, and `AnalysisLevel=latest` solution-wide. CI runs build and test across a Debug/Release matrix and fails on any NuGet security advisory.

`FiveEGoldBox.Godot` has its own solution and is intentionally excluded from the commands above.

## Play it

Text console client:

```bash
dotnet run --project src/FiveEGoldBox.Console
```

Numbered menus throughout; save and load are available from the main menu.

Godot desktop client (requires the [Godot 4 .NET/Mono editor](https://godotengine.org/download)):

```bash
godot --path src/FiveEGoldBox.Godot
```

Boots straight into a real session (the Watchtower scenario by default) — no `-e`/`--editor` flag, which opens the editor instead of running the game. First run on a fresh checkout needs `godot --headless --path src/FiveEGoldBox.Godot --import` once to import assets.

## Roadmap

| Document | Purpose |
|---|---|
| [docs/priority-1-development-plan.md](docs/priority-1-development-plan.md) | Phases 1–8: public API stabilization, versioned persistence, session validation, combat decomposition. All eight complete. |
| [CLAUDE.md](CLAUDE.md) | Working conventions and current status — the authoritative source for what's actually in progress now, since `docs/priority-2-development-plan.md` was removed. |
| [docs/2026-07-30-codebase-reevaluation-and-development-plan.md](docs/2026-07-30-codebase-reevaluation-and-development-plan.md) | A from-scratch, independently-verified audit of the whole codebase (form, function, economy, modularity) and the phased plan it produced — documentation accuracy, a rules-correctness question, CI coverage for the Godot client, and several architectural decisions still awaiting a call. |

Phases 1–8 are done: the engine is loadable with any scenario, proven by two more (the Sunken Chapel, the Hollow Mill) that share no code with the first. See CLAUDE.md for what's in progress now.

## License

Proprietary — all rights reserved. See [LICENSE](LICENSE).

Dungeons & Dragons and D&D are trademarks of Wizards of the Coast LLC, which is not affiliated with and does not endorse this project.
