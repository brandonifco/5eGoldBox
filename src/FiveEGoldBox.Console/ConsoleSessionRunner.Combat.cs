using FiveEGoldBox.Application.Combat;
using FiveEGoldBox.Application.Sessions;

namespace FiveEGoldBox.Console;

internal sealed partial class ConsoleSessionRunner
{
    private CombatSessionRunResult RunCombatSession(
        TextReader input,
        TextWriter output,
        ApplicationSessionState session)
    {
        CombatResolutionResult normalization =
            CombatOperations.AdvanceToDecision(session);

        RenderCombatResolution(output, normalization);

        session = normalization.State;
        CombatDecision decision =
            normalization.ResultingDecision;

        while (true)
        {
            if (decision.State
                == CombatDecisionState.CombatCompleted)
            {
                CombatOutcomeResult outcome =
                    CombatOutcomeRules.Finalize(session);

                RenderCombatOutcome(output, outcome);

                return new CombatSessionRunResult(
                    ExitRequested: false,
                    Session: outcome.State);
            }

            ValidatePlayerDecision(decision);
            RenderSessionSummary(output, session);
            RenderCombatDecision(output, decision);

            IReadOnlyList<CombatMenuOption> options =
                CreateCombatMenuOptions(decision);

            WriteCombatMenu(output, options);

            string? selection = input.ReadLine();

            if (selection is null)
            {
                return new CombatSessionRunResult(
                    ExitRequested: true,
                    Session: session);
            }

            int selectedIndex = ParseSelection(
                selection,
                options.Count);

            if (selectedIndex == 0)
            {
                output.WriteLine("Invalid selection.");
                continue;
            }

            CombatMenuOption selectedOption =
                options[selectedIndex - 1];

            switch (selectedOption.Action)
            {
                case CombatMenuAction.Move:
                    CombatMovementDestinationOption
                        movementDestination =
                            selectedOption.MovementDestination
                            ?? throw new InvalidOperationException(
                                "A movement menu option did not contain a movement destination.");

                    CombatResolutionResult moveResult =
                        CombatOperations.Execute(
                            session,
                            new CombatMoveIntent
                            {
                                ExpectedEncounterRevision =
                                    decision.EncounterRevision,
                                ActorCombatantId =
                                    decision.ActiveCombatantId!,
                                Path = movementDestination.Path
                            });

                    RenderCombatResolution(output, moveResult);
                    session = moveResult.State;
                    decision = moveResult.ResultingDecision;
                    break;
                case CombatMenuAction.WeaponAttack:
                    CombatWeaponAttackOption weaponAttack =
                        selectedOption.WeaponAttack
                        ?? throw new InvalidOperationException(
                            "A weapon-attack menu option did not contain a weapon option.");
                    CombatTargetOption target =
                        selectedOption.Target
                        ?? throw new InvalidOperationException(
                            "A weapon-attack menu option did not contain a target option.");

                    CombatResolutionResult attackResult =
                        CombatOperations.Execute(
                            session,
                            new CombatWeaponAttackIntent
                            {
                                ExpectedEncounterRevision =
                                    decision.EncounterRevision,
                                ActorCombatantId =
                                    decision.ActiveCombatantId!,
                                WeaponId = weaponAttack.WeaponId,
                                TargetCombatantId =
                                    target.TargetCombatantId
                            });

                    RenderCombatResolution(output, attackResult);
                    session = attackResult.State;
                    decision = attackResult.ResultingDecision;
                    break;
                case CombatMenuAction.SpellAttack:
                    CombatSpellAttackOption spellAttack =
                        selectedOption.SpellAttack
                        ?? throw new InvalidOperationException(
                            "A spell-attack menu option did not contain a spell option.");
                    CombatTargetOption spellTarget =
                        selectedOption.Target
                        ?? throw new InvalidOperationException(
                            "A spell-attack menu option did not contain a target option.");

                    CombatResolutionResult castResult =
                        CombatOperations.Execute(
                            session,
                            new CombatSpellAttackIntent
                            {
                                ExpectedEncounterRevision =
                                    decision.EncounterRevision,
                                ActorCombatantId =
                                    decision.ActiveCombatantId!,
                                SpellId = spellAttack.SpellId,
                                TargetCombatantId =
                                    spellTarget.TargetCombatantId
                            });

                    RenderCombatResolution(output, castResult);
                    session = castResult.State;
                    decision = castResult.ResultingDecision;
                    break;
                case CombatMenuAction.EndTurn:
                    CombatResolutionResult endTurnResult =
                        CombatOperations.Execute(
                            session,
                            new CombatEndTurnIntent
                            {
                                ExpectedEncounterRevision =
                                    decision.EncounterRevision,
                                ActorCombatantId =
                                    decision.ActiveCombatantId!
                            });

                    RenderCombatResolution(output, endTurnResult);
                    session = endTurnResult.State;
                    decision = endTurnResult.ResultingDecision;
                    break;
                case CombatMenuAction.InspectEncounter:
                    RenderEncounter(output, session);
                    break;
                case CombatMenuAction.Exit:
                    return new CombatSessionRunResult(
                        ExitRequested: true,
                        Session: session);
                default:
                    throw new InvalidOperationException(
                        "The selected combat operation is unsupported.");
            }
        }
    }

