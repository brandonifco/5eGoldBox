using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Definitions;

public sealed record ClassDefinition
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required DieType HitDie { get; init; }

    public IReadOnlyList<Ability> SavingThrowProficiencies { get; init; }
        = Array.Empty<Ability>();

    public IReadOnlyList<string> ArmorProficiencies { get; init; }
        = Array.Empty<string>();

    public IReadOnlyList<string> WeaponProficiencies { get; init; }
        = Array.Empty<string>();

    public IReadOnlyList<string> ToolProficiencies { get; init; }
        = Array.Empty<string>();

    public IReadOnlyList<string> SkillChoices { get; init; }
        = Array.Empty<string>();

    public int NumberOfSkillChoices { get; init; }

    public IReadOnlyDictionary<int, IReadOnlyList<string>> FeaturesByLevel { get; init; }
        = new Dictionary<int, IReadOnlyList<string>>();

    /// The ability this class casts with. Null for a class that does not cast.
    public Ability? SpellcastingAbility { get; init; }

    /// Slots this class has, keyed by slot level. Derived at character
    /// creation the way hit points are derived from the hit die, so a
    /// character does not restate what its class already says.
    ///
    /// There is no character-level dimension because every character is level
    /// one. Advancement is what adds it.
    public IReadOnlyDictionary<int, int> SpellSlotsByLevel { get; init; }
        = new Dictionary<int, int>();
}
