# Spellcasting and Feature Effects — Design

**Status:** adopted 2026-07-26. **Governs:** the Phase 10 work the Scope Matrix commits
("mechanics get built when authored content needs them"). **Supersedes nothing** — it is
the design for a phase the plans describe but do not specify.

## The governing decision

**5e is the rules authority. Gold Box is the feel.**

Where the two disagree, 5e wins, because [supported-scope-matrix.md](supported-scope-matrix.md)
commits to 2014 5e. What is taken from the Gold Box games is presentation and pacing:
casting is a menu mode — pick a spell, pick a target, watch the dice — resources are
visibly scarce and husbanded across a dungeon, and the engine tells the client exactly
what is legal rather than the client working it out. That last part is not a stylistic
choice; it is what `EncounterActionDiscoveryRules` already does for weapons.

Concretely, the three places they differ:

| | Gold Box (AD&D) | Here |
|---|---|---|
| Preparation | memorise N copies of a spell; casting spends that copy | prepare a **list**, spend a **slot** |
| Cantrips | none — even Magic Missile costs a slot | at-will and unlimited |
| Concentration | does not exist | one at a time, broken by damage or incapacitation |

## The core idea

**Do not build a spell system. Build one seam that features plug into, and make
spellcasting its first and largest consumer.**

Every class feature does at most three things:

| Leg | Examples |
|---|---|
| Grants a resource | spell slots, Second Wind uses, Rage uses, Channel Divinity |
| Grants an action | Cast a Spell, Second Wind, Cunning Action |
| Contributes to a roll | Bless (+1d4 attack/save), Rage (+damage, resistance), Sneak Attack (+Nd6), Archery (+2 attack) |

`ClassDefinition.FeaturesByLevel` already exists as inert string IDs, and nothing reads
them. Those IDs become references to a `FeatureDefinition` declaring which legs it uses.
Spellcasting is a feature granting a resource and an action. Bless is a spell installing
a roll contributor.

The alternative — a bespoke Cleric/Wizard subsystem — means building Sneak Attack
separately, then Rage separately, and ending with several parallel mechanisms that do
the same thing.

## What already exists

Materially more than a greenfield build, which is why the sequencing below starts where
it does:

- **The action economy is built and unused.** `EncounterActionTiming` has
  `Action`/`BonusAction`/`Reaction`; `CombatTurnResources.HasBonusActionAvailable` and
  `CombatTurnResourceRules.SpendBonusAction` exist. `BonusAction` is referenced once in
  the entire engine. Healing Word needs a caller, not an economy.
- **Discovery then execution is the established shape.**
  `EncounterActionDiscoveryRules.DiscoverWeaponAttacks` →
  `EncounterWeaponAttackPrerequisiteRules.Evaluate` → `EncounterWeaponAttackRules`.
  Spells mirror it and reuse line of sight, cover, range and roll-mode resolution rather
  than duplicating them.
- **Saving throws and healing already resolve.** `EncounterSavingThrowRules`,
  `EncounterHealingRules`.
- **Spell slots are the ammunition pattern.** The Ranger's arrows already persist across
  encounters, project into combat and come back out, and PR #102 generalised that
  projection to any member holding a resource. A slot is ammunition with a level.
- **No spell hooks exist in Core.** Nothing to unpick.

## The six spells are the specification

The Scope Matrix commits exactly six, and they are not a sampler — each forces a
distinct mechanism, and together they cover the whole seam.

| Spell | Forces |
|---|---|
| Fire Bolt | spell **attack roll** |
| Sacred Flame | **saving throw**, no attack roll |
| Cure Wounds | **touch** range, healing, slot spend |
| Healing Word | **bonus action** |
| Magic Missile | **automatic hit, multiple damage instances** |
| Bless | **concentration, multiple targets, ongoing roll modifier** |

Nothing else in 5e's first tier needs a mechanism these do not cover.

## Content model

Ruleset data, validated at load like everything else here.

