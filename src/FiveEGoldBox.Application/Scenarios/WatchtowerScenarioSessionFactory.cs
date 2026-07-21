using FiveEGoldBox.Application.Parties;
using FiveEGoldBox.Application.Sessions;

namespace FiveEGoldBox.Application.Scenarios;

public static class WatchtowerScenarioSessionFactory
{
    public static ApplicationSessionState CreateNew(
        int randomSeed)
    {
        PartyState party =
            WatchtowerScenarioContent
                .CreateStartingParty();

        return ApplicationSessionRules.CreateNew(
            WatchtowerScenarioContent.ScenarioId,
            WatchtowerScenarioContent
                .OutpostLocationId,
            party,
            randomSeed);
    }
}
