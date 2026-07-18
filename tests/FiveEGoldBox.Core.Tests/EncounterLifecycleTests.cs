using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Core.Tests;

public sealed class EncounterLifecycleTests
{
    private const string VanguardId =
        "combatant.vanguard";

    private const string ArcherId =
        "combatant.archer";

    private const string RaiderId =
        "combatant.raider";

    private const string PartySideId =
        "side.party";

    private const string EnemySideId =
        "side.enemies";

    private const string VanguardWeaponId =
        "weapon.vanguard_blade";

    private const string ArcherWeaponId =
        "weapon.shortbow";

    private const string RaiderWeaponId =
        "weapon.raider_club";

    [Fact]
    public void CompleteEncounter_ThroughDiscoveredPublicActions_PreservesParticipantConsequences()
    {
        WeaponAttack vanguardWeapon =
            CreateMeleeWeapon(
                weaponId: VanguardWeaponId,
                weaponName: "Vanguard Blade",
                attackBonus: 5,
                damageDie: DieType.D6,
                damageBonus: 1,
                damageType: "damage.slashing");

        WeaponAttack archerWeapon =
            CreateRangedWeapon(
                weaponId: ArcherWeaponId,
                weaponName: "Shortbow",
                attackBonus: 5,
                damageDie: DieType.D6,
                damageBonus: 1,
                damageType: "damage.piercing",
                ammunitionItemId: "item.arrow",
                ammunitionQuantityAvailable: 3);

        WeaponAttack raiderWeapon =
            CreateMeleeWeapon(
                weaponId: RaiderWeaponId,
                weaponName: "Raider Club",
                attackBonus: 4,
                damageDie: DieType.D4,
                damageBonus: 0,
                damageType: "damage.bludgeoning");

        EncounterParticipantSetup[] participants =
        [
            CreateParticipant(
                combatantId: VanguardId,
                sideId: PartySideId,
                position: new GridPosition(0, 1),
                maximumHitPoints: 10,
                zeroHitPointPolicy:
                    CombatantZeroHitPointPolicy
                        .DeathSavingThrows,
                armorClass: 12,
                weapon: vanguardWeapon),
            CreateParticipant(
                combatantId: ArcherId,
                sideId: PartySideId,
                position: new GridPosition(0, 2),
                maximumHitPoints: 10,
                zeroHitPointPolicy:
                    CombatantZeroHitPointPolicy
                        .DeathSavingThrows,
                armorClass: 12,
                weapon: archerWeapon),
            CreateParticipant(
                combatantId: RaiderId,
                sideId: EnemySideId,
                position: new GridPosition(2, 1),
                maximumHitPoints: 9,
                zeroHitPointPolicy:
                    CombatantZeroHitPointPolicy
                        .Defeated,
                armorClass: 10,
                weapon: raiderWeapon)
        ];

        InitiativeOrderEntry[] initiativeOrder =
        [
            CreateInitiativeEntry(
                combatantId: VanguardId,
                position: 1,
                total: 18),
            CreateInitiativeEntry(
                combatantId: RaiderId,
                position: 2,
                total: 14),
            CreateInitiativeEntry(
                combatantId: ArcherId,
                position: 3,
                total: 10)
        ];

        EncounterState state = EncounterRules.Start(
            encounterId: "encounter.lifecycle",
            battlefield: CreateBattlefield(),
            participants: participants,
            initiativeOrder: initiativeOrder);

        Assert.Equal(
            EncounterLifecycleState.Active,
            state.LifecycleState);
        Assert.Null(state.WinningSideId);
        Assert.Equal(3, state.Participants.Count);
        Assert.Equal(VanguardId, state.ActiveCombatantId);
        Assert.Equal(1, state.TurnState.RoundNumber);
        Assert.Equal(1, state.TurnState.ActivePosition);

        EncounterParticipantState vanguard =
            FindParticipant(state, VanguardId);

        EncounterParticipantState archer =
            FindParticipant(state, ArcherId);

        EncounterParticipantState raider =
            FindParticipant(state, RaiderId);

        Assert.Equal(
            new GridPosition(0, 1),
            vanguard.Position);
        Assert.Equal(
            new GridPosition(0, 2),
            archer.Position);
        Assert.Equal(
            new GridPosition(2, 1),
            raider.Position);

        Assert.Equal(
            10,
            vanguard.Combatant.Health.HitPoints
                .CurrentHitPoints);
        Assert.Equal(
            10,
            archer.Combatant.Health.HitPoints
                .CurrentHitPoints);
        Assert.Equal(
            9,
            raider.Combatant.Health.HitPoints
                .CurrentHitPoints);

        Assert.Equal(
            3,
            FindWeapon(archer, ArcherWeaponId)
                .AmmunitionQuantityAvailable);

        EncounterActionCandidate[] vanguardOptions =
        [
            new EncounterActionCandidate
            {
                ActionOptionId =
                    "option.vanguard.move",
                ActorCombatantId =
                    state.ActiveCombatantId,
                Timing =
                    EncounterActionTiming.Movement
            },
            new EncounterActionCandidate
            {
                ActionOptionId =
                    "option.vanguard.action",
                ActorCombatantId =
                    state.ActiveCombatantId,
                Timing =
                    EncounterActionTiming.Action
            }
        ];

        EncounterActionDiscoveryResult
            vanguardOptionDiscovery =
                EncounterActionDiscoveryRules.Discover(
                    state,
                    vanguardOptions);

        EncounterActionEvaluation
            vanguardMovementEvaluation =
                FindEvaluation(
                    vanguardOptionDiscovery,
                    "option.vanguard.move");

        EncounterActionEvaluation
            vanguardActionEvaluation =
                FindEvaluation(
                    vanguardOptionDiscovery,
                    "option.vanguard.action");

        Assert.Equal(
            state.Revision,
            vanguardOptionDiscovery
                .EncounterRevision);
        Assert.Equal(
            state.Revision,
            vanguardMovementEvaluation
                .EncounterRevision);
        Assert.True(
            vanguardMovementEvaluation
                .IsCommonlyLegal);
        Assert.Equal(
            EncounterActionUnavailabilityReason.None,
            vanguardMovementEvaluation
                .UnavailabilityReason);
        Assert.Equal(
            state.Revision,
            vanguardActionEvaluation
                .EncounterRevision);
        Assert.True(
            vanguardActionEvaluation
                .IsCommonlyLegal);
        Assert.Equal(
            EncounterActionUnavailabilityReason.None,
            vanguardActionEvaluation
                .UnavailabilityReason);

        EncounterMovementResult movementResult =
            EncounterMovementRules.Resolve(
                state,
                new EncounterMovementCommand
                {
                    ExpectedRevision = state.Revision,
                    ActorCombatantId = VanguardId,
                    Path =
                    [
                        new GridPosition(1, 1)
                    ]
                });

        state = movementResult.State;
        vanguard = FindParticipant(state, VanguardId);

        Assert.Equal(
            new GridPosition(0, 1),
            movementResult.StartingPosition);
        Assert.Equal(
            new GridPosition(1, 1),
            movementResult.EndingPosition);
        Assert.Equal(5, movementResult.MovementSpentFeet);
        Assert.Equal(
            new GridPosition(1, 1),
            vanguard.Position);
        Assert.Equal(
            5,
            vanguard.TurnResources.MovementSpentFeet);
        Assert.Equal(
            25,
            vanguard.TurnResources
                .MovementRemainingFeet);
        Assert.True(
            vanguard.TurnResources.HasActionAvailable);

        vanguard = FindParticipant(state, VanguardId);
        archer = FindParticipant(state, ArcherId);
        raider = FindParticipant(state, RaiderId);

        WeaponAttack authoritativeVanguardWeapon =
            FindWeapon(vanguard, VanguardWeaponId);

        EncounterWeaponAttackDiscoveryCandidate[]
            vanguardAttackCandidates =
            [
                CreateWeaponAttackCandidate(
                    actionOptionId:
                        "option.vanguard.attack.archer",
                    actorCombatantId:
                        state.ActiveCombatantId,
                    targetCombatantId:
                        archer.Combatant.CombatantId,
                    weaponId:
                        authoritativeVanguardWeapon
                            .WeaponId),
                CreateWeaponAttackCandidate(
                    actionOptionId:
                        "option.vanguard.attack.raider",
                    actorCombatantId:
                        state.ActiveCombatantId,
                    targetCombatantId:
                        raider.Combatant.CombatantId,
                    weaponId:
                        authoritativeVanguardWeapon
                            .WeaponId)
            ];

        EncounterActionDiscoveryResult
            vanguardAttackDiscovery =
                EncounterActionDiscoveryRules
                    .DiscoverWeaponAttacks(
                        state,
                        vanguardAttackCandidates);

        EncounterActionEvaluation
            vanguardAllyTargetEvaluation =
                FindEvaluation(
                    vanguardAttackDiscovery,
                    "option.vanguard.attack.archer");

        EncounterActionEvaluation
            vanguardRaiderTargetEvaluation =
                FindEvaluation(
                    vanguardAttackDiscovery,
                    "option.vanguard.attack.raider");

        Assert.False(
            vanguardAllyTargetEvaluation
                .IsCommonlyLegal);
        Assert.Equal(
            EncounterActionUnavailabilityReason
                .TargetNotHostile,
            vanguardAllyTargetEvaluation
                .UnavailabilityReason);
        Assert.True(
            vanguardRaiderTargetEvaluation
                .IsCommonlyLegal);
        Assert.Equal(
            EncounterActionUnavailabilityReason.None,
            vanguardRaiderTargetEvaluation
                .UnavailabilityReason);

        EncounterWeaponAttackDiscoveryCandidate
            selectedVanguardAttack =
                FindCandidate(
                    vanguardAttackCandidates,
                    vanguardRaiderTargetEvaluation
                        .ActionOptionId);

        EncounterWeaponAttackResult
            vanguardAttackResult =
                EncounterWeaponAttackRules.Resolve(
                    state,
                    new EncounterWeaponAttackCommand
                    {
                        ExpectedRevision = state.Revision,
                        ActorCombatantId =
                            selectedVanguardAttack
                                .ActorCombatantId,
                        TargetCombatantId =
                            selectedVanguardAttack
                                .TargetCombatantId,
                        WeaponId =
                            selectedVanguardAttack
                                .WeaponId,
                        FirstAttackRoll = 10,
                        SecondAttackRoll = null,
                        DamageRolls = [2]
                    });

        state = vanguardAttackResult.State;
        vanguard = FindParticipant(state, VanguardId);
        raider = FindParticipant(state, RaiderId);

        Assert.Equal(
            AttackRollOutcome.Hit,
            vanguardAttackResult.Attack.AttackRoll
                .Outcome);
        Assert.Equal(
            15,
            vanguardAttackResult.Attack.AttackRoll
                .Total);
        Assert.Equal(
            3,
            vanguardAttackResult.Attack.Damage
                .FinalDamage);
        Assert.NotNull(
            vanguardAttackResult.TargetDamage);
        Assert.Equal(
            6,
            raider.Combatant.Health.HitPoints
                .CurrentHitPoints);
        Assert.False(
            vanguard.TurnResources.HasActionAvailable);
        Assert.Equal(
            EncounterLifecycleState.Active,
            state.LifecycleState);

        EncounterTurnAdvancementResult
            vanguardTurnAdvancement =
                EncounterTurnAdvancementRules.Resolve(
                    state,
                    new EncounterTurnAdvancementCommand
                    {
                        ExpectedRevision =
                            state.Revision,
                        ActorCombatantId = VanguardId
                    });

        state = vanguardTurnAdvancement.State;
        raider = FindParticipant(state, RaiderId);

        Assert.Equal(
            VanguardId,
            vanguardTurnAdvancement
                .EndedTurnCombatantId);
        Assert.Equal(
            RaiderId,
            vanguardTurnAdvancement
                .ActiveCombatantId);
        Assert.Equal(
            2,
            vanguardTurnAdvancement.ActivePosition);
        Assert.Equal(RaiderId, state.ActiveCombatantId);
        Assert.Equal(2, state.TurnState.ActivePosition);
        Assert.True(
            raider.TurnResources.HasActionAvailable);
        Assert.Equal(
            30,
            raider.TurnResources.MovementSpeedFeet);
        Assert.Equal(
            0,
            raider.TurnResources.MovementSpentFeet);
        Assert.Equal(
            30,
            raider.TurnResources
                .MovementRemainingFeet);

        vanguard = FindParticipant(state, VanguardId);
        archer = FindParticipant(state, ArcherId);
        raider = FindParticipant(state, RaiderId);

        WeaponAttack authoritativeRaiderWeapon =
            FindWeapon(raider, RaiderWeaponId);

        EncounterWeaponAttackDiscoveryCandidate[]
            raiderAttackCandidates =
            [
                CreateWeaponAttackCandidate(
                    actionOptionId:
                        "option.raider.attack.vanguard",
                    actorCombatantId:
                        state.ActiveCombatantId,
                    targetCombatantId:
                        vanguard.Combatant.CombatantId,
                    weaponId:
                        authoritativeRaiderWeapon
                            .WeaponId),
                CreateWeaponAttackCandidate(
                    actionOptionId:
                        "option.raider.attack.archer",
                    actorCombatantId:
                        state.ActiveCombatantId,
                    targetCombatantId:
                        archer.Combatant.CombatantId,
                    weaponId:
                        authoritativeRaiderWeapon
                            .WeaponId)
            ];

        EncounterActionDiscoveryResult
            raiderAttackDiscovery =
                EncounterActionDiscoveryRules
                    .DiscoverWeaponAttacks(
                        state,
                        raiderAttackCandidates);

        EncounterActionEvaluation
            raiderVanguardTargetEvaluation =
                FindEvaluation(
                    raiderAttackDiscovery,
                    "option.raider.attack.vanguard");

        EncounterActionEvaluation
            raiderArcherTargetEvaluation =
                FindEvaluation(
                    raiderAttackDiscovery,
                    "option.raider.attack.archer");

        Assert.True(
            raiderVanguardTargetEvaluation
                .IsCommonlyLegal);
        Assert.Equal(
            EncounterActionUnavailabilityReason.None,
            raiderVanguardTargetEvaluation
                .UnavailabilityReason);
        Assert.False(
            raiderArcherTargetEvaluation
                .IsCommonlyLegal);
        Assert.Equal(
            EncounterActionUnavailabilityReason
                .TargetOutOfRange,
            raiderArcherTargetEvaluation
                .UnavailabilityReason);

        EncounterWeaponAttackDiscoveryCandidate
            selectedRaiderAttack =
                FindCandidate(
                    raiderAttackCandidates,
                    raiderVanguardTargetEvaluation
                        .ActionOptionId);

        EncounterWeaponAttackResult
            raiderAttackResult =
                EncounterWeaponAttackRules.Resolve(
                    state,
                    new EncounterWeaponAttackCommand
                    {
                        ExpectedRevision = state.Revision,
                        ActorCombatantId =
                            selectedRaiderAttack
                                .ActorCombatantId,
                        TargetCombatantId =
                            selectedRaiderAttack
                                .TargetCombatantId,
                        WeaponId =
                            selectedRaiderAttack
                                .WeaponId,
                        FirstAttackRoll = 10,
                        SecondAttackRoll = null,
                        DamageRolls = [2]
                    });

        state = raiderAttackResult.State;
        vanguard = FindParticipant(state, VanguardId);
        raider = FindParticipant(state, RaiderId);

        Assert.Equal(
            AttackRollOutcome.Hit,
            raiderAttackResult.Attack.AttackRoll
                .Outcome);
        Assert.Equal(
            14,
            raiderAttackResult.Attack.AttackRoll
                .Total);
        Assert.Equal(
            2,
            raiderAttackResult.Attack.Damage
                .FinalDamage);
        Assert.NotNull(raiderAttackResult.TargetDamage);
        Assert.Equal(
            8,
            vanguard.Combatant.Health.HitPoints
                .CurrentHitPoints);
        Assert.False(
            raider.TurnResources.HasActionAvailable);
        Assert.Equal(
            EncounterLifecycleState.Active,
            state.LifecycleState);

        EncounterTurnAdvancementResult
            raiderTurnAdvancement =
                EncounterTurnAdvancementRules.Resolve(
                    state,
                    new EncounterTurnAdvancementCommand
                    {
                        ExpectedRevision =
                            state.Revision,
                        ActorCombatantId = RaiderId
                    });

        state = raiderTurnAdvancement.State;
        archer = FindParticipant(state, ArcherId);

        Assert.Equal(
            RaiderId,
            raiderTurnAdvancement
                .EndedTurnCombatantId);
        Assert.Equal(
            ArcherId,
            raiderTurnAdvancement
                .ActiveCombatantId);
        Assert.Equal(
            3,
            raiderTurnAdvancement.ActivePosition);
        Assert.Equal(ArcherId, state.ActiveCombatantId);
        Assert.Equal(3, state.TurnState.ActivePosition);
        Assert.True(
            archer.TurnResources.HasActionAvailable);
        Assert.Equal(
            30,
            archer.TurnResources.MovementSpeedFeet);
        Assert.Equal(
            0,
            archer.TurnResources.MovementSpentFeet);
        Assert.Equal(
            30,
            archer.TurnResources
                .MovementRemainingFeet);
        Assert.Equal(
            3,
            FindWeapon(archer, ArcherWeaponId)
                .AmmunitionQuantityAvailable);

        vanguard = FindParticipant(state, VanguardId);
        archer = FindParticipant(state, ArcherId);
        raider = FindParticipant(state, RaiderId);

        WeaponAttack authoritativeArcherWeapon =
            FindWeapon(archer, ArcherWeaponId);

        EncounterWeaponAttackDiscoveryCandidate[]
            archerAttackCandidates =
            [
                CreateWeaponAttackCandidate(
                    actionOptionId:
                        "option.archer.attack.vanguard",
                    actorCombatantId:
                        state.ActiveCombatantId,
                    targetCombatantId:
                        vanguard.Combatant.CombatantId,
                    weaponId:
                        authoritativeArcherWeapon
                            .WeaponId),
                CreateWeaponAttackCandidate(
                    actionOptionId:
                        "option.archer.attack.raider",
                    actorCombatantId:
                        state.ActiveCombatantId,
                    targetCombatantId:
                        raider.Combatant.CombatantId,
                    weaponId:
                        authoritativeArcherWeapon
                            .WeaponId)
            ];

        EncounterActionDiscoveryResult
            archerAttackDiscovery =
                EncounterActionDiscoveryRules
                    .DiscoverWeaponAttacks(
                        state,
                        archerAttackCandidates);

        EncounterActionEvaluation
            archerVanguardTargetEvaluation =
                FindEvaluation(
                    archerAttackDiscovery,
                    "option.archer.attack.vanguard");

        EncounterActionEvaluation
            archerRaiderTargetEvaluation =
                FindEvaluation(
                    archerAttackDiscovery,
                    "option.archer.attack.raider");

        Assert.False(
            archerVanguardTargetEvaluation
                .IsCommonlyLegal);
        Assert.Equal(
            EncounterActionUnavailabilityReason
                .TargetNotHostile,
            archerVanguardTargetEvaluation
                .UnavailabilityReason);
        Assert.True(
            archerRaiderTargetEvaluation
                .IsCommonlyLegal);
        Assert.Equal(
            EncounterActionUnavailabilityReason.None,
            archerRaiderTargetEvaluation
                .UnavailabilityReason);

        EncounterWeaponAttackDiscoveryCandidate
            selectedArcherAttack =
                FindCandidate(
                    archerAttackCandidates,
                    archerRaiderTargetEvaluation
                        .ActionOptionId);

        EncounterWeaponAttackResult
            archerAttackResult =
                EncounterWeaponAttackRules.Resolve(
                    state,
                    new EncounterWeaponAttackCommand
                    {
                        ExpectedRevision = state.Revision,
                        ActorCombatantId =
                            selectedArcherAttack
                                .ActorCombatantId,
                        TargetCombatantId =
                            selectedArcherAttack
                                .TargetCombatantId,
                        WeaponId =
                            selectedArcherAttack
                                .WeaponId,
                        FirstAttackRoll = 10,
                        SecondAttackRoll = null,
                        DamageRolls = [5]
                    });

        state = archerAttackResult.State;
        vanguard = FindParticipant(state, VanguardId);
        archer = FindParticipant(state, ArcherId);
        raider = FindParticipant(state, RaiderId);

        WeaponAttack finalArcherWeapon =
            FindWeapon(archer, ArcherWeaponId);

        Assert.Equal(
            AttackRollOutcome.Hit,
            archerAttackResult.Attack.AttackRoll
                .Outcome);
        Assert.Equal(
            15,
            archerAttackResult.Attack.AttackRoll
                .Total);
        Assert.Equal(
            6,
            archerAttackResult.Attack.Damage
                .FinalDamage);
        Assert.NotNull(archerAttackResult.TargetDamage);
        Assert.Equal(
            0,
            raider.Combatant.Health.HitPoints
                .CurrentHitPoints);
        Assert.Equal(
            CombatantLifecycleState.Defeated,
            raider.Combatant.LifecycleState);
        Assert.False(
            archer.TurnResources.HasActionAvailable);
        Assert.Equal(
            2,
            finalArcherWeapon
                .AmmunitionQuantityAvailable);
        Assert.Equal(
            EncounterLifecycleState.Completed,
            state.LifecycleState);
        Assert.Equal(PartySideId, state.WinningSideId);

        Assert.Equal(3, state.Participants.Count);

        Assert.Equal(
            8,
            vanguard.Combatant.Health.HitPoints
                .CurrentHitPoints);
        Assert.Equal(
            10,
            vanguard.Combatant.Health.HitPoints
                .MaximumHitPoints);
        Assert.Equal(
            CombatantLifecycleState.Conscious,
            vanguard.Combatant.LifecycleState);

        Assert.Equal(
            10,
            archer.Combatant.Health.HitPoints
                .CurrentHitPoints);
        Assert.Equal(
            10,
            archer.Combatant.Health.HitPoints
                .MaximumHitPoints);
        Assert.Equal(
            CombatantLifecycleState.Conscious,
            archer.Combatant.LifecycleState);

        Assert.Equal(
            0,
            raider.Combatant.Health.HitPoints
                .CurrentHitPoints);
        Assert.Equal(
            9,
            raider.Combatant.Health.HitPoints
                .MaximumHitPoints);
        Assert.Equal(
            CombatantLifecycleState.Defeated,
            raider.Combatant.LifecycleState);

        Assert.Equal(ArcherWeaponId, finalArcherWeapon.WeaponId);
        Assert.Equal(
            "item.arrow",
            finalArcherWeapon.AmmunitionItemId);
        Assert.Equal(
            2,
            finalArcherWeapon
                .AmmunitionQuantityAvailable);

        Assert.Equal(
            3,
            archerWeapon.AmmunitionQuantityAvailable);
        Assert.Equal(
            EncounterLifecycleState.Completed,
            state.LifecycleState);
        Assert.Equal(PartySideId, state.WinningSideId);
    }

