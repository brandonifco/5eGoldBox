using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Core.Tests;

/// The trigger is *leaving reach*, not moving while threatened. These pin
/// that distinction from both sides, because getting it backwards would tax
/// all movement rather than retreat specifically — which is the difference
/// between position mattering and melee being a trap.
public sealed class EncounterOpportunityAttackRulesTests
{
    private const string HeroId = "combatant.hero";
    private const string EnemyId = "combatant.enemy";
    private const string BystanderId = "combatant.bystander";

    [Fact]
    public void FindProvocations_LeavingReach_NamesTheAttackerAndItsMeleeWeapon()
    {
        EncounterState state = CreateEncounter(
            enemyPosition: new GridPosition(2, 1));

        IReadOnlyList<EncounterOpportunityAttack> provocations =
            EncounterOpportunityAttackRules.FindProvocations(
                state,
                HeroId,
                new GridPosition(1, 1),
                new GridPosition(0, 1));

        EncounterOpportunityAttack provocation = Assert.Single(provocations);

        Assert.Equal(EnemyId, provocation.AttackerCombatantId);
        Assert.Equal("weapon.longsword", provocation.WeaponId);
        Assert.Equal(new GridPosition(1, 1), provocation.FromPosition);
    }

    /// Sidestepping inside the same enemy's reach is free. This is the case
    /// that makes closing to melee and repositioning within it survivable.
    [Fact]
    public void FindProvocations_MovingWithinReach_ProvokesNothing()
    {
        EncounterState state = CreateEncounter(
            enemyPosition: new GridPosition(2, 1));

        Assert.Empty(
            EncounterOpportunityAttackRules.FindProvocations(
                state,
                HeroId,
                new GridPosition(1, 1),
                new GridPosition(1, 2)));
    }

    [Fact]
    public void FindProvocations_MovingIntoReach_ProvokesNothing()
    {
        EncounterState state = CreateEncounter(
            enemyPosition: new GridPosition(3, 1));

        Assert.Empty(
            EncounterOpportunityAttackRules.FindProvocations(
                state,
                HeroId,
                new GridPosition(1, 1),
                new GridPosition(2, 1)));
    }

    /// The whole counterplay: without this, leaving a melee is a free hit
    /// with no answer to it.
    [Fact]
    public void FindProvocations_AfterDisengaging_ProvokesNothing()
    {
        EncounterState state = WithHeroDisengaged(
            CreateEncounter(enemyPosition: new GridPosition(2, 1)));

        Assert.Empty(
            EncounterOpportunityAttackRules.FindProvocations(
                state,
                HeroId,
                new GridPosition(1, 1),
                new GridPosition(0, 1)));
    }

    [Fact]
    public void FindProvocations_AttackerWithoutAReaction_ProvokesNothing()
    {
        EncounterState state = WithEnemyReactionSpent(
            CreateEncounter(enemyPosition: new GridPosition(2, 1)));

        Assert.Empty(
            EncounterOpportunityAttackRules.FindProvocations(
                state,
                HeroId,
                new GridPosition(1, 1),
                new GridPosition(0, 1)));
    }

    /// An archer threatens nobody. 5e's opportunity attack is explicitly one
    /// *melee* attack, so a combatant carrying only a ranged weapon has
    /// nothing to swing even at an adjacent target walking away.
    [Fact]
    public void FindProvocations_AttackerWithOnlyARangedWeapon_ProvokesNothing()
    {
        EncounterState state = CreateEncounter(
            enemyPosition: new GridPosition(2, 1),
            enemyWeapon: CreateWeapon(
                "weapon.shortbow",
                WeaponAttackKind.Ranged,
                reachFeet: null,
                normalRangeFeet: 80,
                longRangeFeet: 320));

        Assert.Empty(
            EncounterOpportunityAttackRules.FindProvocations(
                state,
                HeroId,
                new GridPosition(1, 1),
                new GridPosition(0, 1)));
    }

    /// An ally stepping away is not an opening — only a hostile provokes.
    [Fact]
    public void FindProvocations_SameSide_ProvokesNothing()
    {
        EncounterState state = CreateEncounter(
            enemyPosition: new GridPosition(2, 1),
            enemySideId: "side.party");

        Assert.Empty(
            EncounterOpportunityAttackRules.FindProvocations(
                state,
                HeroId,
                new GridPosition(1, 1),
                new GridPosition(0, 1)));
    }

    /// A reach weapon threatens further out, so the square that escapes it
    /// is further out too. Pins that the rule reads the weapon's own reach
    /// rather than assuming five feet.
    [Fact]
    public void FindProvocations_ReachWeapon_TriggersOnLeavingTheLongerReach()
    {
        EncounterState state = CreateEncounter(
            enemyPosition: new GridPosition(3, 1),
            enemyWeapon: CreateWeapon(
                "weapon.halberd",
                WeaponAttackKind.Melee,
                reachFeet: 10));

        // Two squares away is still inside a ten-foot reach, so stepping
        // from there to three squares away is what actually escapes it.
        Assert.Single(
            EncounterOpportunityAttackRules.FindProvocations(
                state,
                HeroId,
                new GridPosition(1, 1),
                new GridPosition(0, 1)));
    }

