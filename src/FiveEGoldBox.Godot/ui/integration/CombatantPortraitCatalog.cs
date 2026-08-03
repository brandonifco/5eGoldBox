using System.Collections.Generic;

// Maps a real combatant's stable ID to a piece of the medieval-heroes
// asset pack (src/FiveEGoldBox.Godot/assets/medieval-heroes/ -- see
// docs/2026-08-03-medieval-heroes-asset-inventory.md), so the tactical
// grid can show a real sprite for the party's own combatants instead of
// a plain colored pin. Deliberately plain strings, no Godot/Texture2D
// dependency here -- resolving a path to an actual texture is
// CombatView.Markers.cs's job, the same "engine-agnostic integration
// layer, Godot-aware presentation layer" split RealCombatSession/
// RealGameSession already keep.
//
// Role mapping is a real content decision, not inferred from the pack's
// own character names (none of the 8 match a party role or monster by
// name) -- picked once here, by name, rather than guessed per call.
// Enemies aren't mapped at all: the pack has no monster-shaped sprites,
// so an unresolved ID (every monster's CombatantId, and the reserve
// Barbarian/Ranger the active party doesn't field) correctly falls
// through to null, keeping today's placeholder pin for anything this
// catalog doesn't name.
internal static class CombatantPortraitCatalog
{
	private const string BasePath = "res://assets/medieval-heroes";

	// Single static frame (the first of a 3-frame idle "stance1" strip)
	// per party role -- see the "static portrait" scope decision in the
	// asset-inventory doc; a full attack/critical/victory/dead animation
	// pipeline is a separate, larger piece of work, not built here.
	private static readonly IReadOnlyDictionary<string, string> RolePortraits =
		new Dictionary<string, string>
		{
			["party-member.fighter"] = $"{BasePath}/PaulHammerArm/PaulHammerArm_MVsv_alt_stance1.png",
			["party-member.rogue"] = $"{BasePath}/Huntress/Huntress_MVsv_alt_stance1.png",
			["party-member.cleric"] = $"{BasePath}/Naia/Naia_MVsv_alt_stance1.png",
			["party-member.wizard"] = $"{BasePath}/MasterGaerron/MasterGaerron_MVsv_alt_stance1.png",
		};

	internal static string? Resolve(string combatantId)
	{
		return RolePortraits.TryGetValue(combatantId, out string? path)
			? path
			: null;
	}
}
