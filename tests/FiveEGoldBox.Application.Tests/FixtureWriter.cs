using FiveEGoldBox.Application.Exploration;
using FiveEGoldBox.Application.Parties;
using FiveEGoldBox.Application.Persistence;
using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Tests;

/// Writes the frozen save fixtures, so that regenerating one is a deliberate
/// act somebody can repeat rather than a hand-edited file. Skipped in a normal
/// pass: a frozen fixture that regenerates itself proves nothing.
public sealed class FixtureWriter
{
    [Fact(Skip = "Run by hand when a fixture is deliberately regenerated.")]
    public void WriteFourCharacterPartyFixtures()
    {
        Write(
            "v1-outpost-four-character-party.json",
            ScenarioSessionFactory.CreateNew(
                WatchtowerScenarioContent.ScenarioId,
                randomSeed: 424242));

        ApplicationSessionState exploration =
            ScenarioSessionFactory.CreateNew(
                WatchtowerScenarioContent.ScenarioId,
                randomSeed: 8675309);

        exploration = exploration with
        {
            CurrentMode = ApplicationMode.Exploration,
            CurrentLocationId = "location.ruined-watchtower",
            Scenario = WatchtowerScenario.CreateState(
                WatchtowerScenarioProgress.MissionAccepted),
            RandomValuesConsumed = 12,
            Exploration = new ExplorationState
            {
                MapId = "map.ruined-watchtower",
                Floor = "UpperFloor",
                Position = new GridPosition(1, 1),
                Facing = ExplorationFacing.East
            }
        };

        Write("v1-exploration-four-character-party.json", exploration);

        ApplicationSessionState defeated =
            ScenarioSessionFactory.CreateNew(
                WatchtowerScenarioContent.ScenarioId,
                randomSeed: 8675309);

        defeated = defeated with
        {
            CurrentMode = ApplicationMode.ScenarioConclusion,
            CurrentLocationId = "location.ruined-watchtower",
            Scenario = WatchtowerScenario.CreateState(
                WatchtowerScenarioProgress.PartyDefeated),
            RandomValuesConsumed = 37,
            Party = defeated.Party with
            {
                Members = defeated.Party.Members
                    .Select(member => member with
                    {
                        Health = member.Health with
                        {
                            HitPoints = member.Health.HitPoints with
                            {
                                CurrentHitPoints = 0,
                                TemporaryHitPoints = 0
                            }
                        }
                    })
                    .ToArray()
            }
        };

        Write(
            "v1-scenario-conclusion-four-character-party.json",
            defeated);
    }

    private static void Write(
        string fileName,
        ApplicationSessionState session)
    {
        File.WriteAllText(
            Path.Combine(
                AppContext.BaseDirectory,
                "..",
                "..",
                "..",
                "Fixtures",
                fileName),
            ManualSaveSerializer.Serialize(session));
    }
}
