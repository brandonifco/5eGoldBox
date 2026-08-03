using FiveEGoldBox.Application.Exploration;
using FiveEGoldBox.Application.Parties;
using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Application.Travel;
using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Persistence.V1;

internal static class SaveGameMapper
{
    internal const int FormatVersion = 1;

    internal static SaveGameV1 ToSaveV1(
        ApplicationSessionState session)
    {
        return new SaveGameV1
        {
            FormatVersion = FormatVersion,
            Session = ToSaveSession(session)
        };
    }

    internal static ApplicationSessionState ToRuntime(
        SaveGameV1 saveGame)
    {
        ArgumentNullException.ThrowIfNull(saveGame);
        ArgumentNullException.ThrowIfNull(saveGame.Session);

        SaveSessionV1 session = saveGame.Session;

        if (session.ActiveEncounter is not null)
        {
            throw new ArgumentException(
                "Active-encounter save data is not supported in this application phase.",
                nameof(saveGame));
        }

        return new ApplicationSessionState
        {
            ScenarioId = session.ScenarioId,
            CurrentMode = ToRuntimeMode(session.CurrentMode),
            CurrentLocationId = session.CurrentLocationId,
            Party = ToRuntimeParty(session.Party),
            Scenario = ToRuntimeScenario(session.Scenario),
            RandomSeed = session.RandomSeed,
            RandomValuesConsumed = session.RandomValuesConsumed,
            RegionalTravel = session.RegionalTravel is null
                ? null
                : ToRuntimeRegionalTravel(session.RegionalTravel),
            Exploration = session.Exploration is null
                ? null
                : ToRuntimeExploration(session.Exploration),
            ActiveEncounter = null
        };
    }

    private static SaveSessionV1 ToSaveSession(
        ApplicationSessionState session)
    {
        return new SaveSessionV1
        {
            ScenarioId = session.ScenarioId,
            CurrentMode = ToSaveMode(session.CurrentMode),
            CurrentLocationId = session.CurrentLocationId,
            Party = ToSaveParty(session.Party),
            Scenario = ToSaveScenario(session.Scenario),
            RandomSeed = session.RandomSeed,
            RandomValuesConsumed = session.RandomValuesConsumed,
            RegionalTravel = session.RegionalTravel is null
                ? null
                : ToSaveRegionalTravel(session.RegionalTravel),
            Exploration = session.Exploration is null
                ? null
                : ToSaveExploration(session.Exploration),
            ActiveEncounter = null
        };
    }

    private static SavePartyV1 ToSaveParty(
        PartyState party)
    {
        return new SavePartyV1
        {
            PartyId = party.PartyId,
            Members = party.Members
                .Select(ToSaveMember)
                .ToArray(),
            Currency = ToSaveCurrency(party.Currency),
            InventoryItems = party.InventoryItems
                .Select(item => new SavePartyInventoryItemV1
                {
                    ItemId = item.ItemId,
                    Quantity = item.Quantity
                })
                .ToArray()
        };
    }

    private static PartyState ToRuntimeParty(
        SavePartyV1 party)
    {
        ArgumentNullException.ThrowIfNull(party.Members);

        return new PartyState
        {
            PartyId = party.PartyId,
            Members = party.Members
                .Select(ToRuntimeMember)
                .ToArray(),
            Currency = ToRuntimeCurrency(
                party.Currency ?? new SaveCurrencyV1()),
            InventoryItems = (party.InventoryItems
                    ?? Array.Empty<SavePartyInventoryItemV1>())
                .Select(item => new PartyInventoryItemState
                {
                    ItemId = item.ItemId,
                    Quantity = item.Quantity
                })
                .ToArray()
        };
    }

    private static SaveCurrencyV1 ToSaveCurrency(
        CurrencyAmount currency)
    {
        return new SaveCurrencyV1
        {
            CopperPieces = currency.CopperPieces,
            SilverPieces = currency.SilverPieces,
            ElectrumPieces = currency.ElectrumPieces,
            GoldPieces = currency.GoldPieces,
            PlatinumPieces = currency.PlatinumPieces
        };
    }

    private static CurrencyAmount ToRuntimeCurrency(
        SaveCurrencyV1 currency)
    {
        return new CurrencyAmount
        {
            CopperPieces = currency.CopperPieces,
            SilverPieces = currency.SilverPieces,
            ElectrumPieces = currency.ElectrumPieces,
            GoldPieces = currency.GoldPieces,
            PlatinumPieces = currency.PlatinumPieces
        };
    }

    private static SavePartyMemberV1 ToSaveMember(
        PartyMemberState member)
    {
        return new SavePartyMemberV1
        {
            PartyMemberId = member.PartyMemberId,
            CharacterDefinitionId = member.CharacterDefinitionId,
            DisplayName = member.DisplayName,
            ClassId = member.ClassId,
            ZeroHitPointPolicy = ToSaveZeroHitPointPolicy(
                member.ZeroHitPointPolicy),
            Health = ToSaveHealth(member.Health),
            Ammunition = member.Ammunition is null
                ? null
                : ToSaveAmmunition(member.Ammunition),
            Resources = member.Resources
                .Select(resource => new SaveCharacterResourceV1
                {
                    ResourceId = resource.ResourceId,
                    Remaining = resource.Remaining,
                    Maximum = resource.Maximum
                })
                .ToArray()
        };
    }

