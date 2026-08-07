using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Definitions;

public sealed record ClassDefinition
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required DieType HitDie { get; init; }

    /// Player-facing prose for a character-creation screen, so a choice is
    /// more than a bare name to someone who does not already know 5e.
    /// Optional: content that predates this loads unchanged, and a validator
    /// reports what is missing rather than refusing to load it.
    public string? Description { get; init; }

    public IReadOnlyList<Ability> SavingThrowProficiencies { get; init; }
        = Array.Empty<Ability>();

    /// The abilities this class actually runs on, most important first --
    /// what a character-creation screen recommends putting the best scores
    /// into. Deliberately separate from SavingThrowProficiencies, which is a
    /// different fact that happens to overlap for some classes and diverge
    /// for others (a Wizard saves on Intelligence and Wisdom but runs on
    /// Intelligence alone).
    ///
    /// Advisory only: nothing validates a character against it, because a
    /// player is entitled to build a Fighter who leads with Dexterity.
    public IReadOnlyList<Ability> PrimaryAbilities { get; init; }
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

    /// Identity only -- see SubclassDefinition's own doc comment for why.
    public IReadOnlyList<SubclassDefinition> Subclasses { get; init; }
        = Array.Empty<SubclassDefinition>();

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
