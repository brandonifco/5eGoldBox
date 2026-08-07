using FiveEGoldBox.Application.Campaigns;
using FiveEGoldBox.Application.Parties;
using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;
using FiveEGoldBox.Core.Validation;

namespace FiveEGoldBox.Application.Characters;

/// Turns a player's own build choices into a real, playable party member --
/// the same CharacterResolver/CharacterDraft pipeline
/// CampaignCharacterDraftFactory already drives for the fixed technical-slice
/// roster, just fed by a player's own choices instead of authored campaign
/// content. A member this produces carries its build on
/// PartyMemberState.CustomBuild rather than a CharacterDefinitionId a
/// campaign's roster can resolve.
///
/// Scoped to what the accepted Scope Matrix's campaign-character-creation
/// section actually commits to: level 1 only, and ability scores from
/// standard array or point buy -- not the Manual method the fixed roster
/// uses (which has no fairness constraint at all) or the unimplemented
/// Rolled method.
public static class CharacterCreationRules
{
    /// What a client may offer a player to choose from. Resolving a ruleset
    /// is otherwise internal to this assembly (RulesetRegistry), so without
    /// this a client can validate a draft it had no way to build the choices
    /// for.
    public static CharacterCreationOptions DescribeOptions(string rulesetId)
    {
        RequireRulesetId(rulesetId);

        RulesetDefinition ruleset = RulesetRegistry
            .Resolve(rulesetId)
            .Definition;

        return new CharacterCreationOptions
        {
            Races = ruleset.Races,
            Classes = ruleset.Classes,
            Backgrounds = ruleset.Backgrounds,
            Skills = ruleset.Skills,
            Weapons = ruleset.Weapons,
            Spells = ruleset.Spells,
            EquipmentItems = ruleset.EquipmentItems
        };
    }

    public static ValidationResult Validate(
        CharacterDraft draft,
        string rulesetId)
    {
        ArgumentNullException.ThrowIfNull(draft);
        RequireRulesetId(rulesetId);

        ValidatedRuleset ruleset = RulesetRegistry.Resolve(rulesetId);

        List<ValidationIssue> issues = new(
            CheckCreationScope(draft));

        issues.AddRange(CheckPreparedSpells(draft, ruleset));

        CharacterResolver resolver = new(ruleset);

        issues.AddRange(resolver.Validate(draft).Issues);

        return new ValidationResult(issues);
    }

    /// Validates and resolves every entry, then assembles them into a
    /// PartyState ready to start a session (see
    /// ScenarioSessionFactory.CreateNew's custom-party overload). Throws on
    /// the first illegal draft rather than returning a partially-built party
    /// -- a caller that wants per-character validation issues before
    /// committing to a party should call Validate itself for each draft
    /// first.
    public static PartyState CreateParty(
        string partyId,
        IReadOnlyList<CharacterCreationEntry> entries,
        string rulesetId)
    {
        if (string.IsNullOrWhiteSpace(partyId))
        {
            throw new ArgumentException(
                "Party ID is required.",
                nameof(partyId));
        }

        ArgumentNullException.ThrowIfNull(entries);

        if (entries.Count == 0)
        {
            throw new ArgumentException(
                "A party requires at least one character.",
                nameof(entries));
        }

        RequireRulesetId(rulesetId);

        ValidatedRuleset ruleset = RulesetRegistry.Resolve(rulesetId);
        CharacterResolver resolver = new(ruleset);

        HashSet<string> partyMemberIds = new(StringComparer.Ordinal);
        List<PartyMemberState> members = new();

        foreach (CharacterCreationEntry entry in entries)
        {
            ArgumentNullException.ThrowIfNull(entry);
            ArgumentNullException.ThrowIfNull(entry.Draft);

            if (string.IsNullOrWhiteSpace(entry.PartyMemberId))
            {
                throw new ArgumentException(
                    "Party-member ID is required.",
                    nameof(entries));
            }

            if (!partyMemberIds.Add(entry.PartyMemberId))
            {
                throw new ArgumentException(
                    $"Party-member ID '{entry.PartyMemberId}' is used more than once.",
                    nameof(entries));
            }

            RequireLegalCharacter(entry, resolver, ruleset);
            RequireSingleAmmunitionWeapon(ruleset, entry);

            CharacterSnapshot snapshot = resolver.Resolve(entry.Draft);

            members.Add(ToMember(entry, snapshot, ruleset));
        }

        return new PartyState
        {
            PartyId = partyId,
            Members = members
        };
    }