    internal void RenderCombatDecision(
        TextWriter output,
        CombatDecision decision)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(decision);

        ValidatePlayerDecision(decision);

        CombatMovementOption movement =
            decision.Movement!;
        CombatEndTurnOption endTurn =
            decision.EndTurn!;

        output.WriteLine();
        output.WriteLine("Combat Decision");
        output.WriteLine($"Decision State: {decision.State}");
        output.WriteLine(
            $"Encounter Revision: {decision.EncounterRevision}");
        output.WriteLine(
            $"Active Combatant ID: {decision.ActiveCombatantId}");

        if (decision.PendingDeathSavingThrowCombatantId is not null)
        {
            output.WriteLine(
                $"Pending Death Saving Throw Combatant ID: {decision.PendingDeathSavingThrowCombatantId}");
        }

        output.WriteLine(
            $"Movement Remaining Feet: {movement.MovementRemainingFeet}");
        output.WriteLine(
            $"Movement Available: {FormatBoolean(movement.IsAvailable)}");
        output.WriteLine(
            $"Movement Unavailability Reason: {movement.UnavailabilityReason}");
        output.WriteLine(
            $"Legal Movement Destination Count: {(movement.IsAvailable ? movement.DestinationOptions.Count : 0)}");

        // One block per weapon carried, rather than the single fixed weapon
        // this used to assume — a combatant with a bow and a dagger reports
        // both.
        for (int index = 0;
            index < decision.WeaponAttacks.Count;
            index++)
        {
            CombatWeaponAttackOption weaponAttack =
                decision.WeaponAttacks[index];

            output.WriteLine($"Weapon {index + 1} ID: {weaponAttack.WeaponId}");
            output.WriteLine(
                $"Weapon {index + 1} Attack Available: {FormatBoolean(weaponAttack.IsAvailable)}");
            output.WriteLine(
                $"Weapon {index + 1} Attack Unavailability Reason: {weaponAttack.UnavailabilityReason}");
            output.WriteLine(
                $"Weapon {index + 1} Legal Attack Target Count: {(weaponAttack.IsAvailable ? weaponAttack.Targets.Count(target => target.IsAvailable) : 0)}");
        }

        // One block per spell prepared, the same way weapons get one block
        // each rather than a single fixed slot.
        for (int index = 0;
            index < decision.SpellAttacks.Count;
            index++)
        {
            CombatSpellAttackOption spellAttack =
                decision.SpellAttacks[index];

            output.WriteLine($"Spell {index + 1} ID: {spellAttack.SpellId}");
            output.WriteLine(
                $"Spell {index + 1} Cast Available: {FormatBoolean(spellAttack.IsAvailable)}");
            output.WriteLine(
                $"Spell {index + 1} Cast Unavailability Reason: {spellAttack.UnavailabilityReason}");
            output.WriteLine(
                $"Spell {index + 1} Legal Cast Target Count: {(spellAttack.IsAvailable ? spellAttack.Targets.Count(target => target.IsAvailable) : 0)}");
        }

