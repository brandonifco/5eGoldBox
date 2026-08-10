using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Combat;

/// Resolves a single player command against an encounter. Validates the command
/// itself, delegates the mechanics to Core, and reports the resulting encounter
/// state, the random cursor, the step describing what happened, and a receipt.
///
/// It never touches session state and never runs automatic processing — the
/// orchestrator owns both. Every rejection happens before any dice are drawn,
/// so a refused command leaves the caller's cursor untouched.
internal static class EncounterPlayerCommandResolver
{
    internal static EncounterPlayerCommandResolution Resolve(
        EncounterState encounter,
        int randomSeed,
        int cursorBefore,
        CombatMoveIntent intent)
    {
        ArgumentNullException.ThrowIfNull(encounter);
        ArgumentNullException.ThrowIfNull(intent);
        ArgumentNullException.ThrowIfNull(intent.Path);

        GridPosition[] path = intent.Path.ToArray();

        if (path.Length == 0)
        {
            throw new ArgumentException(
                "A watchtower combat movement path must contain at least one position.",
                nameof(intent));
        }

        EncounterMovementResult movement =
            EncounterMovementRules.Resolve(
                encounter,
                new EncounterMovementCommand
                {
                    ExpectedRevision = intent.ExpectedEncounterRevision,
                    ActorCombatantId = intent.ActorCombatantId,
                    Path = Array.AsReadOnly(path)
                });

        return new EncounterPlayerCommandResolution
        {
            State = movement.State,
            CursorAfter = cursorBefore,
            PrimaryStep = EncounterCombatStepFactory.CreateMovement(
                encounter,
                movement),
            Receipt = new EncounterCombatIntentReceipt
            {
                Kind = CombatIntentKind.Move,
                ExpectedEncounterRevision = intent.ExpectedEncounterRevision,
                ActorCombatantId = intent.ActorCombatantId,
                Path = Array.AsReadOnly(path),
                WeaponId = null,
                TargetCombatantId = null
            }
        };
    }

    internal static EncounterPlayerCommandResolution Resolve(
        EncounterState encounter,
        int randomSeed,
        int cursorBefore,
        CombatWeaponAttackIntent intent)
    {
        ArgumentNullException.ThrowIfNull(encounter);
        ArgumentNullException.ThrowIfNull(intent);
        ValidateRequiredId(intent.WeaponId, nameof(intent.WeaponId));
        ValidateRequiredId(intent.TargetCombatantId, nameof(intent.TargetCombatantId));

        EncounterCombatAttackAvailability prerequisites =
            EncounterCombatAttackStaging.EvaluateAvailability(
                encounter,
                intent.ActorCombatantId,
                intent.TargetCombatantId,
                intent.WeaponId);

        if (!prerequisites.IsLegal)
        {
            throw new InvalidOperationException(
                $"The selected weapon attack is unavailable for reason '{prerequisites.UnavailabilityReason}'.");
        }

        EncounterCombatAttackExecution attack =
            EncounterCombatAttackStaging.Resolve(
                encounter,
                randomSeed,
                cursorBefore,
                intent.ActorCombatantId,
                intent.TargetCombatantId,
                intent.WeaponId);

        return new EncounterPlayerCommandResolution
        {
            State = attack.Result.State,
            CursorAfter = attack.CursorAfter,
            PrimaryStep = EncounterCombatStepFactory.CreateWeaponAttack(
                encounter,
                attack.Result,
                attack.Dice),
            Receipt = new EncounterCombatIntentReceipt
            {
                Kind = CombatIntentKind.WeaponAttack,
                ExpectedEncounterRevision = intent.ExpectedEncounterRevision,
                ActorCombatantId = intent.ActorCombatantId,
                Path = Array.Empty<GridPosition>(),
                WeaponId = intent.WeaponId,
                TargetCombatantId = intent.TargetCombatantId
            }
        };
    }

    internal static EncounterPlayerCommandResolution Resolve(
        EncounterState encounter,
        int randomSeed,
        int cursorBefore,
        CombatSpellAttackIntent intent)
    {
        ArgumentNullException.ThrowIfNull(encounter);
        ArgumentNullException.ThrowIfNull(intent);
        ValidateRequiredId(intent.SpellId, nameof(intent.SpellId));
        ValidateRequiredId(intent.TargetCombatantId, nameof(intent.TargetCombatantId));

        EncounterCombatSpellAttackAvailability prerequisites =
            EncounterCombatSpellAttackStaging.EvaluateAvailability(
                encounter,
                intent.ActorCombatantId,
                intent.TargetCombatantId,
                intent.SpellId);

        if (!prerequisites.IsLegal)
        {
            throw new InvalidOperationException(
                $"The selected spell attack is unavailable for reason '{prerequisites.UnavailabilityReason}'.");
        }

        EncounterCombatSpellAttackExecution spellAttack =
            EncounterCombatSpellAttackStaging.Resolve(
                encounter,
                randomSeed,
                cursorBefore,
                intent.ActorCombatantId,
                intent.TargetCombatantId,
                intent.SpellId,
                intent.AdditionalTargetCombatantIds);

        return new EncounterPlayerCommandResolution
        {
            State = spellAttack.Result.State,
            CursorAfter = spellAttack.CursorAfter,
            PrimaryStep = EncounterCombatStepFactory.CreateSpellAttack(
                encounter,
                spellAttack.Result,
                spellAttack.Dice),
            Receipt = new EncounterCombatIntentReceipt
            {
                Kind = CombatIntentKind.SpellAttack,
                ExpectedEncounterRevision = intent.ExpectedEncounterRevision,
                ActorCombatantId = intent.ActorCombatantId,
                Path = Array.Empty<GridPosition>(),
                WeaponId = null,
                SpellId = intent.SpellId,
                TargetCombatantId = intent.TargetCombatantId
            }
        };
    }

    internal static EncounterPlayerCommandResolution Resolve(
        EncounterState encounter,
        int randomSeed,
        int cursorBefore,
        CombatEndTurnIntent intent)
    {
        ArgumentNullException.ThrowIfNull(encounter);
        ArgumentNullException.ThrowIfNull(intent);

        EncounterTurnAdvancementResult turn =
            EncounterTurnAdvancementRules.Resolve(
                encounter,
                new EncounterTurnAdvancementCommand
                {
                    ExpectedRevision = encounter.Revision,
                    ActorCombatantId = intent.ActorCombatantId
                });

        return new EncounterPlayerCommandResolution
        {
            State = turn.State,
            CursorAfter = cursorBefore,
            PrimaryStep = EncounterCombatStepFactory.CreateTurnAdvanced(
                encounter,
                turn,
                EncounterCombatTurnAdvanceReason.PlayerEndTurn),
            Receipt = new EncounterCombatIntentReceipt
            {
                Kind = CombatIntentKind.EndTurn,
                ExpectedEncounterRevision = intent.ExpectedEncounterRevision,
                ActorCombatantId = intent.ActorCombatantId,
                Path = Array.Empty<GridPosition>(),
                WeaponId = null,
                TargetCombatantId = null
            }
        };
    }

    private static void ValidateRequiredId(
        string value,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "A combat identifier is required.",
                parameterName);
        }
    }
}
