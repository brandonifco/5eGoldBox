using FiveEGoldBox.Application.Exploration;
using FiveEGoldBox.Application.Outposts;
using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Scenarios.Definitions;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Application.Travel;

namespace FiveEGoldBox.Application.Views;

/// Projects a session into what a client needs to draw it.
///
/// Without this, every client works out for itself which of six rule classes
/// to ask what, and then invents its own names for the answers — which is how
/// a text client ends up with "Enter Watchtower" written into it. Here the
/// engine answers once, and the words come from the scenario.
///
/// Read-only throughout. Acting on an action means calling the rule it names;
/// this decides what is offered, not what happens next.
public static class SessionView
{
    public static SessionViewModel Describe(
        ApplicationSessionState session)
    {
        ArgumentNullException.ThrowIfNull(session);

        ApplicationSessionState canonicalSession =
            ApplicationSessionRules.CreateCanonical(session);
        ScenarioDefinition scenario =
            ScenarioDefinitionRegistry.Resolve(canonicalSession);
        ScenarioConclusionDefinition? conclusion = scenario.Progress.Conclusions
            .FirstOrDefault(candidate => string.Equals(
                candidate.ProgressId,
                canonicalSession.Scenario.ProgressId,
                StringComparison.Ordinal));

        return new SessionViewModel
        {
            ScenarioDisplayName = scenario.DisplayName,
            Mode = canonicalSession.CurrentMode,
            LocationId = canonicalSession.CurrentLocationId,
            LocationDisplayName = FindLocationDisplayName(
                scenario,
                canonicalSession.CurrentLocationId),
            ProgressId = canonicalSession.Scenario.ProgressId,
            IsConcluded = canonicalSession.CurrentMode
                == ApplicationMode.ScenarioConclusion,
            IsSuccess = conclusion?.IsSuccess,
            Actions = Array.AsReadOnly(
                CreateActions(canonicalSession, scenario).ToArray())
        };
    }

    private static List<SessionAction> CreateActions(
        ApplicationSessionState session,
        ScenarioDefinition scenario)
    {
        List<SessionAction> actions = [];

        switch (session.CurrentMode)
        {
            case ApplicationMode.Outpost:
                AddOutpostActions(session, scenario, actions);
                break;
            case ApplicationMode.RegionalTravel:
                AddTravelActions(session, scenario, actions);
                break;
            case ApplicationMode.Exploration:
                AddExplorationActions(session, scenario, actions);
                break;
            default:
                // An encounter is driven through the combat facade, and a
                // concluded scenario offers nothing further.
                break;
        }

        return actions;
    }

    private static void AddOutpostActions(
        ApplicationSessionState session,
        ScenarioDefinition scenario,
        List<SessionAction> actions)
    {
        ScenarioDecisionDefinition? decision = scenario.Decisions
            .FirstOrDefault(candidate =>
                string.Equals(
                    candidate.LocationId,
                    session.CurrentLocationId,
                    StringComparison.Ordinal)
                && candidate.RequiredProgressIds.Contains(
                    session.Scenario.ProgressId,
                    StringComparer.Ordinal));

        if (decision is not null)
        {
            IReadOnlyList<OutpostMissionChoice> available =
                OutpostMissionRules.GetAvailableChoices(session);

            foreach (ScenarioDecisionOptionDefinition option in decision.Options)
            {
                if (!Enum.TryParse(
                        option.OptionId,
                        ignoreCase: false,
                        out OutpostMissionChoice choice)
                    || !available.Contains(choice))
                {
                    continue;
                }

                actions.Add(new SessionAction
                {
                    Kind = SessionActionKind.ResolveOutpostDecision,
                    DisplayName = option.DisplayName,
                    MissionChoice = choice
                });
            }
        }

        if (RegionalTravelRules.CanBeginJourney(session))
        {
            actions.Add(new SessionAction
            {
                Kind = SessionActionKind.BeginJourney,
                DisplayName = $"Set out for {FindRouteDestinationName(session, scenario)}"
            });
        }
    }

    private static void AddTravelActions(
        ApplicationSessionState session,
        ScenarioDefinition scenario,
        List<SessionAction> actions)
    {
        if (RegionalTravelRules.CanAdvance(session))
        {
            actions.Add(new SessionAction
            {
                Kind = SessionActionKind.AdvanceTravel,
                DisplayName = "Travel onward"
            });
        }

        if (ExplorationRules.CanEnterDestination(session))
        {
            actions.Add(new SessionAction
            {
                Kind = SessionActionKind.EnterDestination,
                DisplayName = $"Enter {FindLocationDisplayName(scenario, session.CurrentLocationId)}"
            });
        }
    }

    private static void AddExplorationActions(
        ApplicationSessionState session,
        ScenarioDefinition scenario,
        List<SessionAction> actions)
    {
        actions.Add(new SessionAction
        {
            Kind = SessionActionKind.MoveForward,
            DisplayName = "Move forward"
        });
        actions.Add(new SessionAction
        {
            Kind = SessionActionKind.TurnLeft,
            DisplayName = "Turn left"
        });
        actions.Add(new SessionAction
        {
            Kind = SessionActionKind.TurnRight,
            DisplayName = "Turn right"
        });

        if (ExplorationRules.CanUseStairs(session))
        {
            actions.Add(new SessionAction
            {
                Kind = SessionActionKind.UseStairs,
                DisplayName = "Take the stairs"
            });
        }

        if (ScenarioTriggerRules.CanActivate(session))
        {
            ScenarioTriggerDefinition? trigger =
                ScenarioTriggerMatcher.FindAvailable(session);

            if (trigger is not null)
            {
                actions.Add(new SessionAction
                {
                    Kind = SessionActionKind.ActivateTrigger,
                    DisplayName = trigger.DisplayName
                });
            }
        }
    }

    private static string FindLocationDisplayName(
        ScenarioDefinition scenario,
        string locationId)
    {
        return scenario.Locations
            .FirstOrDefault(location => string.Equals(
                location.LocationId,
                locationId,
                StringComparison.Ordinal))
            ?.DisplayName
            ?? locationId;
    }

    /// Where the open road leads, named as the scenario names it.
    private static string FindRouteDestinationName(
        ApplicationSessionState session,
        ScenarioDefinition scenario)
    {
        TravelRouteDefinition? route = scenario.Routes
            .FirstOrDefault(candidate =>
                candidate.RequiredProgressIds.Contains(
                    session.Scenario.ProgressId,
                    StringComparer.Ordinal));

        return route is null
            ? "the road"
            : FindLocationDisplayName(scenario, route.DestinationLocationId);
    }
}