    [Fact]
    public void FindProvocations_UnknownMover_ReturnsEmptyRatherThanThrowing()
    {
        EncounterState state = CreateEncounter(
            enemyPosition: new GridPosition(2, 1));

        Assert.Empty(
            EncounterOpportunityAttackRules.FindProvocations(
                state,
                "combatant.not-here",
                new GridPosition(1, 1),
                new GridPosition(0, 1)));
    }

    private static EncounterState WithHeroDisengaged(EncounterState state)
    {
        return WithParticipant(
            state,
            HeroId,
            participant => participant with
            {
                TurnResources = CombatTurnResourceRules.Disengage(
                    participant.TurnResources)
            });
    }

    private static EncounterState WithEnemyReactionSpent(EncounterState state)
    {
        return WithParticipant(
            state,
            EnemyId,
            participant => participant with
            {
                TurnResources = CombatTurnResourceRules.SpendReaction(
                    participant.TurnResources)
            });
    }

    private static EncounterState WithParticipant(
        EncounterState state,
        string combatantId,
        Func<EncounterParticipantState, EncounterParticipantState> change)
    {
        EncounterParticipantState[] participants =
            state.Participants.ToArray();
        int index = Array.FindIndex(
            participants,
            participant => string.Equals(
                participant.Combatant.CombatantId,
                combatantId,
                StringComparison.Ordinal));

        participants[index] = change(participants[index]);

        return state with
        {
            Participants = Array.AsReadOnly(participants)
        };
    }

    private static EncounterState CreateEncounter(
        GridPosition enemyPosition,
        WeaponAttack? enemyWeapon = null,
        string enemySideId = "side.enemies")
    {
        EncounterParticipantSetup[] participants =
        [
            CreateParticipant(
                HeroId,
                "side.party",
                new GridPosition(1, 1),
                CreateWeapon("weapon.dagger", WeaponAttackKind.Melee)),
            CreateParticipant(
                EnemyId,
                enemySideId,
                enemyPosition,
                enemyWeapon
                    ?? CreateWeapon(
                        "weapon.longsword",
                        WeaponAttackKind.Melee)),
            // Far enough away to threaten nothing, and present only so an
            // encounter always has the two sides EncounterRules requires --
            // the same-side case moves the real threat onto the party's
            // side, which would otherwise leave a one-sided encounter.
            CreateParticipant(
                BystanderId,
                "side.enemies",
                new GridPosition(11, 11),
                CreateWeapon("weapon.club", WeaponAttackKind.Melee))
        ];

        InitiativeOrderEntry[] initiativeOrder =
        [
            CreateInitiativeEntry(HeroId, position: 1, total: 15),
            CreateInitiativeEntry(EnemyId, position: 2, total: 10),
            CreateInitiativeEntry(BystanderId, position: 3, total: 5)
        ];

        return EncounterRules.Start(
            "encounter.test",
            new EncounterBattlefieldState
            {
                BattlefieldId = "battlefield.test",
                Width = 12,
                Height = 12,
                BlockedPositions = Array.Empty<GridPosition>(),
                DifficultTerrainPositions = Array.Empty<GridPosition>()
            },
            participants,
            initiativeOrder);
    }

    private static InitiativeOrderEntry CreateInitiativeEntry(
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

    private static EncounterParticipantSetup CreateParticipant(
        string combatantId,
        string sideId,
        GridPosition position,
        WeaponAttack weapon)
    {
        return new EncounterParticipantSetup
        {
            Combatant = CombatantRules.Create(
                combatantId,
                maximumHitPoints: 10,
                CombatantZeroHitPointPolicy.DeathSavingThrows),
            CombatProfile = new EncounterCombatProfile
            {
                ArmorClass = 10,
                WeaponAttacks = Array.AsReadOnly(new[] { weapon })
            },
            SideId = sideId,
            MovementSpeedFeet = 30,
            StartingPosition = position
        };
    }

    private static WeaponAttack CreateWeapon(
        string weaponId,
        WeaponAttackKind attackKind,
        int? reachFeet = null,
        int? normalRangeFeet = null,
        int? longRangeFeet = null)
    {
        return new WeaponAttack
        {
            WeaponId = weaponId,
            WeaponName = weaponId,
            Category = WeaponCategory.Martial,
            AttackKind = attackKind,
            AttackAbility = Ability.Strength,
            AbilityModifier = 3,
            IsProficient = true,
            ProficiencyBonus = 2,
            AttackBonus = 5,
            HasDisadvantage = false,
            DisadvantageReasons = Array.Empty<string>(),
            AttackRollMode = D20RollMode.Normal,
            Damage = new DamageDice
            {
                Count = 1,
                Die = DieType.D8
            },
            VersatileDamage = null,
            DamageType = "damage.slashing",
            DamageBonus = 3,
            Properties = Array.Empty<string>(),
            ReachFeet = reachFeet,
            NormalRangeFeet = normalRangeFeet,
            LongRangeFeet = longRangeFeet,
            AmmunitionItemId = null,
            AmmunitionQuantityAvailable = null
        };
    }
}