    private static EncounterParticipantSetup
        CreateParticipant(
            string combatantId,
            string sideId,
            GridPosition position,
            int maximumHitPoints,
            CombatantZeroHitPointPolicy
                zeroHitPointPolicy,
            int armorClass,
            WeaponAttack weapon)
    {
        return new EncounterParticipantSetup
        {
            Combatant = CombatantRules.Create(
                combatantId,
                maximumHitPoints,
                zeroHitPointPolicy),
            CombatProfile = new EncounterCombatProfile
            {
                ArmorClass = armorClass,
                WeaponAttacks =
                [
                    weapon
                ]
            },
            SideId = sideId,
            MovementSpeedFeet = 30,
            StartingPosition = position
        };
    }

    private static WeaponAttack CreateMeleeWeapon(
        string weaponId,
        string weaponName,
        int attackBonus,
        DieType damageDie,
        int damageBonus,
        string damageType)
    {
        return new WeaponAttack
        {
            WeaponId = weaponId,
            WeaponName = weaponName,
            Category = WeaponCategory.Martial,
            AttackKind = WeaponAttackKind.Melee,
            AttackAbility = Ability.Strength,
            AbilityModifier =
                attackBonus - 2,
            IsProficient = true,
            ProficiencyBonus = 2,
            AttackBonus = attackBonus,
            HasDisadvantage = false,
            DisadvantageReasons =
                Array.Empty<string>(),
            AttackRollMode = D20RollMode.Normal,
            Damage = new DamageDice
            {
                Count = 1,
                Die = damageDie
            },
            VersatileDamage = null,
            DamageType = damageType,
            DamageBonus = damageBonus,
            Properties = Array.Empty<string>(),
            ReachFeet = null,
            NormalRangeFeet = null,
            LongRangeFeet = null,
            AmmunitionItemId = null,
            AmmunitionQuantityAvailable = null
        };
    }

