using System.Collections.Generic;

// Maps a combatant's *role identity* to a piece of the medieval-heroes
// asset pack (src/FiveEGoldBox.Godot/assets/medieval-heroes/ -- see
// docs/2026-08-03-medieval-heroes-asset-inventory.md), so the tactical
// grid can show a real sprite instead of a plain colored pin. "Role
// identity" is a party member's own CombatantId ("party-member.fighter",
// stable and unique per role) or a monster's MonsterId ("monster.mill-
// rat", shared by every instance of that monster placed on the
// battlefield) -- never a monster's own CombatantId, which is a per-
// placement instance ID ("combatant.mill-rat.first") this catalog would
// never match. RealCombatSession is responsible for passing the right
// one in (MonsterId ?? CombatantId). Deliberately plain strings, no
// Godot/Texture2D dependency here -- resolving a path to an actual
// texture is CombatView.Markers.cs's job, the same "engine-agnostic
// integration layer, Godot-aware presentation layer" split
// RealCombatSession/RealGameSession already keep.
//
// Role mapping is a real content decision, not inferred from the pack's
// own character names (none of the 8 match a party role or monster by
// name) -- picked once here, by name, rather than guessed per call.
// Most enemies still aren't mapped: the medieval-heroes pack has no
// monster-shaped sprites, and the PVGames Apex Predators pack (three
// reptilian beasts, docs/2026-08-03-apex-predators-asset-inventory.md)
// only reads as a plausible stand-in for monster.mill-rat -- the other
// five monsters (raiders, guardians, thrall) are human/humanoid and
// would look wrong in kaiju-lizard art, so they're deliberately left
// unmapped. An unresolved role identity (every other monster's
// MonsterId, and the reserve Barbarian/Ranger the active party doesn't
// field) correctly falls through to null, keeping today's placeholder
// pin.
internal static class CombatantPortraitCatalog
{
	private const string HeroesBasePath = "res://assets/medieval-heroes";
	private const string ApexPredatorsBasePath = "res://assets/apex-predators";

	// Single static frame per role/monster -- see the "static portrait"
	// scope decision in the asset-inventory docs; a full attack/critical/
	// victory/dead animation pipeline is a separate, larger piece of
	// work, not built here. The Apex Predators frame is the first cell
	// (row 0, col 0) of each creature's SideViewBattler sheet, trimmed
	// and centered the same way rather than a pack-provided "stance".
	private static readonly IReadOnlyDictionary<string, string> RolePortraits =
		new Dictionary<string, string>
		{
			["party-member.fighter"] = $"{HeroesBasePath}/PaulHammerArm/PaulHammerArm_MVsv_alt_stance1.png",
			["party-member.rogue"] = $"{HeroesBasePath}/Huntress/Huntress_MVsv_alt_stance1.png",
			["party-member.cleric"] = $"{HeroesBasePath}/Naia/Naia_MVsv_alt_stance1.png",
			["party-member.wizard"] = $"{HeroesBasePath}/MasterGaerron/MasterGaerron_MVsv_alt_stance1.png",
			["monster.mill-rat"] = $"{ApexPredatorsBasePath}/ApexStalker/ApexStalker_MVsv_portrait.png",
		};

	internal static string? Resolve(string roleIdentity)
	{
		return RolePortraits.TryGetValue(roleIdentity, out string? path)
			? path
			: null;
	}
}
