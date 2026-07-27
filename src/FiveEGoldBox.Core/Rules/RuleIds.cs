namespace FiveEGoldBox.Core.Rules;

public static class RuleIds
{
    public static class Skills
    {
        public const string Perception = "skill.perception";
        public const string Stealth = "skill.stealth";
    }

    public static class WeaponProperties
    {
        public const string Ammunition = "weapon_property.ammunition";
        public const string Finesse = "weapon_property.finesse";
        public const string Heavy = "weapon_property.heavy";
        public const string TwoHanded = "weapon_property.two_handed";
        public const string Versatile = "weapon_property.versatile";
    }

    public static class WeaponProficiencies
    {
        public const string Simple = "weapon.simple";
        public const string Martial = "weapon.martial";
    }

    public static class ArmorProficiencies
    {
        public const string Light = "armor.light";
        public const string Medium = "armor.medium";
        public const string Heavy = "armor.heavy";
        public const string Shields = "armor.shields";
    }

    /// Identifiers for what a combatant spends.
    ///
    /// Here rather than in the layer above because `SpellAttack.SlotResourceId`
    /// and `CombatantResource.ResourceId` are both Core's, so Core could name
    /// the thing it already refers to everywhere except in one place.
    public static class Resources
    {
        private const string SpellSlotPrefix = "resource.spell-slot.";

        public static string SpellSlot(
            int slotLevel)
        {
            if (slotLevel < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(slotLevel),
                    slotLevel,
                    "Spell slots start at level one.");
            }

            return SpellSlotPrefix + slotLevel.ToString(
                System.Globalization.CultureInfo.InvariantCulture);
        }

        public static bool IsSpellSlot(
            string resourceId)
        {
            ArgumentNullException.ThrowIfNull(resourceId);

            return resourceId.StartsWith(
                SpellSlotPrefix,
                StringComparison.Ordinal);
        }
    }

    public static class DisadvantageReasons
    {
        public const string HeavyWeaponSmallSize = "weapon.heavy.small_size";
    }
}