    private static PartyMemberState ToRuntimeMember(
        SavePartyMemberV1 member)
    {
        return new PartyMemberState
        {
            PartyMemberId = member.PartyMemberId,
            CharacterDefinitionId = member.CharacterDefinitionId,
            DisplayName = member.DisplayName,
            ClassId = member.ClassId,
            ZeroHitPointPolicy = ToRuntimeZeroHitPointPolicy(
                member.ZeroHitPointPolicy),
            Health = ToRuntimeHealth(member.Health),
            Ammunition = member.Ammunition is null
                ? null
                : ToRuntimeAmmunition(member.Ammunition),
            Resources = (member.Resources
                    ?? Array.Empty<SaveCharacterResourceV1>())
                .Select(resource => new CharacterResourceState
                {
                    ResourceId = resource.ResourceId,
                    Remaining = resource.Remaining,
                    Maximum = resource.Maximum
                })
                .ToArray()
        };
    }

    private static SaveHealthV1 ToSaveHealth(
        CombatantHealthState health)
    {
        return new SaveHealthV1
        {
            HitPoints = new SaveHitPointsV1
            {
                MaximumHitPoints =
                    health.HitPoints.MaximumHitPoints,
                CurrentHitPoints =
                    health.HitPoints.CurrentHitPoints,
                TemporaryHitPoints =
                    health.HitPoints.TemporaryHitPoints
            },
            DeathSavingThrows = new SaveDeathSavingThrowsV1
            {
                SuccessCount =
                    health.DeathSavingThrows.SuccessCount,
                FailureCount =
                    health.DeathSavingThrows.FailureCount,
                IsStable =
                    health.DeathSavingThrows.IsStable
            },
            IsInstantlyDead = health.IsInstantlyDead
        };
    }

    private static CombatantHealthState ToRuntimeHealth(
        SaveHealthV1 health)
    {
        ArgumentNullException.ThrowIfNull(health.HitPoints);
        ArgumentNullException.ThrowIfNull(health.DeathSavingThrows);

        return new CombatantHealthState
        {
            HitPoints = new HitPointState
            {
                MaximumHitPoints =
                    health.HitPoints.MaximumHitPoints,
                CurrentHitPoints =
                    health.HitPoints.CurrentHitPoints,
                TemporaryHitPoints =
                    health.HitPoints.TemporaryHitPoints
            },
            DeathSavingThrows = new DeathSavingThrowState
            {
                SuccessCount =
                    health.DeathSavingThrows.SuccessCount,
                FailureCount =
                    health.DeathSavingThrows.FailureCount,
                IsStable =
                    health.DeathSavingThrows.IsStable
            },
            IsInstantlyDead = health.IsInstantlyDead
        };
    }

    private static SaveAmmunitionV1 ToSaveAmmunition(
        AmmunitionState ammunition)
    {
        return new SaveAmmunitionV1
        {
            WeaponId = ammunition.WeaponId,
            AmmunitionItemId = ammunition.AmmunitionItemId,
            RemainingQuantity = ammunition.RemainingQuantity
        };
    }

    private static AmmunitionState ToRuntimeAmmunition(
        SaveAmmunitionV1 ammunition)
    {
        return new AmmunitionState
        {
            WeaponId = ammunition.WeaponId,
            AmmunitionItemId = ammunition.AmmunitionItemId,
            RemainingQuantity = ammunition.RemainingQuantity
        };
    }

    // The marker is stored as written. Which markers a scenario uses is the
    // scenario's business, and the session is validated against its own
    // definition on load, so the save format does not need to know them.
    private static SaveScenarioStateV1 ToSaveScenario(
        ScenarioState scenario)
    {
        return new SaveScenarioStateV1
        {
            Progress = scenario.ProgressId
        };
    }

    private static ScenarioState ToRuntimeScenario(
        SaveScenarioStateV1 scenario)
    {
        return new ScenarioState
        {
            ProgressId = scenario.Progress
        };
    }

    private static SaveRegionalTravelV1 ToSaveRegionalTravel(
        RegionalTravelState travel)
    {
        return new SaveRegionalTravelV1
        {
            RouteId = travel.RouteId,
            OriginLocationId = travel.OriginLocationId,
            DestinationLocationId = travel.DestinationLocationId,
            CurrentStepIndex = travel.CurrentStepIndex,
            FinalStepIndex = travel.FinalStepIndex
        };
    }

    private static RegionalTravelState ToRuntimeRegionalTravel(
        SaveRegionalTravelV1 travel)
    {
        return new RegionalTravelState
        {
            RouteId = travel.RouteId,
            OriginLocationId = travel.OriginLocationId,
            DestinationLocationId = travel.DestinationLocationId,
            CurrentStepIndex = travel.CurrentStepIndex,
            FinalStepIndex = travel.FinalStepIndex
        };
    }

