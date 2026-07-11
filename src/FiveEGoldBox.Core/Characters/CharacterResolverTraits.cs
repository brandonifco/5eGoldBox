using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Characters;

public sealed partial class CharacterResolver
{
    private static IReadOnlyList<CharacterConditionImmunity> ResolveConditionImmunities(
        RaceDefinition? selectedRace,
        SubraceDefinition? selectedSubrace)
    {
        HashSet<ConditionType> immunities = [];

        AddConditionImmunities(immunities, selectedRace?.ConditionImmunities);
        AddConditionImmunities(immunities, selectedSubrace?.ConditionImmunities);

        return immunities
            .OrderBy(condition => condition)
            .Select(condition => new CharacterConditionImmunity
            {
                Condition = condition
            })
            .ToArray();
    }

    private static void AddConditionImmunities(
        HashSet<ConditionType> immunities,
        IReadOnlyList<ConditionImmunityDefinition>? conditionImmunities)
    {
        if (conditionImmunities is null)
        {
            return;
        }

        foreach (ConditionImmunityDefinition conditionImmunity in conditionImmunities)
        {
            immunities.Add(conditionImmunity.Condition);
        }
    }

    private static IReadOnlyList<CharacterDamageResponse> ResolveDamageResponses(
        RaceDefinition? selectedRace,
        SubraceDefinition? selectedSubrace)
    {
        HashSet<(string DamageType, DamageResponseType ResponseType)> responses = [];

        AddDamageResponses(responses, selectedRace?.DamageResponses);
        AddDamageResponses(responses, selectedSubrace?.DamageResponses);

        return responses
            .OrderBy(response => response.DamageType)
            .ThenBy(response => response.ResponseType)
            .Select(response => new CharacterDamageResponse
            {
                DamageType = response.DamageType,
                ResponseType = response.ResponseType
            })
            .ToArray();
    }

    private static void AddDamageResponses(
        HashSet<(string DamageType, DamageResponseType ResponseType)> responses,
        IReadOnlyList<DamageResponseDefinition>? damageResponses)
    {
        if (damageResponses is null)
        {
            return;
        }

        foreach (DamageResponseDefinition damageResponse in damageResponses)
        {
            responses.Add((
                damageResponse.DamageType,
                damageResponse.ResponseType));
        }
    }

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

    private static IReadOnlyList<CharacterSense> ResolveSenses(
        RaceDefinition? selectedRace,
        SubraceDefinition? selectedSubrace)
    {
        Dictionary<SenseType, int> sensesByType = [];

        AddSenses(sensesByType, selectedRace?.Senses);
        AddSenses(sensesByType, selectedSubrace?.Senses);

        return sensesByType
            .OrderBy(sense => sense.Key)
            .Select(sense => new CharacterSense
            {
                Type = sense.Key,
                RangeFeet = sense.Value
            })
            .ToArray();
    }

    private static void AddSenses(
        Dictionary<SenseType, int> sensesByType,
        IReadOnlyList<SenseDefinition>? senses)
    {
        if (senses is null)
        {
            return;
        }

        foreach (SenseDefinition sense in senses)
        {
            if (!sensesByType.TryGetValue(sense.Type, out int existingRangeFeet)
                || sense.RangeFeet > existingRangeFeet)
            {
                sensesByType[sense.Type] = sense.RangeFeet;
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