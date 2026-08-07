using FiveEGoldBox.Application.Content.V1;

namespace FiveEGoldBox.ContentEditor.Models;

/// Editable projection of an EncounterDefinitionV1, backing the battlefield
/// grid editor.
///
/// BlockedPositions and PartyStartingPositions are ordered Lists for the same
/// reason a floor's TraversablePositions is: committed order is hand-authored
/// and not row-major (Watchtower deploys the party (1,1), (1,2), (0,2), (0,1)
/// -- a formation, read clockwise, not a scan), so round-tripping through an
/// unordered set would rewrite it on the first save. Toggling mutates in
/// place: removing leaves the rest in order, adding appends.
internal sealed class EncounterFormModel
{
    public string EncounterId { get; set; } = "";

    public string BattlefieldId { get; set; } = "";

    public int Width { get; set; }

    public int Height { get; set; }

    public string PartySideId { get; set; } = "";

    public List<GridPositionV1> BlockedPositions { get; set; } = [];

    public List<GridPositionV1> PartyStartingPositions { get; set; } = [];

    public List<EncounterCombatantFormModel> Combatants { get; set; } = [];

    public string VictoryProgressId { get; set; } = "";

    public string DefeatProgressId { get; set; } = "";

    public static EncounterFormModel FromDefinition(
        EncounterDefinitionV1 encounter)
    {
        return new EncounterFormModel
        {
            EncounterId = encounter.EncounterId,
            BattlefieldId = encounter.BattlefieldId,
            Width = encounter.Width,
            Height = encounter.Height,
            PartySideId = encounter.PartySideId,
            BlockedPositions = encounter.BlockedPositions.ToList(),
            PartyStartingPositions = encounter.PartyStartingPositions.ToList(),
            Combatants = encounter.Combatants
                .Select(EncounterCombatantFormModel.FromDefinition)
                .ToList(),
            VictoryProgressId = encounter.Outcome.VictoryProgressId,
            DefeatProgressId = encounter.Outcome.DefeatProgressId
        };
    }

    public EncounterDefinitionV1 ToDefinition()
    {
        return new EncounterDefinitionV1
        {
            EncounterId = EncounterId,
            BattlefieldId = BattlefieldId,
            Width = Width,
            Height = Height,
            PartySideId = PartySideId,
            BlockedPositions = BlockedPositions,
            PartyStartingPositions = PartyStartingPositions,
            Combatants = Combatants.Select(combatant => combatant.ToDefinition()).ToList(),
            Outcome = new EncounterOutcomeDefinitionV1
            {
                VictoryProgressId = VictoryProgressId,
                DefeatProgressId = DefeatProgressId
            }
        };
    }

    public bool IsBlocked(int x, int y) =>
        BlockedPositions.Any(position => position.X == x && position.Y == y);

    public bool IsPartyStart(int x, int y) =>
        PartyStartingPositions.Any(position => position.X == x && position.Y == y);

    public EncounterCombatantFormModel? FindCombatant(int x, int y) =>
        Combatants.FirstOrDefault(combatant => combatant.X == x && combatant.Y == y);

    public void ToggleBlocked(int x, int y) =>
        Toggle(BlockedPositions, x, y);

    public void TogglePartyStart(int x, int y) =>
        Toggle(PartyStartingPositions, x, y);

    private static void Toggle(
        List<GridPositionV1> positions,
        int x,
        int y)
    {
        GridPositionV1? existing = positions
            .FirstOrDefault(position => position.X == x && position.Y == y);

        if (existing is not null)
        {
            positions.Remove(existing);
            return;
        }

        positions.Add(new GridPositionV1 { X = x, Y = y });
    }

    /// Suggests a combatant id following committed content's own
    /// "combatant.<monster-without-its-prefix>" convention
    /// (monster.mill-rat -> combatant.mill-rat), disambiguated when the same
    /// monster appears more than once -- which it does, since an encounter
    /// routinely fields two of the same creature.
    public string SuggestCombatantId(
        string monsterId)
    {
        string stem = monsterId.StartsWith("monster.", StringComparison.Ordinal)
            ? monsterId["monster.".Length..]
            : monsterId;

        HashSet<string> taken = new(
            Combatants.Select(combatant => combatant.CombatantId),
            StringComparer.Ordinal);

        string bare = $"combatant.{stem}";

        if (taken.Add(bare))
        {
            return bare;
        }

        for (int index = 2; ; index++)
        {
            string candidate = $"combatant.{stem}.{index}";

            if (taken.Add(candidate))
            {
                return candidate;
            }
        }
    }
}

internal sealed class EncounterCombatantFormModel
{
    public string CombatantId { get; set; } = "";

    public string MonsterId { get; set; } = "";

    public string SideId { get; set; } = "";

    public int X { get; set; }

    public int Y { get; set; }

    public static EncounterCombatantFormModel FromDefinition(
        EncounterCombatantDefinitionV1 combatant)
    {
        return new EncounterCombatantFormModel
        {
            CombatantId = combatant.CombatantId,
            MonsterId = combatant.MonsterId,
            SideId = combatant.SideId,
            X = combatant.StartingPosition.X,
            Y = combatant.StartingPosition.Y
        };
    }

    public EncounterCombatantDefinitionV1 ToDefinition()
    {
        return new EncounterCombatantDefinitionV1
        {
            CombatantId = CombatantId,
            MonsterId = MonsterId,
            SideId = SideId,
            StartingPosition = new GridPositionV1 { X = X, Y = Y }
        };
    }
}
