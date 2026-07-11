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
        public const string Finesse = "weapon_property.finesse";
        public const string Heavy = "weapon_property.heavy";
        public const string TwoHanded = "weapon_property.two_handed";
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

    public static class DisadvantageReasons
    {
        public const string HeavyWeaponSmallSize = "weapon.heavy.small_size";
    }
}