        output.WriteLine(
            $"End Turn Available: {FormatBoolean(endTurn.IsAvailable)}");
        output.WriteLine(
            $"End Turn Unavailability Reason: {endTurn.UnavailabilityReason}");
    }

    internal void RenderEncounter(
        TextWriter output,
        ApplicationSessionState session)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(session);

        var encounter = session.ActiveEncounter?.Encounter
            ?? throw new InvalidOperationException(
                "Encounter inspection requires active-encounter state.");

        output.WriteLine();
        output.WriteLine("Encounter Inspection");
        output.WriteLine($"Encounter ID: {encounter.EncounterId}");
        output.WriteLine($"Lifecycle: {encounter.LifecycleState}");
        output.WriteLine($"Revision: {encounter.Revision}");
        output.WriteLine(
            $"Active Combatant ID: {encounter.ActiveCombatantId}");
        output.WriteLine(
            $"Round Number: {encounter.TurnState.RoundNumber}");

        if (encounter.WinningSideId is not null)
        {
            output.WriteLine(
                $"Winning Side ID: {encounter.WinningSideId}");
        }

        for (int index = 0;
            index < encounter.Participants.Count;
            index++)
        {
            var participant = encounter.Participants[index];
            var combatant = participant.Combatant;
            var health = combatant.Health;
            var hitPoints = health.HitPoints;
            var deathSavingThrows = health.DeathSavingThrows;

            output.WriteLine($"Participant {index + 1}");
            output.WriteLine(
                $"Combatant ID: {combatant.CombatantId}");
            output.WriteLine($"Side ID: {participant.SideId}");
            output.WriteLine(
                $"Position X: {participant.Position.X}");
            output.WriteLine(
                $"Position Y: {participant.Position.Y}");
            output.WriteLine(
                $"Lifecycle State: {combatant.LifecycleState}");
            output.WriteLine(
                $"Hit Points: {hitPoints.CurrentHitPoints} / {hitPoints.MaximumHitPoints}");
            output.WriteLine(
                $"Temporary Hit Points: {hitPoints.TemporaryHitPoints}");
            output.WriteLine(
                $"Stable: {FormatBoolean(deathSavingThrows.IsStable)}");
            output.WriteLine(
                $"Instant Death: {FormatBoolean(health.IsInstantlyDead)}");
            output.WriteLine(
                $"Death Save Successes: {deathSavingThrows.SuccessCount}");
            output.WriteLine(
                $"Death Save Failures: {deathSavingThrows.FailureCount}");
        }
    }

    private static IReadOnlyList<CombatMenuOption>
        CreateCombatMenuOptions(
            CombatDecision decision)
    {
        ValidatePlayerDecision(decision);

        List<CombatMenuOption> options = new();
        CombatMovementOption movement =
            decision.Movement!;

        if (movement.IsAvailable)
        {
            foreach (CombatMovementDestinationOption destination
                in movement.DestinationOptions)
            {
                options.Add(
                    new CombatMenuOption(
                        $"Move to ({destination.Destination.X}, {destination.Destination.Y}) - {destination.MovementCostFeet} ft",
                        CombatMenuAction.Move,
                        MovementDestination: destination));
            }
        }

        // One menu row per (weapon, target) pair, across every weapon the
        // combatant carries — a bow and a dagger each offer their own row
        // rather than the second going unmentioned.
        foreach (CombatWeaponAttackOption weaponAttack
            in decision.WeaponAttacks)
        {
            if (!weaponAttack.IsAvailable)
            {
                continue;
            }

            foreach (CombatTargetOption target
                in weaponAttack.Targets.Where(target =>
                    target.IsAvailable))
            {
                options.Add(
                    new CombatMenuOption(
                        CreateWeaponAttackLabel(
                            weaponAttack,
                            target),
                        CombatMenuAction.WeaponAttack,
                        WeaponAttack: weaponAttack,
                        Target: target));
            }
        }

        // One menu row per (spell, target) pair, the same shape as weapons —
        // a spell that can land on three legal targets offers three rows.
        foreach (CombatSpellAttackOption spellAttack
            in decision.SpellAttacks)
        {
            if (!spellAttack.IsAvailable)
            {
                continue;
            }

            foreach (CombatTargetOption target
                in spellAttack.Targets.Where(target =>
                    target.IsAvailable))
            {
                options.Add(
                    new CombatMenuOption(
                        CreateSpellAttackLabel(
                            spellAttack,
                            target),
                        CombatMenuAction.SpellAttack,
                        SpellAttack: spellAttack,
                        Target: target));
            }
        }

        if (decision.EndTurn!.IsAvailable)
        {
            options.Add(
                new CombatMenuOption(
                    "End Turn",
                    CombatMenuAction.EndTurn));
        }

        options.Add(
            new CombatMenuOption(
                "Inspect Encounter",
                CombatMenuAction.InspectEncounter));
        options.Add(
            new CombatMenuOption(
                "Exit",
                CombatMenuAction.Exit));

        return options.AsReadOnly();
    }

    private static string CreateWeaponAttackLabel(
        CombatWeaponAttackOption weaponAttack,
        CombatTargetOption target)
    {
        string label =
            $"Attack {target.TargetCombatantId} with {weaponAttack.WeaponId}";

        if (target.DistanceFeet is not null
            && target.AttackRollMode is not null)
        {
            return $"{label} - {target.DistanceFeet} ft, {target.AttackRollMode}";
        }

        if (target.DistanceFeet is not null)
        {
            return $"{label} - {target.DistanceFeet} ft";
        }

        if (target.AttackRollMode is not null)
        {
            return $"{label} - {target.AttackRollMode}";
        }

        return label;
    }

    private static string CreateSpellAttackLabel(
        CombatSpellAttackOption spellAttack,
        CombatTargetOption target)
    {
        string label =
            $"Cast {spellAttack.SpellId} on {target.TargetCombatantId}";

        if (target.DistanceFeet is not null)
        {
            label += $" - {target.DistanceFeet} ft";
        }

        if (target.AttackRollMode is not null)
        {
            label += target.DistanceFeet is null
                ? $" - {target.AttackRollMode}"
                : $", {target.AttackRollMode}";
        }
        else if (target.SaveAbility is not null)
        {
            string saveClause =
                $"DC {target.SaveDc} {target.SaveAbility} save";

            label += target.DistanceFeet is null
                ? $" - {saveClause}"
                : $", {saveClause}";
        }

        return label;
    }

    private static void WriteCombatMenu(
        TextWriter output,
        IReadOnlyList<CombatMenuOption> options)
    {
        output.WriteLine();
        output.WriteLine("Combat Menu");

        for (int index = 0; index < options.Count; index++)
        {
            output.WriteLine(
                $"{index + 1}. {options[index].Label}");
        }

        output.Write("Selection: ");
    }

    private static void RenderCombatResolution(
        TextWriter output,
        CombatResolutionResult result)
    {
        output.WriteLine();
        output.WriteLine("Combat Resolution");
        output.WriteLine(
            $"Prior Encounter Revision: {result.PriorEncounterRevision}");
        output.WriteLine(
            $"Resulting Encounter Revision: {result.ResultingEncounterRevision}");
        output.WriteLine(
            $"Random Values Consumed Before: {result.RandomValuesConsumedBefore}");
        output.WriteLine(
            $"Random Values Consumed After: {result.RandomValuesConsumedAfter}");

        if (result.SubmittedIntent is not null)
        {
            output.WriteLine(
                $"Submitted Intent Kind: {result.SubmittedIntent.Kind}");
            output.WriteLine(
                $"Submitted Intent Actor ID: {result.SubmittedIntent.ActorCombatantId}");
        }

        if (result.PrimaryStep is not null)
        {
            RenderCombatStep(
                output,
                "Primary Step",
                result.PrimaryStep);
        }

        for (int index = 0;
            index < result.AutomaticSteps.Count;
            index++)
        {
            RenderCombatStep(
                output,
                $"Automatic Step {index + 1}",
                result.AutomaticSteps[index]);
        }
    }

    private static void RenderCombatStep(
        TextWriter output,
        string label,
        CombatStepResult step)
    {
        output.WriteLine();
        output.WriteLine(label);
        output.WriteLine($"Step Kind: {step.Kind}");
        output.WriteLine(
            $"Starting Revision: {step.StartingEncounterRevision}");
        output.WriteLine(
            $"Resulting Revision: {step.ResultingEncounterRevision}");

        if (step.ActorCombatantId is not null)
        {
            output.WriteLine(
                $"Actor ID: {step.ActorCombatantId}");
        }

        if (step.TargetCombatantId is not null)
        {
            output.WriteLine(
                $"Target ID: {step.TargetCombatantId}");
        }

        if (step.WinningSideId is not null)
        {
            output.WriteLine(
                $"Winning Side ID: {step.WinningSideId}");
        }

        foreach (CombatDieRoll die in step.Dice)
        {
            output.WriteLine(
                $"Die {die.Ordinal}: Purpose={die.Purpose}, Sides={die.Sides}, Value={die.Value}");
        }

        switch (step.Kind)
        {
            case CombatStepKind.Movement:
                RenderMovementStep(output, step);
                break;
            case CombatStepKind.WeaponAttack:
                RenderWeaponAttackStep(output, step);
                break;
            case CombatStepKind.SpellAttack:
                RenderSpellAttackStep(output, step);
                break;
            case CombatStepKind.DeathSavingThrow:
                RenderDeathSavingThrowStep(output, step);
                break;
            case CombatStepKind.TurnAdvanced:
                RenderTurnAdvanceStep(output, step);
                break;
            case CombatStepKind.CombatCompleted:
                if (step.WinningSideId is null)
                {
                    throw new InvalidOperationException(
                        "A combat-completion step did not contain a winning side.");
                }

                break;
            default:
                throw new InvalidOperationException(
                    "The combat step kind is unsupported by the console client.");
        }

        // Either a weapon attack or a spell cast can trigger this, so it is
        // rendered once here rather than duplicated in both step renderers.
        if (step.ConcentrationCheck is not null)
        {
            RenderConcentrationCheck(output, step.ConcentrationCheck);
        }
    }

    private static void RenderMovementStep(
        TextWriter output,
        CombatStepResult step)
    {
        var movement = step.Movement
            ?? throw new InvalidOperationException(
                "A movement step did not contain a movement result.");

        output.WriteLine(
            $"Starting Position: ({movement.StartingPosition.X}, {movement.StartingPosition.Y})");
        output.WriteLine(
            $"Ending Position: ({movement.EndingPosition.X}, {movement.EndingPosition.Y})");

        for (int index = 0; index < movement.Path.Count; index++)
        {
            output.WriteLine(
                $"Path Position {index + 1}: ({movement.Path[index].X}, {movement.Path[index].Y})");
        }

        output.WriteLine(
            $"Movement Spent Feet: {movement.MovementSpentFeet}");
    }

    private static void RenderWeaponAttackStep(
        TextWriter output,
        CombatStepResult step)
    {
        var weaponAttack = step.WeaponAttack
            ?? throw new InvalidOperationException(
                "A weapon-attack step did not contain a weapon-attack result.");
        output.WriteLine($"Weapon ID: {weaponAttack.WeaponId}");
        output.WriteLine(
            $"Distance Feet: {weaponAttack.DistanceFeet}");
        output.WriteLine(
            $"Line of Sight: {FormatBoolean(weaponAttack.HasLineOfSight)}");
        output.WriteLine(
            $"Cover Level: {weaponAttack.CoverLevel}");
        output.WriteLine(
            $"Cover Armor Class Bonus: {weaponAttack.CoverArmorClassBonus}");
        output.WriteLine(
            $"Attack Roll Mode: {weaponAttack.AttackRollMode}");
        output.WriteLine($"First Roll: {weaponAttack.FirstRoll}");

        if (weaponAttack.SecondRoll is not null)
        {
            output.WriteLine(
                $"Second Roll: {weaponAttack.SecondRoll}");
        }

        output.WriteLine($"Natural Roll: {weaponAttack.NaturalRoll}");
        output.WriteLine($"Attack Bonus: {weaponAttack.AttackBonus}");
        output.WriteLine($"Attack Total: {weaponAttack.AttackTotal}");
        output.WriteLine(
            $"Target Armor Class: {weaponAttack.TargetArmorClass}");
        output.WriteLine($"Attack Outcome: {weaponAttack.Outcome}");
        output.WriteLine(
            $"Final Damage: {weaponAttack.FinalDamage}");

        if (weaponAttack.DamagedTarget is null)
        {
            return;
        }

        var damagedTarget = weaponAttack.DamagedTarget;

        output.WriteLine(
            $"Resulting Target Hit Points: {damagedTarget.CurrentHitPoints} / {damagedTarget.MaximumHitPoints}");
        output.WriteLine(
            $"Resulting Target Temporary Hit Points: {damagedTarget.TemporaryHitPoints}");
        output.WriteLine(
            $"Resulting Target Lifecycle: {damagedTarget.LifecycleState}");
    }

    private static void RenderSpellAttackStep(
        TextWriter output,
        CombatStepResult step)
    {
        var spellAttack = step.SpellAttack
            ?? throw new InvalidOperationException(
                "A spell-attack step did not contain a spell-attack result.");
        output.WriteLine($"Spell ID: {spellAttack.SpellId}");
        output.WriteLine(
            $"Distance Feet: {spellAttack.DistanceFeet}");

        if (spellAttack.AttackRollMode is not null)
        {
            output.WriteLine(
                $"Attack Roll Mode: {spellAttack.AttackRollMode}");
            output.WriteLine(
                $"First Roll: {spellAttack.FirstAttackRoll}");

            if (spellAttack.SecondAttackRoll is not null)
            {
                output.WriteLine(
                    $"Second Roll: {spellAttack.SecondAttackRoll}");
            }

            output.WriteLine(
                $"Natural Roll: {spellAttack.AttackNaturalRoll}");
            output.WriteLine(
                $"Attack Bonus: {spellAttack.AttackBonus}");
            output.WriteLine(
                $"Attack Total: {spellAttack.AttackTotal}");
            output.WriteLine(
                $"Target Armor Class: {spellAttack.TargetArmorClass}");
            output.WriteLine(
                $"Attack Outcome: {spellAttack.AttackOutcome}");
        }
        else if (spellAttack.SaveAbility is not null)
        {
            output.WriteLine(
                $"Save Ability: {spellAttack.SaveAbility}");
            output.WriteLine(
                $"Save First Roll: {spellAttack.SaveFirstRoll}");
            output.WriteLine(
                $"Combined Saving Throw Bonus: {spellAttack.CombinedSavingThrowBonus}");
            output.WriteLine(
                $"Save Difficulty Class: {spellAttack.SaveDifficultyClass}");
            output.WriteLine(
                $"Save Outcome: {spellAttack.SaveOutcome}");
        }

        output.WriteLine($"Took Effect: {FormatBoolean(spellAttack.TookEffect)}");
        output.WriteLine($"Damage Dealt: {spellAttack.DamageDealt}");
        output.WriteLine($"Healing Done: {spellAttack.HealingDone}");

        if (spellAttack.DamagedTarget is null)
        {
            return;
        }

        var damagedTarget = spellAttack.DamagedTarget;

        output.WriteLine(
            $"Resulting Target Hit Points: {damagedTarget.CurrentHitPoints} / {damagedTarget.MaximumHitPoints}");
        output.WriteLine(
            $"Resulting Target Temporary Hit Points: {damagedTarget.TemporaryHitPoints}");
        output.WriteLine(
            $"Resulting Target Lifecycle: {damagedTarget.LifecycleState}");
    }

    private static void RenderConcentrationCheck(
        TextWriter output,
        CombatConcentrationCheckStepDetail concentrationCheck)
    {
        output.WriteLine();
        output.WriteLine("Concentration Check");
        output.WriteLine(
            $"Combatant ID: {concentrationCheck.CombatantId}");
        output.WriteLine(
            $"Effect ID: {concentrationCheck.EffectId}");
        output.WriteLine(
            $"Broken By Incapacitation: {FormatBoolean(concentrationCheck.BrokenByIncapacitation)}");

        if (!concentrationCheck.BrokenByIncapacitation)
        {
            output.WriteLine(
                $"First Roll: {concentrationCheck.FirstRoll}");
            output.WriteLine(
                $"Combined Saving Throw Bonus: {concentrationCheck.CombinedSavingThrowBonus}");
            output.WriteLine($"Total: {concentrationCheck.Total}");
            output.WriteLine(
                $"Difficulty Class: {concentrationCheck.DifficultyClass}");
            output.WriteLine($"Outcome: {concentrationCheck.Outcome}");
        }

        output.WriteLine(
            $"Effect Dropped: {FormatBoolean(concentrationCheck.EffectDropped)}");
    }

    private static void RenderDeathSavingThrowStep(
        TextWriter output,
        CombatStepResult step)
    {
        var deathSavingThrow = step.DeathSavingThrow
            ?? throw new InvalidOperationException(
                "A death-saving-throw step did not contain a death-saving-throw result.");
        output.WriteLine(
            $"Previous Lifecycle: {deathSavingThrow.PreviousLifecycleState}");
        output.WriteLine(
            $"Resulting Lifecycle: {deathSavingThrow.LifecycleState}");
        output.WriteLine($"First Roll: {deathSavingThrow.FirstRoll}");

        if (deathSavingThrow.SecondRoll is not null)
        {
            output.WriteLine(
                $"Second Roll: {deathSavingThrow.SecondRoll}");
        }

        output.WriteLine($"Natural Roll: {deathSavingThrow.NaturalRoll}");
        output.WriteLine(
            $"Saving Throw Bonus: {deathSavingThrow.SavingThrowBonus}");
        output.WriteLine($"Saving Throw Total: {deathSavingThrow.Total}");
        output.WriteLine(
            $"Difficulty Class: {deathSavingThrow.DifficultyClass}");
        output.WriteLine(
            $"Death Saving Throw Outcome: {deathSavingThrow.Outcome}");
        output.WriteLine(
            $"Resulting Death Save Successes: {deathSavingThrow.SuccessCount}");
        output.WriteLine(
            $"Resulting Death Save Failures: {deathSavingThrow.FailureCount}");
        output.WriteLine(
            $"Resulting Stable: {FormatBoolean(deathSavingThrow.IsStable)}");
    }

    private static void RenderTurnAdvanceStep(
        TextWriter output,
        CombatStepResult step)
    {
        var turnAdvance = step.TurnAdvancement
            ?? throw new InvalidOperationException(
                "A turn-advance step did not contain a turn-advancement result.");

        output.WriteLine(
            $"Ended Turn Combatant ID: {turnAdvance.EndedTurnCombatantId}");
        output.WriteLine(
            $"New Active Combatant ID: {turnAdvance.ActiveCombatantId}");
        output.WriteLine(
            $"Previous Round Number: {turnAdvance.PreviousRoundNumber}");
        output.WriteLine(
            $"Resulting Round Number: {turnAdvance.RoundNumber}");
        output.WriteLine(
            $"Started New Round: {FormatBoolean(turnAdvance.StartedNewRound)}");

        for (int index = 0;
            index < turnAdvance.SkippedCombatantIds.Count;
            index++)
        {
            output.WriteLine(
                $"Skipped Combatant {index + 1}: {turnAdvance.SkippedCombatantIds[index]}");
        }

        if (step.TurnAdvanceReason is not null)
        {
            output.WriteLine(
                $"Turn Advance Reason: {step.TurnAdvanceReason}");
        }
    }

    private static void RenderCombatOutcome(
        TextWriter output,
        CombatOutcomeResult outcome)
    {
        output.WriteLine();
        output.WriteLine("Combat Outcome");
        output.WriteLine($"Outcome: {outcome.Outcome}");
        output.WriteLine(
            $"Resulting Mode: {outcome.ResultingMode}");
        output.WriteLine(
            $"Resulting Progress: {outcome.ResultingProgressId}");
    }

    private static void ValidatePlayerDecision(
        CombatDecision decision)
    {
        if (decision.State
            == CombatDecisionState.AutomaticProcessingRequired)
        {
            throw new InvalidOperationException(
                "Automatic combat processing remained after Application normalization.");
        }

        if (decision.State
            != CombatDecisionState.PlayerDecisionRequired)
        {
            throw new InvalidOperationException(
                "A selectable combat menu requires a player decision.");
        }

        if (string.IsNullOrWhiteSpace(decision.ActiveCombatantId))
        {
            throw new InvalidOperationException(
                "A player combat decision requires an active combatant.");
        }

        if (decision.Movement is null
            || decision.EndTurn is null)
        {
            throw new InvalidOperationException(
                "A player combat decision requires movement and End Turn options.");
        }
    }

    private sealed record CombatMenuOption(
        string Label,
        CombatMenuAction Action,
        CombatMovementDestinationOption?
            MovementDestination = null,
        CombatWeaponAttackOption? WeaponAttack = null,
        CombatSpellAttackOption? SpellAttack = null,
        CombatTargetOption? Target = null);

    private sealed record CombatSessionRunResult(
        bool ExitRequested,
        ApplicationSessionState Session);

    private enum CombatMenuAction
    {
        Move,
        WeaponAttack,
        SpellAttack,
        EndTurn,
        InspectEncounter,
        Exit
    }
}