    private static SaveExplorationV1 ToSaveExploration(
        ExplorationState exploration)
    {
        return new SaveExplorationV1
        {
            MapId = exploration.MapId,
            Floor = exploration.Floor,
            Position = new SaveGridPositionV1
            {
                X = exploration.Position.X,
                Y = exploration.Position.Y
            },
            Facing = ToSaveFacing(exploration.Facing),
            OpenDoorIds = exploration.OpenDoorIds,
            RevealedSecretDoorIds = exploration.RevealedSecretDoorIds,
            CollectedTreasureIds = exploration.CollectedTreasureIds
        };
    }

    private static ExplorationState ToRuntimeExploration(
        SaveExplorationV1 exploration)
    {
        ArgumentNullException.ThrowIfNull(exploration.Position);

        return new ExplorationState
        {
            MapId = exploration.MapId,
            Floor = exploration.Floor,
            Position = new GridPosition(
                exploration.Position.X,
                exploration.Position.Y),
            Facing = ToRuntimeFacing(exploration.Facing),
            OpenDoorIds = exploration.OpenDoorIds,
            RevealedSecretDoorIds = exploration.RevealedSecretDoorIds,
            CollectedTreasureIds = exploration.CollectedTreasureIds
        };
    }

    private static SaveApplicationModeV1 ToSaveMode(
        ApplicationMode mode)
    {
        return mode switch
        {
            ApplicationMode.Outpost =>
                SaveApplicationModeV1.Outpost,
            ApplicationMode.RegionalTravel =>
                SaveApplicationModeV1.RegionalTravel,
            ApplicationMode.Exploration =>
                SaveApplicationModeV1.Exploration,
            ApplicationMode.Encounter =>
                SaveApplicationModeV1.Encounter,
            ApplicationMode.ScenarioConclusion =>
                SaveApplicationModeV1.ScenarioConclusion,
            _ => throw new ArgumentOutOfRangeException(
                nameof(mode),
                mode,
                "Unsupported application mode.")
        };
    }

    private static ApplicationMode ToRuntimeMode(
        SaveApplicationModeV1 mode)
    {
        return mode switch
        {
            SaveApplicationModeV1.Outpost =>
                ApplicationMode.Outpost,
            SaveApplicationModeV1.RegionalTravel =>
                ApplicationMode.RegionalTravel,
            SaveApplicationModeV1.Exploration =>
                ApplicationMode.Exploration,
            SaveApplicationModeV1.Encounter =>
                ApplicationMode.Encounter,
            SaveApplicationModeV1.ScenarioConclusion =>
                ApplicationMode.ScenarioConclusion,
            _ => throw new ArgumentOutOfRangeException(
                nameof(mode),
                mode,
                "Unsupported saved application mode.")
        };
    }

    private static SaveZeroHitPointPolicyV1 ToSaveZeroHitPointPolicy(
        CombatantZeroHitPointPolicy policy)
    {
        return policy switch
        {
            CombatantZeroHitPointPolicy.DeathSavingThrows =>
                SaveZeroHitPointPolicyV1.DeathSavingThrows,
            CombatantZeroHitPointPolicy.Defeated =>
                SaveZeroHitPointPolicyV1.Defeated,
            _ => throw new ArgumentOutOfRangeException(
                nameof(policy),
                policy,
                "Unsupported zero-hit-point policy.")
        };
    }

    private static CombatantZeroHitPointPolicy ToRuntimeZeroHitPointPolicy(
        SaveZeroHitPointPolicyV1 policy)
    {
        return policy switch
        {
            SaveZeroHitPointPolicyV1.DeathSavingThrows =>
                CombatantZeroHitPointPolicy.DeathSavingThrows,
            SaveZeroHitPointPolicyV1.Defeated =>
                CombatantZeroHitPointPolicy.Defeated,
            _ => throw new ArgumentOutOfRangeException(
                nameof(policy),
                policy,
                "Unsupported saved zero-hit-point policy.")
        };
    }


    private static SaveExplorationFacingV1 ToSaveFacing(
        ExplorationFacing facing)
    {
        return facing switch
        {
            ExplorationFacing.North =>
                SaveExplorationFacingV1.North,
            ExplorationFacing.East =>
                SaveExplorationFacingV1.East,
            ExplorationFacing.South =>
                SaveExplorationFacingV1.South,
            ExplorationFacing.West =>
                SaveExplorationFacingV1.West,
            _ => throw new ArgumentOutOfRangeException(
                nameof(facing),
                facing,
                "Unsupported exploration facing.")
        };
    }

    private static ExplorationFacing ToRuntimeFacing(
        SaveExplorationFacingV1 facing)
    {
        return facing switch
        {
            SaveExplorationFacingV1.North =>
                ExplorationFacing.North,
            SaveExplorationFacingV1.East =>
                ExplorationFacing.East,
            SaveExplorationFacingV1.South =>
                ExplorationFacing.South,
            SaveExplorationFacingV1.West =>
                ExplorationFacing.West,
            _ => throw new ArgumentOutOfRangeException(
                nameof(facing),
                facing,
                "Unsupported saved exploration facing.")
        };
    }
}
