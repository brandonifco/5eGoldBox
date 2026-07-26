using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Core.Tests;

/// Fixtures shared by the Start and Complete halves of these tests.
public sealed partial class EncounterRulesTests
{
    private static EncounterParticipantSetup[]
        CreateParticipants()
    {
        return
        [
            CreateParticipant(
            combatantId: "combatant.hero",
            sideId: "side.party"),
        CreateParticipant(
            combatantId: "combatant.enemy",
            sideId: "side.enemies")
        ];
    }

    private static EncounterParticipantSetup
       CreateParticipant(
           string combatantId,
           string sideId,
           int movementSpeedFeet = 30,
           GridPosition? startingPosition = null)
    {
        return new EncounterParticipantSetup
        {
            Combatant = CombatantRules.Create(
                combatantId,
                maximumHitPoints: 10,
                CombatantZeroHitPointPolicy
                    .DeathSavingThrows),
            CombatProfile = new EncounterCombatProfile
            {
                ArmorClass = 10
            },
            SideId = sideId,
            MovementSpeedFeet = movementSpeedFeet,
            StartingPosition =
                startingPosition
                ?? (sideId == "side.enemies"
                    ? new GridPosition(2, 1)
                    : new GridPosition(1, 1))
        };
    }

    private static SavingThrowBonus
        CreateSavingThrowBonus(
            Ability ability,
            int totalBonus)
    {
        return CreateSavingThrowBonus(
            ability,
            abilityModifier: totalBonus,
            isProficient: false,
            proficiencyBonus: 0,
            totalBonus);
    }

    private static SavingThrowBonus
        CreateSavingThrowBonus(
            Ability ability,
            int abilityModifier,
            bool isProficient,
            int proficiencyBonus,
            int totalBonus)
    {
        return new SavingThrowBonus
        {
            Ability = ability,
            AbilityModifier = abilityModifier,
            IsProficient = isProficient,
            ProficiencyBonus = proficiencyBonus,
            TotalBonus = totalBonus
        };
    }

    private static InitiativeOrderEntry[]
        CreateInitiativeOrder()
    {
        return
        [
            CreateInitiativeEntry(
            combatantId: "combatant.hero",
            position: 1,
            total: 15),
        CreateInitiativeEntry(
            combatantId: "combatant.enemy",
            position: 2,
            total: 10)
        ];
    }

    private static InitiativeOrderEntry
        CreateInitiativeEntry(
            string combatantId,
            int position,
            int total)
    {
        return new InitiativeOrderEntry
        {
            CombatantId = combatantId,
            Initiative = InitiativeRules.ResolveInitiative(
                D20RollMode.Normal,
                firstRoll: total,
                secondRoll: null,
                initiativeBonus: 0),
            Position = position,
            HasTiedInitiative = false
        };
    }

    private static EncounterState StartEncounter(
    string encounterId,
    IReadOnlyList<EncounterParticipantSetup> participants,
    IReadOnlyList<InitiativeOrderEntry> initiativeOrder)
    {
        return EncounterRules.Start(
            encounterId,
            CreateBattlefield(),
            participants,
            initiativeOrder);
    }

    private static EncounterBattlefieldState CreateBattlefield(
        string battlefieldId = "battlefield.test",
        int width = 12,
        int height = 12,
        IReadOnlyList<GridPosition>? blockedPositions = null,
        IReadOnlyList<EncounterCoverPosition>?
            coverPositions = null,
        IReadOnlyList<GridPosition>? difficultTerrainPositions = null)
    {
        return new EncounterBattlefieldState
        {
            BattlefieldId = battlefieldId,
            Width = width,
            Height = height,
            BlockedPositions =
                blockedPositions
                ?? Array.Empty<GridPosition>(),
            CoverPositions =
                coverPositions
                ?? Array.Empty<EncounterCoverPosition>(),
            DifficultTerrainPositions =
                difficultTerrainPositions
                ?? Array.Empty<GridPosition>()
        };
    }
}
