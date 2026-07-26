using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Runtime;

/// Casts a spell that has already been found legal.
///
/// The caller owns randomness, as it does for a weapon attack: every die is
/// rolled outside and handed in, so a cast is a pure function of the state and
/// the numbers it is given.
///
/// Three ways a spell lands, and they differ only in what decides whether it
/// does. An attack spell is rolled against armour class; a saving-throw spell
/// is rolled against by the target; an automatic one simply happens. After
/// that they are the same.
public static class EncounterSpellRules
{
    public static EncounterSpellCastResult Resolve(
        EncounterState state,
        EncounterSpellCastCommand command)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(command);

        EncounterRules.ValidateState(state);

        if (command.ExpectedRevision != state.Revision)
        {
            throw new InvalidOperationException(
                $"Expected encounter revision '{command.ExpectedRevision}', but the current revision is '{state.Revision}'.");
        }

        EncounterSpellPrerequisiteEvaluation prerequisites =
            EncounterSpellPrerequisiteRules.Evaluate(
                state,
                command.ActorCombatantId,
                command.TargetCombatantId,
                command.SpellId);

        if (!prerequisites.IsLegal)
        {
            throw new InvalidOperationException(
                $"The spell is unavailable for reason '{prerequisites.UnavailabilityReason}'.");
        }

        int actorIndex = FindParticipantIndex(state, command.ActorCombatantId);
        int targetIndex = FindParticipantIndex(state, command.TargetCombatantId);
        EncounterParticipantState actor = state.Participants[actorIndex];
        EncounterParticipantState target = state.Participants[targetIndex];
        SpellAttack spell = FindSpell(actor, command.SpellId);

        ValidateRollsMatchResolution(spell, command);

        IReadOnlyList<string> allTargets = ResolveTargets(
            state,
            command,
            spell);

        AttackRollResult? attackRoll = ResolveAttackRoll(
            spell,
            command,
            prerequisites,
            target);
        SavingThrowResult? savingThrow = ResolveSavingThrow(
            spell,
            command,
            prerequisites,
            target);
        bool tookEffect = TookEffect(spell, attackRoll, savingThrow);

        int damage = tookEffect
            ? ResolveDamage(spell, command, attackRoll, savingThrow, target)
            : 0;
        int healing = tookEffect
            ? ResolveHealing(spell, command)
            : 0;

        // Casting costs the caster its action or bonus action whether or not
        // the spell landed. A missed Fire Bolt is still a spent turn.
        EncounterParticipantState[] participants =
            state.Participants.ToArray();
        participants[actorIndex] = SpendSlot(
            SpendCastingTime(actor, spell),
            spell);

        EncounterState castState = state with
        {
            Participants = Array.AsReadOnly(participants)
        };

        CombatantDamageResult? targetDamage = null;
        EncounterState resolvedState;

        if (tookEffect && spell.AppliedEffectId is not null)
        {
            castState = ApplyEffect(
                castState,
                command.ActorCombatantId,
                allTargets,
                spell);
        }

        if (healing > 0)
        {
            resolvedState = EncounterHealingRules.Resolve(
                castState,
                new EncounterHealingCommand
                {
                    ExpectedRevision = command.ExpectedRevision,
                    TargetCombatantId = command.TargetCombatantId,
                    HealingAmount = healing
                })
                .State;
        }
        else if (damage > 0)
        {
            EncounterDamageResult damageResult =
                EncounterDamageRules.Resolve(
                    castState,
                    new EncounterDamageCommand
                    {
                        ExpectedRevision = command.ExpectedRevision,
                        TargetCombatantId = command.TargetCombatantId,
                        DamageAmount = damage,
                        IsCriticalHit = attackRoll?.Outcome
                            == AttackRollOutcome.CriticalHit
                    });

            targetDamage = damageResult.CombatantDamage;
            resolvedState = damageResult.State;
        }
        else
        {
            resolvedState = castState with
            {
                Revision = checked(state.Revision + 1)
            };
        }

        EncounterRules.ValidateState(resolvedState);

