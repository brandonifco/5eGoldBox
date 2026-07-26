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

        AttackRollResult? attackRoll = ResolveAttackRoll(
            spell,
            command,
            prerequisites,
            target);
        SavingThrowResult? savingThrow = ResolveSavingThrow(
            spell,
            command,
            target);
        bool tookEffect = TookEffect(spell, attackRoll, savingThrow);

        int damage = tookEffect
            ? ResolveDamage(spell, command, attackRoll, savingThrow, target)
            : 0;

        // Casting costs the caster its action or bonus action whether or not
        // the spell landed. A missed Fire Bolt is still a spent turn.
        EncounterParticipantState[] participants =
            state.Participants.ToArray();
        participants[actorIndex] = SpendCastingTime(actor, spell);

        EncounterState castState = state with
        {
            Participants = Array.AsReadOnly(participants)
        };

        CombatantDamageResult? targetDamage = null;
        EncounterState resolvedState;

        if (damage > 0)
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
            DamageDealt = damage,
            TargetDamage = targetDamage,
            State = resolvedState
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
            spell.AttackBonus,
            target.CombatProfile.ArmorClass
                + (prerequisites.Cover?.ArmorClassBonus ?? 0));
    }

    private static SavingThrowResult? ResolveSavingThrow(
        SpellAttack spell,
        EncounterSpellCastCommand command,
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
            bonus.TotalBonus,
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

    private static void ValidateRollsMatchResolution(
        SpellAttack spell,
        EncounterSpellCastCommand command)
    {
        ArgumentNullException.ThrowIfNull(command.EffectRolls);

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
