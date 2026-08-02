using FiveEGoldBox.Application.Exploration;
using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Console;

/// The `goto` verb: a developer tool that jumps straight into an arbitrary
/// exploration location/floor/position/facing, bypassing the outpost
/// decision and regional travel a normal session would have to walk through
/// first. Built so a specific spot on a map can be reached to visually check
/// something (e.g. a door or piece of treasure) without replaying a scenario
/// up to it every time.
internal static class GotoLocationCommand
{
    private const string Usage =
        "Usage: goto <scenario-id> <location-id> <floor> <x> <y> <facing> [progress-id]";

    private const int DefaultRandomSeed = 1;

    internal static int Run(
        IReadOnlyList<string> arguments,
        TextWriter output,
        TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);

        if (arguments.Count is < 6 or > 7)
        {
            error.WriteLine(Usage);
            return 2;
        }

        string scenarioId = arguments[0];
        string locationId = arguments[1];
        string floor = arguments[2];
        string? progressId = arguments.Count == 7 ? arguments[6] : null;

        if (!int.TryParse(arguments[3], out int x))
        {
            error.WriteLine($"'{arguments[3]}' is not a valid X coordinate. {Usage}");
            return 2;
        }

        if (!int.TryParse(arguments[4], out int y))
        {
            error.WriteLine($"'{arguments[4]}' is not a valid Y coordinate. {Usage}");
            return 2;
        }

        if (!Enum.TryParse(arguments[5], ignoreCase: true, out ExplorationFacing facing)
            || !Enum.IsDefined(facing))
        {
            error.WriteLine($"'{arguments[5]}' is not a valid facing. {Usage}");
            return 2;
        }

        ApplicationSessionState session;

        try
        {
            session = ScenarioSessionFactory.CreateAtExploration(
                scenarioId,
                locationId,
                floor,
                new GridPosition(x, y),
                facing,
                DefaultRandomSeed,
                progressId);
        }
        catch (ArgumentException exception)
        {
            error.WriteLine($"Could not start a session there: {exception.Message}");
            return 1;
        }

        string savePath = Path.Combine(
            Environment.CurrentDirectory,
            "savegame.json");

        return new ConsoleSessionRunner().RunSession(
            System.Console.In,
            output,
            savePath,
            session);
    }
}