```
SpellDefinition
  Id, Name, Level (0 = cantrip)
  CastingTime      Action | BonusAction | Reaction
  Range            SelfOnly | Touch | Feet(n)
  Targeting        Self | Single | UpTo(n)
  Resolution       SpellAttack | SavingThrow(ability, onSuccess) | Automatic
  Effects          Damage(dice, type, instances) | Healing(dice) | ApplyEffect(id)
  Concentration    bool
  Duration         Instantaneous | Rounds(n)

FeatureDefinition
  Id, Name
  GrantedResources  [ResourceGrant(id, amount, recharge: ShortRest | LongRest)]
  GrantedActions    [actionId]
  Spellcasting      { Ability, SlotsByLevel, PreparedSpellIds }?
  Modifiers         [RollContribution]
```

Validation: spells reference declared damage types and dice the random source can roll
(the Phase 7 capability check already exists for exactly this), features reference
declared spells, prepared spells fit the slots available.

## Runtime state

- **Resources on `PartyMemberState`**, beside `Ammunition` — persistent, saved,
  projected into an encounter and back out. Same shape, lifecycle and tests as arrows.
- **`ActiveEffectState`** on a combatant: effect ID, source, remaining rounds, whether
  it is concentration.
- **`ConcentrationState`**: at most one, broken by a second concentration spell, by
  incapacitation, or by failing a Constitution save on damage.

## The crux: the roll-contribution seam

Bless adds `+1d4` to attack rolls and saving throws.
`EncounterWeaponAttackPrerequisiteRules.ResolveAttackRollMode` **already** collects votes
from several sources — weapon mode, long range, an adjacent hostile, and since PR #120
the target's own state. That is the precedent. Extend the same shape from
advantage/disadvantage to numeric and dice contributions:

```
RollContribution
  AppliesTo    AttackRoll | SavingThrow | DamageRoll
  Condition    Always | MeleeOnly | AgainstProne | ...
  Effect       FlatBonus(n) | DiceBonus(count, die) | Advantage | Resistance
```

Attack and save resolution ask a combatant's active effects and features for
contributions and apply them in a defined order. This one type is what makes Rage,
Sneak Attack, Bardic Inspiration and fighting styles cheap afterwards.

**Bless consumes randomness.** A blessed attack rolls an extra d4, so the deterministic
ordinal sequence shifts. The frozen transcripts move the first time a blessed character
attacks. That is correct behaviour rather than breakage, but it must be a deliberate,
reviewed regeneration — see sequencing.

## Sequencing

Each step independently valuable and testable.

1. **Spell content model and validation.** Pure data. Lands before execution reads it,
   the same way the scenario definition model did in Phase 6.
2. **Resources.** Slots as persistent, ammunition-shaped state; save format extended;
   a caster added to the campaign ruleset.
3. **Fire Bolt and Sacred Flame.** The attack-roll and saving-throw paths, reusing the
   existing prerequisite and save machinery.
4. **Cure Wounds, Healing Word, Magic Missile.** Healing, automatic hits, multiple
   damage instances — and the dormant bonus action wakes up.
5. **Bless.** The roll-contribution seam and concentration. The hard one.
6. **Retrofit one non-spell feature.** Second Wind or Sneak Attack, to prove the seam is
   not secretly spell-shaped. This is the same move that validated the content boundary:
   a second consumer is what tells you an abstraction is real.

Everything through step 5 is testable against **test-local characters**, because spells
are ruleset content and do not need the campaign roster to change. The roster swap to
the Scope Matrix's Fighter/Rogue/Cleric/Wizard happens **once**, after step 5, when the
casters can actually cast — and that single commit is where the frozen transcripts are
deliberately regenerated.

## Deliberately out of scope

- **Upcasting.** The slice is level 1, so only first-level slots exist. Moot until levels do.
- **Areas of effect.** None of the six needs one; no Fireball in the committed set.
- **Preparation swapping.** The party is fixed and there is no character-creation UI, so
  each caster's prepared list is authored campaign content.
- **Reactions beyond opportunity attacks.** The Scope Matrix defers them explicitly.
