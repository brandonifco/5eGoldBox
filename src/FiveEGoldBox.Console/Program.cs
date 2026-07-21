namespace FiveEGoldBox.Console;

internal static class Program
{
    private const int DefaultRandomSeed = 1;

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
            DefaultRandomSeed);
    }
}
