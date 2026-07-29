// Real, named locations on the frontier region map — mirrors
// ExplorationSceneKeys' role (M6b): one shared source for the live
// content (MockRegionalMapContent) and whatever later reads these same
// IDs, rather than matching string literals in more than one place.
internal static class RegionalLocationIds
{
	public const string Outpost = "outpost";
	public const string Northhold = "northhold";
	public const string OldMine = "old-mine";
	public const string Mirefen = "mirefen";
	public const string Lakeside = "lakeside";
	public const string Seagate = "seagate";
}
