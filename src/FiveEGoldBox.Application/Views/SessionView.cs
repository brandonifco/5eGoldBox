using FiveEGoldBox.Application.Campaigns;
using FiveEGoldBox.Application.Exploration;
using FiveEGoldBox.Application.Outposts;
using FiveEGoldBox.Application.Parties;
using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Scenarios.Definitions;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Application.Travel;
using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

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
            ConclusionText = conclusion?.EpilogueText,
            Actions = Array.AsReadOnly(
                CreateActions(canonicalSession, scenario).ToArray()),
            Party = DescribeParty(canonicalSession.Party, scenario)
        };
    }

    private static PartyViewModel DescribeParty(
        PartyState party,
        ScenarioDefinition scenario)
    {
        ValidatedRuleset ruleset = RulesetRegistry.Resolve(scenario.RulesetId);
        CampaignDefinition campaign =
            CampaignRegistry.Resolve(FrontierCampaignIds.CampaignId);

        return new PartyViewModel
        {
            Members = Array.AsReadOnly(party.Members
                .Select(member => DescribePartyMember(
                    member,
                    campaign,
                    ruleset))
                .ToArray()),
            Currency = DescribeCurrency(party.Currency),
            InventoryItems = Array.AsReadOnly(party.InventoryItems
                .Select(item => new PartyInventoryItemViewModel
                {
                    ItemId = item.ItemId,
                    DisplayName = FindItemDisplayName(ruleset, item.ItemId),
                    Quantity = item.Quantity
                })
                .ToArray())
        };
    }

    /// Resolves the same CharacterSnapshot PartyEncounterMapper resolves for
    /// combat -- ability scores, armor class, and carrying capacity are all
    /// already computed there and simply never crossed this read-only
    /// boundary before now.
    private static PartyMemberViewModel DescribePartyMember(
        PartyMemberState member,
        CampaignDefinition campaign,
        ValidatedRuleset ruleset)
    {
        CharacterDraft draft = CampaignCharacterDraftFactory.CreateDraft(
            member,
            campaign,
            ruleset);
        CharacterSnapshot snapshot = new CharacterResolver(ruleset).Resolve(draft);

        return new PartyMemberViewModel
        {
            PartyMemberId = member.PartyMemberId,
            DisplayName = member.DisplayName,
            RaceDisplayName = snapshot.RaceName ?? "Unknown",
            ClassDisplayName = FindClassDisplayName(ruleset, member.ClassId),
            Level = snapshot.Level,
            CurrentHitPoints = member.Health.HitPoints.CurrentHitPoints,
            MaximumHitPoints = member.Health.HitPoints.MaximumHitPoints,
            ArmorClass = snapshot.ArmorClass ?? 10,
            AbilityScores = Array.AsReadOnly(Enum.GetValues<Ability>()
                .Select(ability => new AbilityScoreViewModel
                {
                    AbilityName = ability.ToString(),
                    Score = snapshot.AbilityScores[ability],
                    Modifier = snapshot.AbilityModifiers[ability]
                })
                .ToArray()),
            CarryingCapacityPounds = snapshot.CarryingCapacityPounds,
            TotalCarriedWeightPounds = snapshot.TotalCarriedWeightPounds,
            IsOverCarryingCapacity = snapshot.IsOverCarryingCapacity,
            PreparedSpells = Array.AsReadOnly(snapshot.SpellAttacks
                .Select(DescribePreparedSpell)
                .ToArray())
        };
    }

    private static PreparedSpellViewModel DescribePreparedSpell(
        SpellAttack spell)
    {
        string resolutionSummary = spell.Resolution switch
        {
            SpellResolutionKind.SpellAttack => $"Attack +{spell.AttackBonus}",
            SpellResolutionKind.SavingThrow =>
                $"DC {spell.SaveDc} {spell.SaveAbility} save",
            SpellResolutionKind.Automatic => "Automatic",
            _ => spell.Resolution.ToString()
        };

        string range = spell.RangeKind switch
        {
            SpellRangeKind.SelfOnly => "Self",
            SpellRangeKind.Touch => "Touch",
            SpellRangeKind.Ranged => $"{spell.RangeFeet} ft.",
            _ => spell.RangeKind.ToString()
        };

        string castingTime = spell.CastingTime switch
        {
            SpellCastingTime.Action => "Action",
            SpellCastingTime.BonusAction => "Bonus Action",
            SpellCastingTime.Reaction => "Reaction",
            _ => spell.CastingTime.ToString()
        };

        return new PreparedSpellViewModel
        {
            SpellId = spell.SpellId,
            SpellName = spell.SpellName,
            Level = spell.Level,
            CastingTime = castingTime,
            Range = range,
            ResolutionSummary = resolutionSummary,
            RequiresConcentration = spell.RequiresConcentration
        };
    }

    private static string FindClassDisplayName(
        ValidatedRuleset ruleset,
        string classId)
    {
        return ruleset.Definition.Classes
            .FirstOrDefault(candidate => string.Equals(
                candidate.Id,
                classId,
                StringComparison.Ordinal))
            ?.Name
            ?? classId;
    }

    private static CurrencyViewModel DescribeCurrency(
        CurrencyAmount currency)
    {
        return new CurrencyViewModel
        {
            CopperPieces = currency.CopperPieces,
            SilverPieces = currency.SilverPieces,
            ElectrumPieces = currency.ElectrumPieces,
            GoldPieces = currency.GoldPieces,
            PlatinumPieces = currency.PlatinumPieces
        };
    }

    private static string FindItemDisplayName(
        ValidatedRuleset ruleset,
        string itemId)
    {
        return ruleset.Definition.EquipmentItems
            .FirstOrDefault(item => string.Equals(
                item.Id,
                itemId,
                StringComparison.Ordinal))
            ?.Name
            ?? itemId;
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
            foreach (ScenarioDecisionOptionDefinition option in decision.Options)
            {
                actions.Add(new SessionAction
                {
                    Kind = SessionActionKind.ResolveOutpostDecision,
                    DisplayName = option.DisplayName,
                    DecisionOptionId = option.OptionId
                });
            }
        }

        foreach (TravelRouteDefinition route
            in RegionalTravelRules.GetAvailableRoutes(session))
        {
            string destinationDisplayName = FindLocationDisplayName(
                scenario,
                route.DestinationLocationId);

            actions.Add(new SessionAction
            {
                Kind = SessionActionKind.BeginJourney,
                DisplayName = $"Set out for {destinationDisplayName}",
                RouteId = route.RouteId,
                DestinationDisplayName = destinationDisplayName
            });
        }

        ShopDefinition? shop = ShopRules.FindShopHere(session);

        if (shop is not null)
        {
            ValidatedRuleset ruleset = RulesetRegistry.Resolve(scenario.RulesetId);

            foreach (ShopItemDefinition item
                in shop.Items.Where(item =>
                    ShopRules.CanPurchase(session, item.ItemId)))
            {
                string itemDisplayName = FindItemDisplayName(
                    ruleset,
                    item.ItemId);

                actions.Add(new SessionAction
                {
                    Kind = SessionActionKind.PurchaseShopItem,
                    DisplayName = $"Buy {itemDisplayName} ({item.PriceGoldPieces} gp)",
                    ShopItemId = item.ItemId
                });
            }
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

        if (ExplorationRules.CanRevealSecretDoor(session))
        {
            actions.Add(new SessionAction
            {
                Kind = SessionActionKind.RevealSecretDoor,
                DisplayName = "Search for a hidden door"
            });
        }

        if (ExplorationRules.CanCollectTreasure(session))
        {
            actions.Add(new SessionAction
            {
                Kind = SessionActionKind.CollectTreasure,
                DisplayName = "Collect the treasure"
            });
        }

        if (ExplorationRules.CanTalkToNpc(session))
        {
            NpcDefinition? npc = ExplorationRules.FindNpcToTalkTo(
                ApplicationSessionRules.CreateCanonical(session));

            actions.Add(new SessionAction
            {
                Kind = SessionActionKind.TalkToNpc,
                DisplayName = npc is null
                    ? "Talk"
                    : $"Talk to {npc.Name}"
            });
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

}
