using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Definitions;

public sealed record AbilityScoreIncrease(
    Ability Ability,
    int Amount);