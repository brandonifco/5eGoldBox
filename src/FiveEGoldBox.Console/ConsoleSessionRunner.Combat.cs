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
        WatchtowerCombatResolutionResult normalization =
            WatchtowerCombatRules.AdvanceToDecision(session);

        RenderCombatResolution(output, normalization);

        session = normalization.State;
        WatchtowerCombatDecision decision =
            normalization.ResultingDecision;

        while (true)
        {
            if (decision.State
                == WatchtowerCombatDecisionState.CombatCompleted)
            {
                WatchtowerCombatOutcomeResult outcome =
                    WatchtowerCombatOutcomeRules.Finalize(session);

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
                    WatchtowerCombatMovementDestinationOption
                        movementDestination =
                            selectedOption.MovementDestination
                            ?? throw new InvalidOperationException(
                                "A movement menu option did not contain a movement destination.");

                    WatchtowerCombatResolutionResult moveResult =
                        WatchtowerCombatRules.Execute(
                            session,
                            new WatchtowerCombatMoveIntent
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
                    WatchtowerCombatWeaponAttackOption weaponAttack =
                        selectedOption.WeaponAttack
                        ?? throw new InvalidOperationException(
                            "A weapon-attack menu option did not contain a weapon option.");
                    WatchtowerCombatTargetOption target =
                        selectedOption.Target
                        ?? throw new InvalidOperationException(
                            "A weapon-attack menu option did not contain a target option.");

                    WatchtowerCombatResolutionResult attackResult =
                        WatchtowerCombatRules.Execute(
                            session,
                            new WatchtowerCombatWeaponAttackIntent
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
                case CombatMenuAction.EndTurn:
                    WatchtowerCombatResolutionResult endTurnResult =
                        WatchtowerCombatRules.Execute(
                            session,
                            new WatchtowerCombatEndTurnIntent
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
        WatchtowerCombatDecision decision)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(decision);

        ValidatePlayerDecision(decision);

        WatchtowerCombatMovementOption movement =
            decision.Movement!;
        WatchtowerCombatWeaponAttackOption weaponAttack =
            decision.WeaponAttack!;
        WatchtowerCombatEndTurnOption endTurn =
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
        output.WriteLine($"Weapon ID: {weaponAttack.WeaponId}");
        output.WriteLine(
            $"Weapon Attack Available: {FormatBoolean(weaponAttack.IsAvailable)}");
        output.WriteLine(
            $"Weapon Attack Unavailability Reason: {weaponAttack.UnavailabilityReason}");
        output.WriteLine(
            $"Legal Attack Target Count: {(weaponAttack.IsAvailable ? weaponAttack.Targets.Count(target => target.IsAvailable) : 0)}");
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
            WatchtowerCombatDecision decision)
    {
        ValidatePlayerDecision(decision);

        List<CombatMenuOption> options = new();
        WatchtowerCombatMovementOption movement =
            decision.Movement!;

        if (movement.IsAvailable)
        {
            foreach (WatchtowerCombatMovementDestinationOption destination
                in movement.DestinationOptions)
            {
                options.Add(
                    new CombatMenuOption(
                        $"Move to ({destination.Destination.X}, {destination.Destination.Y}) - {destination.MovementSpentFeet} ft",
                        CombatMenuAction.Move,
                        MovementDestination: destination));
            }
        }

        WatchtowerCombatWeaponAttackOption weaponAttack =
            decision.WeaponAttack!;

        if (weaponAttack.IsAvailable)
        {
            foreach (WatchtowerCombatTargetOption target
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
        WatchtowerCombatWeaponAttackOption weaponAttack,
        WatchtowerCombatTargetOption target)
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
        WatchtowerCombatResolutionResult result)
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
        WatchtowerCombatStepResult step)
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

        foreach (WatchtowerCombatDieRoll die in step.Dice)
        {
            output.WriteLine(
                $"Die {die.Ordinal}: Purpose={die.Purpose}, Sides={die.Sides}, Value={die.Value}");
        }

        switch (step.Kind)
        {
            case WatchtowerCombatStepKind.Movement:
                RenderMovementStep(output, step);
                break;
            case WatchtowerCombatStepKind.WeaponAttack:
                RenderWeaponAttackStep(output, step);
                break;
            case WatchtowerCombatStepKind.DeathSavingThrow:
                RenderDeathSavingThrowStep(output, step);
                break;
            case WatchtowerCombatStepKind.TurnAdvanced:
                RenderTurnAdvanceStep(output, step);
                break;
            case WatchtowerCombatStepKind.CombatCompleted:
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
    }

    private static void RenderMovementStep(
        TextWriter output,
        WatchtowerCombatStepResult step)
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
        WatchtowerCombatStepResult step)
    {
        var weaponAttack = step.WeaponAttack
            ?? throw new InvalidOperationException(
                "A weapon-attack step did not contain a weapon-attack result.");
        var attackRoll = weaponAttack.Attack.AttackRoll;

        output.WriteLine($"Weapon ID: {weaponAttack.WeaponId}");
        output.WriteLine(
            $"Distance Feet: {weaponAttack.DistanceFeet}");
        output.WriteLine(
            $"Line of Sight: {FormatBoolean(weaponAttack.LineOfSight.HasLineOfSight)}");
        output.WriteLine(
            $"Cover Level: {weaponAttack.Cover.CoverLevel}");
        output.WriteLine(
            $"Cover Armor Class Bonus: {weaponAttack.Cover.ArmorClassBonus}");
        output.WriteLine(
            $"Attack Roll Mode: {attackRoll.RollMode}");
        output.WriteLine($"First Roll: {attackRoll.FirstRoll}");

        if (attackRoll.SecondRoll is not null)
        {
            output.WriteLine(
                $"Second Roll: {attackRoll.SecondRoll}");
        }

        output.WriteLine($"Natural Roll: {attackRoll.NaturalRoll}");
        output.WriteLine($"Attack Bonus: {attackRoll.AttackBonus}");
        output.WriteLine($"Attack Total: {attackRoll.Total}");
        output.WriteLine(
            $"Target Armor Class: {attackRoll.TargetArmorClass}");
        output.WriteLine($"Attack Outcome: {attackRoll.Outcome}");
        output.WriteLine(
            $"Final Damage: {weaponAttack.Attack.Damage.FinalDamage}");

        if (weaponAttack.TargetDamage is null)
        {
            return;
        }

        var targetState = weaponAttack.TargetDamage.State;
        var hitPoints = targetState.Health.HitPoints;

        output.WriteLine(
            $"Resulting Target Hit Points: {hitPoints.CurrentHitPoints} / {hitPoints.MaximumHitPoints}");
        output.WriteLine(
            $"Resulting Target Temporary Hit Points: {hitPoints.TemporaryHitPoints}");
        output.WriteLine(
            $"Resulting Target Lifecycle: {targetState.LifecycleState}");
    }

    private static void RenderDeathSavingThrowStep(
        TextWriter output,
        WatchtowerCombatStepResult step)
    {
        var deathSavingThrow = step.DeathSavingThrow
            ?? throw new InvalidOperationException(
                "A death-saving-throw step did not contain a death-saving-throw result.");
        var result = deathSavingThrow.CombatantDeathSavingThrow
            .HealthDeathSavingThrow.DeathSavingThrow;

        output.WriteLine(
            $"Previous Lifecycle: {deathSavingThrow.PreviousLifecycleState}");
        output.WriteLine(
            $"Resulting Lifecycle: {deathSavingThrow.LifecycleState}");
        output.WriteLine($"First Roll: {result.FirstRoll}");

        if (result.SecondRoll is not null)
        {
            output.WriteLine(
                $"Second Roll: {result.SecondRoll}");
        }

        output.WriteLine($"Natural Roll: {result.NaturalRoll}");
        output.WriteLine(
            $"Saving Throw Bonus: {result.SavingThrowBonus}");
        output.WriteLine($"Saving Throw Total: {result.Total}");
        output.WriteLine(
            $"Difficulty Class: {result.DifficultyClass}");
        output.WriteLine(
            $"Death Saving Throw Outcome: {result.Outcome}");
        output.WriteLine(
            $"Resulting Death Save Successes: {result.State.SuccessCount}");
        output.WriteLine(
            $"Resulting Death Save Failures: {result.State.FailureCount}");
        output.WriteLine(
            $"Resulting Stable: {FormatBoolean(result.State.IsStable)}");
    }

    private static void RenderTurnAdvanceStep(
        TextWriter output,
        WatchtowerCombatStepResult step)
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
        WatchtowerCombatOutcomeResult outcome)
    {
        output.WriteLine();
        output.WriteLine("Combat Outcome");
        output.WriteLine($"Outcome: {outcome.Outcome}");
        output.WriteLine(
            $"Resulting Mode: {outcome.ResultingMode}");
        output.WriteLine(
            $"Resulting Progress: {outcome.ResultingProgress}");
    }

    private static void ValidatePlayerDecision(
        WatchtowerCombatDecision decision)
    {
        if (decision.State
            == WatchtowerCombatDecisionState.AutomaticProcessingRequired)
        {
            throw new InvalidOperationException(
                "Automatic combat processing remained after Application normalization.");
        }

        if (decision.State
            != WatchtowerCombatDecisionState.PlayerDecisionRequired)
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
            || decision.WeaponAttack is null
            || decision.EndTurn is null)
        {
            throw new InvalidOperationException(
                "A player combat decision requires movement, weapon-attack, and End Turn options.");
        }
    }

    private sealed record CombatMenuOption(
        string Label,
        CombatMenuAction Action,
        WatchtowerCombatMovementDestinationOption?
            MovementDestination = null,
        WatchtowerCombatWeaponAttackOption? WeaponAttack = null,
        WatchtowerCombatTargetOption? Target = null);

    private sealed record CombatSessionRunResult(
        bool ExitRequested,
        ApplicationSessionState Session);

    private enum CombatMenuAction
    {
        Move,
        WeaponAttack,
        EndTurn,
        InspectEncounter,
        Exit
    }
}
