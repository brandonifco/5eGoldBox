using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Characters;

public sealed partial class CharacterResolver
{
    private static IReadOnlyList<CharacterMovementSpeed> ResolveMovementSpeeds(
        RaceDefinition? selectedRace,
        SubraceDefinition? selectedSubrace,
        int? speedFeet)
    {
        Dictionary<MovementMode, int> speedsByMode = [];

        AddMovementSpeeds(speedsByMode, selectedRace?.MovementSpeeds);
        AddMovementSpeeds(speedsByMode, selectedSubrace?.MovementSpeeds);

        if (speedFeet is not null)
        {
            speedsByMode[MovementMode.Walk] = speedFeet.Value;
        }

        return speedsByMode
            .OrderBy(speed => speed.Key)
            .Select(speed => new CharacterMovementSpeed
            {
                Mode = speed.Key,
                SpeedFeet = speed.Value
            })
            .ToArray();
    }

    private static void AddMovementSpeeds(
        Dictionary<MovementMode, int> speedsByMode,
        IReadOnlyList<MovementSpeedDefinition>? movementSpeeds)
    {
        if (movementSpeeds is null)
        {
            return;
        }

        foreach (MovementSpeedDefinition movementSpeed in movementSpeeds)
        {
            if (!speedsByMode.TryGetValue(movementSpeed.Mode, out int existingSpeedFeet)
                || movementSpeed.SpeedFeet > existingSpeedFeet)
            {
                speedsByMode[movementSpeed.Mode] = movementSpeed.SpeedFeet;
            }
        }
    }

    private static int? CalculateSpeedFeet(
        RaceDefinition? selectedRace,
        ArmorDefinition? equippedArmor,
        IReadOnlyDictionary<Ability, int> abilityScores)
    {
        if (selectedRace is null)
        {
            return null;
        }

        int speedFeet = selectedRace.BaseSpeedFeet;

        if (equippedArmor?.StrengthRequirement is not int strengthRequirement)
        {
            return speedFeet;
        }

        int strengthScore = abilityScores[Ability.Strength];

        return strengthScore < strengthRequirement
            ? speedFeet - 10
            : speedFeet;
    }
}
