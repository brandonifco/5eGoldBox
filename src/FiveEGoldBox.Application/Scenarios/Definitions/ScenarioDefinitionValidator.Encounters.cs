using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Runtime;
using FiveEGoldBox.Core.Validation;

namespace FiveEGoldBox.Application.Scenarios.Definitions;

internal static partial class ScenarioDefinitionValidator
{
    private static void AddEncounterIssues(
        ScenarioDefinition definition,
        ValidatedRuleset? ruleset,
        List<ValidationIssue> issues)
    {
        AddDuplicateIdIssues(
            issues,
            definition.Encounters.Select(encounter => encounter.EncounterId),
            "scenario.encounters.duplicate_id",
            "encounter ID");

        foreach (EncounterDefinition encounter in definition.Encounters)
        {
            AddEncounterIssues(definition, encounter, ruleset, issues);
        }
    }

    private static void AddEncounterIssues(
        ScenarioDefinition definition,
        EncounterDefinition encounter,
        ValidatedRuleset? ruleset,
        List<ValidationIssue> issues)
    {
        AddIfBlank(
            issues,
            encounter.EncounterId,
            "scenario.encounters.id_required",
            "Encounter IDs must not be blank.");
        AddIfBlank(
            issues,
            encounter.BattlefieldId,
            "scenario.encounters.battlefield_required",
            $"Encounter '{encounter.EncounterId}' requires a battlefield ID.");
        AddIfBlank(
            issues,
            encounter.PartySideId,
            "scenario.encounters.party_side_required",
            $"Encounter '{encounter.EncounterId}' requires a party side ID.");

        if (encounter.Width <= 0 || encounter.Height <= 0)
        {
            issues.Add(Error(
                "scenario.encounters.dimensions",
                $"Encounter '{encounter.EncounterId}' must have positive width and height."));
            return;
        }

        HashSet<GridPosition> blocked = [];

        foreach (GridPosition position in encounter.BlockedPositions)
        {
            if (!IsWithin(encounter, position))
            {
                issues.Add(OutOfBounds(encounter, position, "A blocked square"));
                continue;
            }

            blocked.Add(position);
        }

        HashSet<GridPosition> occupied = [];

        AddDeploymentIssues(definition, encounter, blocked, occupied, issues);
        AddCombatantIssues(encounter, ruleset, blocked, occupied, issues);
    }

    /// The battlefield has to have room for the largest party the scenario is
    /// willing to admit, or a legal party could arrive with nowhere to stand.
    private static void AddDeploymentIssues(
        ScenarioDefinition definition,
        EncounterDefinition encounter,
        HashSet<GridPosition> blocked,
        HashSet<GridPosition> occupied,
        List<ValidationIssue> issues)
    {
        if (encounter.PartyStartingPositions.Count
            < definition.PartyRequirement.MaximumMembers)
        {
            issues.Add(Error(
                "scenario.encounters.insufficient_deployment",
                $"Encounter '{encounter.EncounterId}' offers {encounter.PartyStartingPositions.Count} starting squares but the scenario admits up to {definition.PartyRequirement.MaximumMembers} party members."));
        }

        foreach (GridPosition position in encounter.PartyStartingPositions)
        {
            if (!IsWithin(encounter, position))
            {
                issues.Add(OutOfBounds(encounter, position, "A party starting square"));
                continue;
            }

            if (blocked.Contains(position))
            {
                issues.Add(Error(
                    "scenario.encounters.deployment_blocked",
                    $"Encounter '{encounter.EncounterId}' deploys the party onto a blocked square ({position.X}, {position.Y})."));
            }

            if (!occupied.Add(position))
            {
                issues.Add(Error(
                    "scenario.encounters.deployment_overlap",
                    $"Encounter '{encounter.EncounterId}' deploys two party members onto ({position.X}, {position.Y})."));
            }
        }
    }

    private static void AddCombatantIssues(
        EncounterDefinition encounter,
        ValidatedRuleset? ruleset,
        HashSet<GridPosition> blocked,
        HashSet<GridPosition> occupied,
        List<ValidationIssue> issues)
    {
        if (encounter.Combatants.Count == 0)
        {
            issues.Add(Error(
                "scenario.encounters.no_combatants",
                $"Encounter '{encounter.EncounterId}' has no opposition."));
        }

        AddDuplicateIdIssues(
            issues,
            encounter.Combatants.Select(combatant => combatant.CombatantId),
            "scenario.combatants.duplicate_id",
            $"combatant ID in encounter '{encounter.EncounterId}'");

        foreach (EncounterCombatantDefinition combatant in encounter.Combatants)
        {
            string subject =
                $"Combatant '{combatant.CombatantId}' in encounter '{encounter.EncounterId}'";

            AddIfBlank(
                issues,
                combatant.CombatantId,
                "scenario.combatants.id_required",
                "Combatant IDs must not be blank.");

            AddIfBlank(
                issues,
                combatant.MonsterId,
                "scenario.combatants.monster_id_required",
                $"{subject} requires a monster ID.");

            AddIfBlank(
                issues,
                combatant.SideId,
                "scenario.combatants.side_required",
                $"{subject} requires a side ID.");

            if (string.Equals(
                combatant.SideId,
                encounter.PartySideId,
                StringComparison.Ordinal))
            {
                issues.Add(Error(
                    "scenario.combatants.party_side",
                    $"{subject} is placed on the party's own side; scenarios author opposition, not party members."));
            }

            if (ruleset is not null
                && !string.IsNullOrWhiteSpace(combatant.MonsterId)
                && !ruleset.Definition.Monsters.Any(monster => string.Equals(
                    monster.Id,
                    combatant.MonsterId,
                    StringComparison.Ordinal)))
            {
                issues.Add(Error(
                    "scenario.combatants.monster_id_unresolved",
                    $"{subject} references monster '{combatant.MonsterId}', which the scenario's ruleset does not define."));
            }

            AddPlacementIssues(
                encounter,
                combatant,
                blocked,
                occupied,
                subject,
                issues);
        }
    }

    private static void AddPlacementIssues(
        EncounterDefinition encounter,
        EncounterCombatantDefinition combatant,
        HashSet<GridPosition> blocked,
        HashSet<GridPosition> occupied,
        string subject,
        List<ValidationIssue> issues)
    {
        GridPosition position = combatant.StartingPosition;

        if (!IsWithin(encounter, position))
        {
            issues.Add(OutOfBounds(encounter, position, subject));
            return;
        }

        if (blocked.Contains(position))
        {
            issues.Add(Error(
                "scenario.combatants.blocked_square",
                $"{subject} starts on a blocked square."));
        }

        if (!occupied.Add(position))
        {
            issues.Add(Error(
                "scenario.combatants.overlap",
                $"{subject} starts on a square already taken by another combatant or a party member."));
        }
    }

    private static ValidationIssue OutOfBounds(
        EncounterDefinition encounter,
        GridPosition position,
        string subject)
    {
        return Error(
            "scenario.encounters.position_out_of_bounds",
            $"{subject} at ({position.X}, {position.Y}) lies outside encounter '{encounter.EncounterId}'.");
    }

    private static bool IsWithin(
        EncounterDefinition encounter,
        GridPosition position)
    {
        return position.X >= 0
            && position.Y >= 0
            && position.X < encounter.Width
            && position.Y < encounter.Height;
    }
}
