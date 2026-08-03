namespace FiveEGoldBox.Application.Tests;

/// Writes the two frozen combat transcript fixtures, mirroring
/// FixtureWriter.cs's pattern for the JSON save fixtures: skipped in a normal
/// pass (a frozen transcript that regenerates itself proves nothing), run by
/// hand when a fixture is deliberately regenerated. Reuses the same scripted
/// runner the frozen-transcript assertions in the other partial of this class
/// already drive, so there is exactly one place that knows how to play out
/// the aggressive and passive scripts.
public sealed partial class WatchtowerCombatOrchestratorCharacterizationTests
{
    [Fact(Skip = "Run by hand when a fixture is deliberately regenerated.")]
    public void WriteFrozenCombatTranscripts()
    {
        ScriptedRun aggressive = RunScriptedCombat(
            WatchtowerSignalTestData.CreateEncounterSession(),
            aggressive: true);

        Write("watchtower-combat-aggressive-transcript.txt", aggressive.Lines);

        ScriptedRun passive = RunScriptedCombat(
            CreateFragilePartyEncounterSession(),
            aggressive: false);

        Write("watchtower-combat-passive-transcript.txt", passive.Lines);
    }

    private static void Write(string fileName, IReadOnlyList<string> lines)
    {
        File.WriteAllLines(
            Path.Combine(
                AppContext.BaseDirectory,
                "..",
                "..",
                "..",
                "Fixtures",
                fileName),
            lines);
    }
}