    private static void RequireLegalCharacter(
        CharacterCreationEntry entry,
        CharacterResolver resolver,
        ValidatedRuleset ruleset)
    {
        List<ValidationIssue> issues = new(
            CheckCreationScope(entry.Draft));
        issues.AddRange(CheckPreparedSpells(entry.Draft, ruleset));
        issues.AddRange(resolver.Validate(entry.Draft).Issues);

        ValidationResult validation = new(issues);

        if (validation.IsValid)
        {
            return;
        }

        string reasons = string.Join(
            "; ",
            validation.Issues
                .Where(issue => issue.Severity == ValidationSeverity.Error)
                .Select(issue => issue.Message));

        throw new InvalidOperationException(
            $"Character '{entry.Draft.Name ?? entry.PartyMemberId}' is not a legal character: {reasons}");
    }

    /// Checks the parts of the Scope Matrix's commitment that CharacterDraft
    /// validation itself has no reason to know about -- CharacterResolver.
    /// Validate happily accepts a level-6 Manual-scored character, since the
    /// fixed roster legitimately builds every character that way. Player
    /// creation is narrower than what the draft shape as a whole allows.
    private static IEnumerable<ValidationIssue> CheckCreationScope(
        CharacterDraft draft)
    {
        if (draft.Level != 1)
        {
            yield return new ValidationIssue(
                ValidationSeverity.Error,
                "character.creation.level.unsupported",
                "Player-created characters are level 1 only.");
        }

        if (draft.AbilityScoreGenerationMethod
                is not AbilityScoreGenerationMethod.StandardArray
                and not AbilityScoreGenerationMethod.PointBuy)
        {
            yield return new ValidationIssue(
                ValidationSeverity.Error,
                "character.creation.ability_generation.unsupported",
                "Player-created characters must use the standard array or point buy.");
        }
    }

    /// CharacterResolver.Validate checks nothing at all about
    /// PreparedSpellIds -- not that a spell exists, not that the character
    /// can cast. That was survivable while every character came from a
    /// campaign roster, since CampaignDefinitionValidator cross-checks a
    /// roster entry's spells against the ruleset; a player-created character
    /// goes through neither, so an unknown spell would surface much later as
    /// a resolution failure, and a prepared spell on a non-caster would
    /// simply never be castable with nothing saying why.
    private static IEnumerable<ValidationIssue> CheckPreparedSpells(
        CharacterDraft draft,
        ValidatedRuleset ruleset)
    {
        if (draft.PreparedSpellIds.Count == 0)
        {
            yield break;
        }

        ClassDefinition? selectedClass = draft.ClassId is null
            ? null
            : ruleset.Definition.Classes.FirstOrDefault(
                characterClass => string.Equals(
                    characterClass.Id,
                    draft.ClassId,
                    StringComparison.Ordinal));

        // An unresolvable class is already reported as
        // character.class.required/not_found; saying it again here as a
        // spellcasting problem would be noise.
        if (selectedClass is not null
            && selectedClass.SpellcastingAbility is null)
        {
            yield return new ValidationIssue(
                ValidationSeverity.Error,
                "character.spells.not_a_caster",
                $"Class '{selectedClass.Id}' cannot prepare spells.");
        }

        if (draft.PreparedSpellIds.Distinct(StringComparer.Ordinal).Count()
            != draft.PreparedSpellIds.Count)
        {
            yield return new ValidationIssue(
                ValidationSeverity.Error,
                "character.spells.duplicate",
                "Prepared spells must not contain duplicates.");
        }

        foreach (string spellId in draft.PreparedSpellIds)
        {
            bool spellExists = ruleset.Definition.Spells.Any(
                spell => string.Equals(
                    spell.Id,
                    spellId,
                    StringComparison.Ordinal));

            if (!spellExists)
            {
                yield return new ValidationIssue(
                    ValidationSeverity.Error,
                    "character.spells.not_found",
                    $"Prepared spell '{spellId}' was not found in ruleset " +
                        $"'{ruleset.Definition.Id}'.");
            }
        }
    }

