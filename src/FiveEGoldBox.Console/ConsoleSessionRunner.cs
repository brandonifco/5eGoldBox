using System.Globalization;
using FiveEGoldBox.Application.Persistence;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Application.Scenarios;

namespace FiveEGoldBox.Console;

internal sealed partial class ConsoleSessionRunner
{
    internal int Run(
        TextReader input,
        TextWriter output,
        string savePath,
        int defaultRandomSeed)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(output);

        if (string.IsNullOrWhiteSpace(savePath))
        {
            throw new ArgumentException(
                "A save path is required.",
                nameof(savePath));
        }

        while (true)
        {
            WriteStartMenu(output);

            string? selection = input.ReadLine();

            if (selection is null)
            {
                return 0;
            }

            switch (ParseSelection(selection, maximum: 3))
            {
                case 1:
                    ApplicationSessionState newSession =
                        WatchtowerScenarioSessionFactory.CreateNew(
                            defaultRandomSeed);

                    return RunSession(
                        input,
                        output,
                        savePath,
                        newSession);
                case 2:
                    ApplicationSessionState? loadedSession =
                        LoadSession(output, savePath);

                    if (loadedSession is not null)
                    {
                        return RunSession(
                            input,
                            output,
                            savePath,
                            loadedSession);
                    }

                    break;
                case 3:
                    return 0;
                default:
                    output.WriteLine("Invalid selection.");
                    break;
            }
        }
    }

    internal void RenderSessionSummary(
        TextWriter output,
        ApplicationSessionState session)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(session);

        bool canSerialize =
            ManualSaveSerializer.CanSerialize(session);

        output.WriteLine();
        output.WriteLine("Session Summary");
        output.WriteLine($"Mode: {session.CurrentMode}");
        output.WriteLine($"Scenario: {session.ScenarioId}");
        output.WriteLine(
            $"Location: {session.CurrentLocationId}");
        output.WriteLine(
            $"Progress: {session.Scenario.Progress}");
        output.WriteLine($"Random Seed: {session.RandomSeed}");
        output.WriteLine(
            $"Random Values Consumed: {session.RandomValuesConsumed}");
        output.WriteLine(
            $"Manual Save Available: {FormatBoolean(canSerialize)}");

        RenderModeSummary(output, session);
    }

    internal void RenderParty(
        TextWriter output,
        ApplicationSessionState session)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(session);

        output.WriteLine();
        output.WriteLine("Party Inspection");
        output.WriteLine($"Party ID: {session.Party.PartyId}");

        for (int index = 0;
            index < session.Party.Members.Count;
            index++)
        {
            var member = session.Party.Members[index];
            var health = member.Health;
            var hitPoints = health.HitPoints;
            var deathSavingThrows =
                health.DeathSavingThrows;

            output.WriteLine($"Member {index + 1}");
            output.WriteLine($"Name: {member.DisplayName}");
            output.WriteLine(
                $"Party Member ID: {member.PartyMemberId}");
            output.WriteLine($"Class ID: {member.ClassId}");
            output.WriteLine(
                $"Hit Points: {hitPoints.CurrentHitPoints} / {hitPoints.MaximumHitPoints}");
            output.WriteLine(
                $"Temporary Hit Points: {hitPoints.TemporaryHitPoints}");
            output.WriteLine(
                $"Stable: {FormatBoolean(deathSavingThrows.IsStable)}");
            output.WriteLine(
                $"Dead: {FormatBoolean(health.IsDead)}");
            output.WriteLine(
                $"Instant Death: {FormatBoolean(health.IsInstantlyDead)}");
            output.WriteLine(
                $"Death Save Successes: {deathSavingThrows.SuccessCount}");
            output.WriteLine(
                $"Death Save Failures: {deathSavingThrows.FailureCount}");

            var ammunition = member.Ammunition;

            if (ammunition is not null)
            {
                output.WriteLine(
                    $"Weapon ID: {ammunition.WeaponId}");
                output.WriteLine(
                    $"Ammunition Item ID: {ammunition.AmmunitionItemId}");
                output.WriteLine(
                    $"Ammunition Remaining: {ammunition.RemainingQuantity}");
            }
        }
    }

    internal int RunSession(
        TextReader input,
        TextWriter output,
        string savePath,
        ApplicationSessionState session)
    {
        return RunNoncombatSession(
            input,
            output,
            savePath,
            session);
    }

    private static void WriteStartMenu(TextWriter output)
    {
        output.WriteLine("5eGoldBox Console Reference Client");
        output.WriteLine("1. New Game");
        output.WriteLine("2. Load Game");
        output.WriteLine("3. Exit");
        output.Write("Selection: ");
    }

    private static ApplicationSessionState? LoadSession(
        TextWriter output,
        string savePath)
    {
        string serializedData;

        try
        {
            serializedData = File.ReadAllText(savePath);
        }
        catch (FileNotFoundException)
        {
            output.WriteLine(
                $"Load failed: save file not found at {savePath}.");
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            output.WriteLine(
                $"Load failed: access was denied for {savePath}.");
            return null;
        }
        catch (IOException)
        {
            output.WriteLine(
                $"Load failed: unable to read {savePath}.");
            return null;
        }

        ManualSaveLoadResult result =
            ManualSaveSerializer.Deserialize(serializedData);

        if (result.IsSuccess)
        {
            return result.Session
                ?? throw new InvalidOperationException(
                    "A successful manual-save load did not return a session.");
        }

        ManualSaveLoadFailureReason failureReason =
            result.FailureReason
            ?? throw new InvalidOperationException(
                "A failed manual-save load did not return a failure reason.");

        output.WriteLine(
            failureReason switch
            {
                ManualSaveLoadFailureReason.MalformedSerializedData =>
                    "Load failed: malformed save data.",
                ManualSaveLoadFailureReason.UnsupportedFormatVersion =>
                    "Load failed: unsupported save format version.",
                ManualSaveLoadFailureReason.InvalidSessionState =>
                    "Load failed: invalid saved session state.",
                _ => throw new InvalidOperationException(
                    "The manual-save load failure reason is unsupported.")
            });

        return null;
    }

    private static void SaveSession(
        TextWriter output,
        string savePath,
        ApplicationSessionState session)
    {
        if (!ManualSaveSerializer.CanSerialize(session))
        {
            output.WriteLine(
                "Save failed: manual saving is no longer available.");
            return;
        }

        string serializedData =
            ManualSaveSerializer.Serialize(session);

        try
        {
            File.WriteAllText(savePath, serializedData);
        }
        catch (UnauthorizedAccessException)
        {
            output.WriteLine(
                $"Save failed: access was denied for {savePath}.");
            return;
        }
        catch (IOException)
        {
            output.WriteLine(
                $"Save failed: unable to write {savePath}.");
            return;
        }

        output.WriteLine($"Game saved to {savePath}.");
    }

    private static void RenderModeSummary(
        TextWriter output,
        ApplicationSessionState session)
    {
        switch (session.CurrentMode)
        {
            case ApplicationMode.Outpost:
                output.WriteLine(
                    "Outpost State: Party is at the current outpost.");
                break;
            case ApplicationMode.RegionalTravel:
                var travel = session.RegionalTravel
                    ?? throw new InvalidOperationException(
                        "Regional-travel mode requires regional-travel state.");

                output.WriteLine($"Route ID: {travel.RouteId}");
                output.WriteLine(
                    $"Origin Location ID: {travel.OriginLocationId}");
                output.WriteLine(
                    $"Destination Location ID: {travel.DestinationLocationId}");
                output.WriteLine(
                    $"Current Step Index: {travel.CurrentStepIndex}");
                output.WriteLine(
                    $"Final Step Index: {travel.FinalStepIndex}");
                output.WriteLine(
                    $"Travel Complete: {FormatBoolean(travel.IsComplete)}");
                break;
            case ApplicationMode.Exploration:
                var exploration = session.Exploration
                    ?? throw new InvalidOperationException(
                        "Exploration mode requires exploration state.");

                output.WriteLine($"Map ID: {exploration.MapId}");
                output.WriteLine($"Floor: {exploration.Floor}");
                output.WriteLine(
                    $"Position X: {exploration.Position.X}");
                output.WriteLine(
                    $"Position Y: {exploration.Position.Y}");
                output.WriteLine($"Facing: {exploration.Facing}");
                break;
            case ApplicationMode.Encounter:
                var encounter = session.ActiveEncounter?.Encounter
                    ?? throw new InvalidOperationException(
                        "Encounter mode requires active-encounter state.");

                output.WriteLine(
                    $"Encounter ID: {encounter.EncounterId}");
                output.WriteLine(
                    $"Encounter Lifecycle: {encounter.LifecycleState}");
                output.WriteLine(
                    $"Encounter Revision: {encounter.Revision}");
                output.WriteLine(
                    $"Active Combatant ID: {encounter.ActiveCombatantId}");

                if (encounter.WinningSideId is not null)
                {
                    output.WriteLine(
                        $"Winning Side ID: {encounter.WinningSideId}");
                }

                break;
            case ApplicationMode.ScenarioConclusion:
                output.WriteLine("Scenario Conclusion");
                output.WriteLine(
                    $"Conclusion Progress: {session.Scenario.Progress}");
                output.WriteLine(
                    $"Conclusion Location: {session.CurrentLocationId}");
                break;
            default:
                throw new InvalidOperationException(
                    "The application mode is unsupported by the console client.");
        }
    }

    private static int ParseSelection(
        string value,
        int maximum)
    {
        return int.TryParse(
            value,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out int selection)
            && selection >= 1
            && selection <= maximum
                ? selection
                : 0;
    }

    private static string FormatBoolean(bool value)
    {
        return value ? "Yes" : "No";
    }
}
