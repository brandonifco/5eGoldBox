namespace FiveEGoldBox.Console;

internal static class Program
{
    private const int DefaultRandomSeed = 1;

    /// Which scenario this client launches. The console knows the name of a
    /// scenario, not anything about its content.
    private const string ScenarioId = "scenario.watchtower";

    private static int Main()
    {
        string savePath = Path.Combine(
            Environment.CurrentDirectory,
            "savegame.json");
        ConsoleSessionRunner runner = new();

        return runner.Run(
            System.Console.In,
            System.Console.Out,
            savePath,
            DefaultRandomSeed,
            ScenarioId);
    }
}
