# 5eGoldBox

A from-scratch C#/.NET 8 implementation of Dungeons & Dragons 5th edition (2014 ruleset) combat and character mechanics, wrapped in a Gold Box–inspired turn-based CRPG.

> **Project status: engine vertical slice, not a finished game.**
> One complete, deterministic, save/loadable scenario ("Watchtower") plays end to end through a text console client. The engine is not yet scenario-agnostic, and the Godot UI is an unwired shell. See [Roadmap](#roadmap) for what that means and what's next.

## What works today

A full playthrough loop, covered by ~1,780 tests:

1. **Party** — a fixed three-member party (Fighter, Barbarian, Ranger) with real 5e character resolution: ability scores, proficiencies, hit points, equipment, currency, carrying capacity.
2. **Outpost** — accept or decline the Watchtower mission.
3. **Regional travel** — a step-indexed, resumable route to the watchtower.
4. **Exploration** — grid movement across floors with facing and stairs, up to a signal mechanism.
5. **Encounter** — turn-based tactical combat: movement and pathfinding, weapon attacks, ammunition, cover, line of sight, death saving throws, enemy AI, and win/loss resolution.
6. **Conclusion** — scenario wrap-up (defeat path is the validated one today).
7. **Save / load** — a versioned JSON format with validated, tamper-resistant loading and atomic writes.

## Architecture

```
FiveEGoldBox.Core         Pure 5e rules engine. No randomness, no I/O, no orchestration.
      ↑
FiveEGoldBox.Application  Session lifecycle, scenario orchestration, combat, persistence,
                          deterministic randomness.
      ↑
FiveEGoldBox.Console      Text reference client. The playable surface today.

FiveEGoldBox.Godot        Early UI shell. Separate solution, not yet wired to the engine.
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

```bash
dotnet run --project src/FiveEGoldBox.Console
```

Numbered menus throughout; save and load are available from the main menu.

## Roadmap

| Document | Purpose |
|---|---|
| [docs/priority-1-development-plan.md](docs/priority-1-development-plan.md) | Phases 1–8: public API stabilization, versioned persistence, session validation, combat decomposition. All eight complete. |
| [CLAUDE.md](CLAUDE.md) | Working conventions, current phase status, and repo hygiene notes — the authoritative source for what's actually in progress now (Priority 2, Phases 9–12), since `docs/priority-2-development-plan.md` was removed. |

Phases 1–8 are done: the engine is loadable with any scenario, proven by a second one (the Sunken Chapel) that shares nothing with the first. See CLAUDE.md for what's in progress now.

## License

Proprietary — all rights reserved. See [LICENSE](LICENSE).

Dungeons & Dragons and D&D are trademarks of Wizards of the Coast LLC, which is not affiliated with and does not endorse this project.