    private static void RequireSingleAmmunitionWeapon(
        ValidatedRuleset ruleset,
        CharacterCreationEntry entry)
    {
        int ammunitionWeaponCount = ResolveAmmunitionWeapons(
                ruleset,
                entry.Draft.EquippedWeaponIds)
            .Count();

        if (ammunitionWeaponCount > 1)
        {
            throw new InvalidOperationException(
                $"Character '{entry.Draft.Name ?? entry.PartyMemberId}' equips more than one weapon that requires ammunition, which its party state cannot represent.");
        }
    }

    private static PartyMemberState ToMember(
        CharacterCreationEntry entry,
        CharacterSnapshot snapshot,
        ValidatedRuleset ruleset)
    {
        CharacterDraft draft = entry.Draft;

        int maxHitPoints = snapshot.MaxHitPoints
            ?? throw new InvalidOperationException(
                $"Character '{entry.PartyMemberId}' did not resolve maximum hit points.");

        return new PartyMemberState
        {
            PartyMemberId = entry.PartyMemberId,
            CharacterDefinitionId = $"character.custom.{entry.PartyMemberId}",
            DisplayName = draft.Name ?? entry.PartyMemberId,
            ClassId = draft.ClassId!,
            ZeroHitPointPolicy = CombatantZeroHitPointPolicy.DeathSavingThrows,
            Health = new CombatantHealthState
            {
                HitPoints = new HitPointState
                {
                    MaximumHitPoints = maxHitPoints,
                    CurrentHitPoints = maxHitPoints,
                    TemporaryHitPoints = 0
                },
                DeathSavingThrows = new DeathSavingThrowState
                {
                    SuccessCount = 0,
                    FailureCount = 0,
                    IsStable = false
                },
                IsInstantlyDead = false
            },
            Ammunition = ResolveStartingAmmunition(ruleset, draft),
            Resources = CreateStartingResources(ruleset.Definition.Id, draft.ClassId!),
            CustomBuild = draft
        };
    }

    private static AmmunitionState? ResolveStartingAmmunition(
        ValidatedRuleset ruleset,
        CharacterDraft draft)
    {
        WeaponDefinition? ammunitionWeapon = ResolveAmmunitionWeapons(
                ruleset,
                draft.EquippedWeaponIds)
            .FirstOrDefault();

        if (ammunitionWeapon is null)
        {
            return null;
        }

        int startingQuantity = draft.InventoryItems
            .Where(item => string.Equals(
                item.ItemId,
                ammunitionWeapon.AmmunitionItemId,
                StringComparison.Ordinal))
            .Sum(item => item.Quantity);

        return new AmmunitionState
        {
            WeaponId = ammunitionWeapon.Id,
            AmmunitionItemId = ammunitionWeapon.AmmunitionItemId!,
            RemainingQuantity = startingQuantity
        };
    }

    private static IEnumerable<WeaponDefinition> ResolveAmmunitionWeapons(
        ValidatedRuleset ruleset,
        IReadOnlyList<string> equippedWeaponIds)
    {
        return equippedWeaponIds
            .Select(weaponId => ruleset.Definition.Weapons
                .FirstOrDefault(weapon => string.Equals(
                    weapon.Id,
                    weaponId,
                    StringComparison.Ordinal)))
            .Where(weapon => weapon?.AmmunitionItemId is not null)
            .Select(weapon => weapon!);
    }

    private static IReadOnlyList<CharacterResourceState> CreateStartingResources(
        string rulesetId,
        string classId)
    {
        return CampaignResourceGrants
            .ForClass(rulesetId, classId)
            .Select(grant => new CharacterResourceState
            {
                ResourceId = grant.Key,
                Remaining = grant.Value,
                Maximum = grant.Value
            })
            .ToArray();
    }

    private static void RequireRulesetId(string rulesetId)
    {
        if (string.IsNullOrWhiteSpace(rulesetId))
        {
            throw new ArgumentException(
                "Ruleset ID is required.",
                nameof(rulesetId));
        }
    }
}