        return new EncounterSpellCastResult
        {
            ActorCombatantId = command.ActorCombatantId,
            TargetCombatantId = command.TargetCombatantId,
            SpellId = command.SpellId,
            DistanceFeet = prerequisites.DistanceFeet!.Value,
            AttackRoll = attackRoll,
            SavingThrow = savingThrow,
            TookEffect = tookEffect,
            EffectedCombatantIds = spell.AppliedEffectId is null
                ? Array.Empty<string>()
                : allTargets,
            DamageDealt = damage,
            HealingDone = healing,
            TargetDamage = targetDamage,
            State = resolvedState
        };
    }

    /// Every creature the spell reaches, the named one first. A spell may not
    /// touch more than it says, nor the same creature twice.
    private static IReadOnlyList<string> ResolveTargets(
        EncounterState state,
        EncounterSpellCastCommand command,
        SpellAttack spell)
    {
        List<string> targets = [command.TargetCombatantId];

        foreach (string targetId in command.AdditionalTargetCombatantIds)
        {
            if (targets.Contains(targetId, StringComparer.Ordinal))
            {
                throw new ArgumentException(
                    $"Spell '{spell.SpellId}' names '{targetId}' twice.",
                    nameof(command));
            }

            EncounterSpellPrerequisiteEvaluation additional =
                EncounterSpellPrerequisiteRules.Evaluate(
                    state,
                    command.ActorCombatantId,
                    targetId,
                    command.SpellId);

            if (!additional.IsLegal)
            {
                throw new InvalidOperationException(
                    $"Spell '{spell.SpellId}' cannot reach '{targetId}': '{additional.UnavailabilityReason}'.");
            }

            targets.Add(targetId);
        }

        if (targets.Count > spell.MaximumTargets)
        {
            throw new ArgumentException(
                $"Spell '{spell.SpellId}' reaches {spell.MaximumTargets} creatures, but {targets.Count} were named.",
                nameof(command));
        }

        return Array.AsReadOnly(targets.ToArray());
    }

    /// Settles the effect on everyone it reached, and takes the caster's
    /// concentration if it needs it.
    ///
    /// A caster sustains one thing at a time, so starting a second drops the
    /// first — everywhere, not just on the caster, because an effect nobody is
    /// concentrating on is over.
    private static EncounterState ApplyEffect(
        EncounterState state,
        string casterCombatantId,
        IReadOnlyList<string> targetCombatantIds,
        SpellAttack spell)
    {
        EncounterParticipantState[] participants =
            state.Participants.ToArray();

        if (spell.RequiresConcentration)
        {
            string? previous = participants
                .First(participant => string.Equals(
                    participant.Combatant.CombatantId,
                    casterCombatantId,
                    StringComparison.Ordinal))
                .ConcentratingOnEffectId;

            if (previous is not null)
            {
                for (int index = 0; index < participants.Length; index++)
                {
                    participants[index] = participants[index] with
                    {
                        ActiveEffects = participants[index].ActiveEffects
                            .Where(effect => !(string.Equals(
                                    effect.EffectId,
                                    previous,
                                    StringComparison.Ordinal)
                                && string.Equals(
                                    effect.SourceCombatantId,
                                    casterCombatantId,
                                    StringComparison.Ordinal)))
                            .ToArray()
                    };
                }
            }
        }

        ActiveEffect applied = new()
        {
            EffectId = spell.AppliedEffectId!,
            SourceCombatantId = casterCombatantId,
            RemainingRounds = spell.DurationRounds
                ?? throw new InvalidOperationException(
                    $"Spell '{spell.SpellId}' applies an effect but states no duration."),
            RequiresConcentration = spell.RequiresConcentration,
            Contributions = spell.AppliedContributions
        };

        foreach (string targetId in targetCombatantIds)
        {
            int index = Array.FindIndex(
                participants,
                participant => string.Equals(
                    participant.Combatant.CombatantId,
                    targetId,
                    StringComparison.Ordinal));

            participants[index] = participants[index] with
            {
                ActiveEffects = participants[index].ActiveEffects
                    .Where(effect => !string.Equals(
                        effect.EffectId,
                        applied.EffectId,
                        StringComparison.Ordinal))
                    .Append(applied)
                    .ToArray()
            };
        }

        if (spell.RequiresConcentration)
        {
            int casterIndex = Array.FindIndex(
                participants,
                participant => string.Equals(
                    participant.Combatant.CombatantId,
                    casterCombatantId,
                    StringComparison.Ordinal));

            participants[casterIndex] = participants[casterIndex] with
            {
                ConcentratingOnEffectId = applied.EffectId
            };
        }

        return state with
        {
            Participants = Array.AsReadOnly(participants)
        };
    }

    private static AttackRollResult? ResolveAttackRoll(
        SpellAttack spell,
        EncounterSpellCastCommand command,
        EncounterSpellPrerequisiteEvaluation prerequisites,
        EncounterParticipantState target)
    {
        if (spell.Resolution != SpellResolutionKind.SpellAttack)
        {
            return null;
        }

        return AttackRollRules.ResolveResult(
            prerequisites.AttackRollMode!.Value,
            command.FirstAttackRoll!.Value,
            command.SecondAttackRoll,
            checked(spell.AttackBonus
                + RollContributionRules.Total(
                    prerequisites.AttackRollContributions,
                    command.AttackContributionRolls)),
            target.CombatProfile.ArmorClass
                + (prerequisites.Cover?.ArmorClassBonus ?? 0));
    }

    private static SavingThrowResult? ResolveSavingThrow(
        SpellAttack spell,
        EncounterSpellCastCommand command,
        EncounterSpellPrerequisiteEvaluation prerequisites,
        EncounterParticipantState target)
    {
        if (spell.Resolution != SpellResolutionKind.SavingThrow)
        {
            return null;
        }

        SavingThrowBonus bonus = target.CombatProfile.SavingThrowBonuses
            .FirstOrDefault(candidate =>
                candidate.Ability == spell.SaveAbility!.Value)
            ?? throw new InvalidOperationException(
                $"Combatant '{target.Combatant.CombatantId}' has no saving throw for '{spell.SaveAbility}'.");

        return SavingThrowRules.ResolveSavingThrow(
            spell.SaveAbility!.Value,
            D20RollMode.Normal,
            command.SavingThrowRoll!.Value,
            secondRoll: null,
            checked(bonus.TotalBonus
                + RollContributionRules.Total(
                    prerequisites.SavingThrowContributions,
                    command.SavingThrowContributionRolls)),
            spell.SaveDc);
    }

    /// An attack spell needs a hit; a saving-throw spell that only negates
    /// needs a failed save; one that halves damage lands either way.
    private static bool TookEffect(
        SpellAttack spell,
        AttackRollResult? attackRoll,
        SavingThrowResult? savingThrow)
    {
        return spell.Resolution switch
        {
            SpellResolutionKind.SpellAttack =>
                attackRoll!.Outcome != AttackRollOutcome.Miss,
            SpellResolutionKind.SavingThrow =>
                !IsSaved(savingThrow!)
                    || spell.SaveOutcome == SpellSaveOutcome.HalvesDamage,
            _ => true
        };
    }

    private static bool IsSaved(
        SavingThrowResult savingThrow)
    {
        return savingThrow.Test.Outcome == D20TestOutcome.Success;
    }

    private static int ResolveDamage(
        SpellAttack spell,
        EncounterSpellCastCommand command,
        AttackRollResult? attackRoll,
        SavingThrowResult? savingThrow,
        EncounterParticipantState target)
    {
        int total = 0;
        int consumed = 0;

        foreach (SpellAttackEffect effect in spell.Effects
            .Where(effect => effect.Kind == SpellEffectKind.Damage))
        {
            for (int instance = 0; instance < effect.Instances; instance++)
            {
                total += ResolveDamageInstance(
                    effect,
                    command.EffectRolls,
                    ref consumed,
                    attackRoll,
                    savingThrow,
                    spell,
                    target);
            }
        }

        return total;
    }

    /// Each instance is resolved on its own, so a target's resistance applies
    /// to every dart rather than once to their total.
    private static int ResolveDamageInstance(
        SpellAttackEffect effect,
        IReadOnlyList<int> rolls,
        ref int consumed,
        AttackRollResult? attackRoll,
        SavingThrowResult? savingThrow,
        SpellAttack spell,
        EncounterParticipantState target)
    {
        DamageDice dice = attackRoll?.Outcome == AttackRollOutcome.CriticalHit
            ? DamageRules.GetCriticalHitDamageDice(effect.Dice)
            : effect.Dice;

        if (consumed + dice.Count > rolls.Count)
        {
            throw new ArgumentException(
                "The spell was given fewer dice than it rolls.",
                nameof(rolls));
        }

        int[] instanceRolls = rolls
            .Skip(consumed)
            .Take(dice.Count)
            .ToArray();

        consumed += dice.Count;

        int raw = DamageRules.GetDamageDiceTotal(dice, instanceRolls)
            + effect.FlatBonus;

        // A save that halves takes effect before resistance, as 5e resolves
        // them in that order.
        if (savingThrow is not null && IsSaved(savingThrow)
            && spell.SaveOutcome == SpellSaveOutcome.HalvesDamage)
        {
            raw /= 2;
        }

        return DamageRules.ApplyDamageResponses(
            raw,
            GetDamageResponseTypes(target, effect.DamageType));
    }

    /// Healing has nothing to hit and nothing to resist, so its dice are
    /// simply totalled. It consumes the same roll list, after any damage.
    private static int ResolveHealing(
        SpellAttack spell,
        EncounterSpellCastCommand command)
    {
        int consumed = spell.Effects
            .Where(effect => effect.Kind == SpellEffectKind.Damage)
            .Sum(effect => effect.Dice.Count * effect.Instances);
        int total = 0;

        foreach (SpellAttackEffect effect in spell.Effects
            .Where(effect => effect.Kind == SpellEffectKind.Healing))
        {
            for (int instance = 0; instance < effect.Instances; instance++)
            {
                if (consumed + effect.Dice.Count > command.EffectRolls.Count)
                {
                    throw new ArgumentException(
                        "The spell was given fewer dice than it rolls.",
                        nameof(command));
                }

                total += DamageRules.GetDamageDiceTotal(
                    effect.Dice,
                    command.EffectRolls
                        .Skip(consumed)
                        .Take(effect.Dice.Count)
                        .ToArray())
                    + effect.FlatBonus;

                consumed += effect.Dice.Count;
            }
        }

        return total;
    }

    private static IReadOnlyList<DamageResponseType> GetDamageResponseTypes(
        EncounterParticipantState target,
        string? damageType)
    {
        if (damageType is null)
        {
            return Array.Empty<DamageResponseType>();
        }

        return target.CombatProfile.DamageResponses
            .Where(response => string.Equals(
                response.DamageType,
                damageType,
                StringComparison.Ordinal))
            .Select(response => response.ResponseType)
            .ToArray();
    }

    private static EncounterParticipantState SpendCastingTime(
        EncounterParticipantState actor,
        SpellAttack spell)
    {
        return actor with
        {
            TurnResources = spell.CastingTime == SpellCastingTime.BonusAction
                ? CombatTurnResourceRules.SpendBonusAction(actor.TurnResources)
                : CombatTurnResourceRules.SpendAction(actor.TurnResources)
        };
    }

    /// A slot is spent whether or not the spell lands, the same way the
    /// action is. Cantrips spend nothing.
    private static EncounterParticipantState SpendSlot(
        EncounterParticipantState actor,
        SpellAttack spell)
    {
        if (spell.SlotResourceId is null)
        {
            return actor;
        }

        CombatantResource[] resources =
            actor.CombatProfile.Resources.ToArray();
        int index = Array.FindIndex(
            resources,
            resource => string.Equals(
                resource.ResourceId,
                spell.SlotResourceId,
                StringComparison.Ordinal));

        resources[index] = resources[index] with
        {
            Remaining = resources[index].Remaining - 1
        };

        return actor with
        {
            CombatProfile = actor.CombatProfile with
            {
                Resources = Array.AsReadOnly(resources)
            }
        };
    }

    private static void ValidateRollsMatchResolution(
        SpellAttack spell,
        EncounterSpellCastCommand command)
    {
        ArgumentNullException.ThrowIfNull(command.EffectRolls);
        ArgumentNullException.ThrowIfNull(
            command.AttackContributionRolls);
        ArgumentNullException.ThrowIfNull(
            command.SavingThrowContributionRolls);

        bool needsAttackRoll =
            spell.Resolution == SpellResolutionKind.SpellAttack;
        bool needsSavingThrow =
            spell.Resolution == SpellResolutionKind.SavingThrow;

        if (needsAttackRoll != command.FirstAttackRoll.HasValue)
        {
            throw new ArgumentException(
                $"Spell '{spell.SpellId}' {(needsAttackRoll ? "requires" : "does not use")} an attack roll.",
                nameof(command));
        }

        if (needsSavingThrow != command.SavingThrowRoll.HasValue)
        {
            throw new ArgumentException(
                $"Spell '{spell.SpellId}' {(needsSavingThrow ? "requires" : "does not use")} a saving throw.",
                nameof(command));
        }

        // Dice contributed to a roll nobody makes were rolled for nothing, and
        // randomness spent here is randomness the next roll does not get.
        if (!needsAttackRoll
            && command.AttackContributionRolls.Count > 0)
        {
            throw new ArgumentException(
                $"Spell '{spell.SpellId}' does not use an attack roll, so nothing can contribute to one.",
                nameof(command));
        }

        if (!needsSavingThrow
            && command.SavingThrowContributionRolls.Count > 0)
        {
            throw new ArgumentException(
                $"Spell '{spell.SpellId}' does not use a saving throw, so nothing can contribute to one.",
                nameof(command));
        }
    }

    private static SpellAttack FindSpell(
        EncounterParticipantState actor,
        string spellId)
    {
        return actor.CombatProfile.SpellAttacks.First(spell =>
            string.Equals(spell.SpellId, spellId, StringComparison.Ordinal));
    }

    private static int FindParticipantIndex(
        EncounterState state,
        string combatantId)
    {
        return state.Participants
            .Select((participant, index) => (participant, index))
            .First(entry => string.Equals(
                entry.participant.Combatant.CombatantId,
                combatantId,
                StringComparison.Ordinal))
            .index;
    }
}