    private static WeaponAttack CreateRangedWeapon(
        string weaponId,
        string weaponName,
        int attackBonus,
        DieType damageDie,
        int damageBonus,
        string damageType,
        string ammunitionItemId,
        int ammunitionQuantityAvailable)
    {
        return new WeaponAttack
        {
            WeaponId = weaponId,
            WeaponName = weaponName,
            Category = WeaponCategory.Simple,
            AttackKind = WeaponAttackKind.Ranged,
            AttackAbility = Ability.Dexterity,
            AbilityModifier =
                attackBonus - 2,
            IsProficient = true,
            ProficiencyBonus = 2,
            AttackBonus = attackBonus,
            HasDisadvantage = false,
            DisadvantageReasons =
                Array.Empty<string>(),
            AttackRollMode = D20RollMode.Normal,
            Damage = new DamageDice
            {
                Count = 1,
                Die = damageDie
            },
            VersatileDamage = null,
            DamageType = damageType,
            DamageBonus = damageBonus,
            Properties = Array.Empty<string>(),
            ReachFeet = null,
            NormalRangeFeet = 30,
            LongRangeFeet = 120,
            AmmunitionItemId = ammunitionItemId,
            AmmunitionQuantityAvailable =
                ammunitionQuantityAvailable
        };
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

    private static EncounterBattlefieldState
        CreateBattlefield()
    {
        return new EncounterBattlefieldState
        {
            BattlefieldId = "battlefield.lifecycle",
            Width = 4,
            Height = 3,
            BlockedPositions =
                Array.Empty<GridPosition>(),
            CoverPositions =
                Array.Empty<EncounterCoverPosition>(),
            DifficultTerrainPositions =
                Array.Empty<GridPosition>()
        };
    }

    private static EncounterWeaponAttackDiscoveryCandidate
        CreateWeaponAttackCandidate(
            string actionOptionId,
            string actorCombatantId,
            string targetCombatantId,
            string weaponId)
    {
        return new EncounterWeaponAttackDiscoveryCandidate
        {
            ActionOptionId = actionOptionId,
            ActorCombatantId = actorCombatantId,
            TargetCombatantId = targetCombatantId,
            WeaponId = weaponId
        };
    }

    private static EncounterParticipantState
        FindParticipant(
            EncounterState state,
            string combatantId)
    {
        return Assert.Single(
            state.Participants,
            participant => string.Equals(
                participant.Combatant.CombatantId,
                combatantId,
                StringComparison.Ordinal));
    }

    private static WeaponAttack FindWeapon(
        EncounterParticipantState participant,
        string weaponId)
    {
        return Assert.Single(
            participant.CombatProfile.WeaponAttacks,
            weapon => string.Equals(
                weapon.WeaponId,
                weaponId,
                StringComparison.Ordinal));
    }

    private static EncounterActionEvaluation
        FindEvaluation(
            EncounterActionDiscoveryResult result,
            string actionOptionId)
    {
        return Assert.Single(
            result.Evaluations,
            evaluation => string.Equals(
                evaluation.ActionOptionId,
                actionOptionId,
                StringComparison.Ordinal));
    }

    private static EncounterWeaponAttackDiscoveryCandidate
        FindCandidate(
            IReadOnlyList<
                EncounterWeaponAttackDiscoveryCandidate>
                    candidates,
            string actionOptionId)
    {
        return Assert.Single(
            candidates,
            candidate => string.Equals(
                candidate.ActionOptionId,
                actionOptionId,
                StringComparison.Ordinal));
    }
}
