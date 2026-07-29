using System.Linq;

// The reusable location-interaction screen's content (M9f) — the same
// M9a shell (ModalScreenView/ModalScreenCard, ModalViewModel) every
// other M9 screen uses, proven reusable across every kind the milestone
// names by giving four of Frontier Region's own named settlements
// (MockRegionalMapContent, M7a) a distinct kind each: Northhold gets a
// shop, Mirefen an inn, Lakeside services, Seagate a temple. Training,
// Dialogue, and Rewards are proven as real content here too, just not
// yet wired to a location — the same "declared ahead of a caller"
// precedent M4b set for ShellInteractionContext.Targeting/Confirmation
// before M6-M9 gave them real producers.
internal static class MockLocationInteractionContent
{
	public static ModalViewModel? ForLocation(string locationId)
	{
		return locationId switch
		{
			RegionalLocationIds.Northhold => Shop(),
			RegionalLocationIds.Mirefen => Inn(),
			RegionalLocationIds.Lakeside => Services(),
			RegionalLocationIds.Seagate => Temple(),
			_ => null,
		};
	}

	private static readonly (string Id, string Label, string Price)[] ShopGoods =
	{
		("torch", "Torch", "5 sp"),
		("rope", "Rope, 50ft", "1 gp"),
		("rations", "Trail Rations (per day)", "5 cp"),
	};

	private static ModalViewModel Shop()
	{
		return new ModalViewModel(
			"Northhold General Store",
			"A cramped shop stocked for travelers heading into the " +
				"mountains. Buying is not connected to the real engine yet.",
			ShopGoods
				.Select(good => new CommandViewModel(
					good.Id, $"{good.Label} — {good.Price}"))
				.ToArray(),
			new[] { new CommandViewModel("close", "Leave", "C") });
	}

	public static string ShopPurchaseMessage(string goodId)
	{
		(string Id, string Label, string Price) good =
			ShopGoods.First(candidate => candidate.Id == goodId);

		return $"You would buy {good.Label} for {good.Price}. Shops are " +
			"not connected to the real engine yet.";
	}

	private static ModalViewModel Inn()
	{
		return new ModalViewModel(
			"The Mire Rest",
			"A room for the night, away from the marsh damp. Resting " +
				"here is not connected to the real engine yet.",
			ListItems: null,
			Commands: new[]
			{
				new CommandViewModel("rent-room", "Rent a Room", "R"),
				new CommandViewModel("close", "Leave", "C"),
			});
	}

	private static ModalViewModel Temple()
	{
		return new ModalViewModel(
			"Shrine of the Tidewatch",
			"The clergy offer healing for a small donation, and " +
				"blessings for those setting out to sea. Not connected " +
				"to the real engine yet.",
			ListItems: null,
			Commands: new[]
			{
				new CommandViewModel("request-healing", "Request Healing", "H"),
				new CommandViewModel("close", "Leave", "C"),
			});
	}

	private static ModalViewModel Services()
	{
		return new ModalViewModel(
			"Lakeside Boat Docks",
			"Boatmen offer passage across the lake and fresh tackle for " +
				"the road. Not connected to the real engine yet.",
			ListItems: null,
			Commands: new[]
			{
				new CommandViewModel("charter-passage", "Charter Passage", "P"),
				new CommandViewModel("close", "Leave", "C"),
			});
	}

	// Proven content, not yet wired to a location — no settlement in the
	// current Frontier Region content is a training ground.
	public static ModalViewModel Training()
	{
		return new ModalViewModel(
			"Frontier Garrison",
			"Off-duty soldiers spar in the yard and offer pointers for " +
				"a fee. Training is not connected to the real engine yet.",
			ListItems: null,
			Commands: new[]
			{
				new CommandViewModel("train", "Train", "T"),
				new CommandViewModel("close", "Leave", "C"),
			});
	}

	// Proven content, not yet wired to a location — a pure dialogue
	// screen: flavor only, no action beyond leaving.
	public static ModalViewModel Dialogue()
	{
		return new ModalViewModel(
			"Harbor Master",
			"\"Strange lights on the old watchtower, some nights. " +
				"Wouldn't go poking at it myself.\"",
			ListItems: null,
			Commands: new[] { new CommandViewModel("close", "Leave", "C") });
	}

	// Proven content, not yet wired to a location.
	public static ModalViewModel Rewards()
	{
		return new ModalViewModel(
			"Bounty Board",
			"A weathered board of postings. Claiming a bounty is not " +
				"connected to the real engine yet.",
			new[]
			{
				new CommandViewModel(
					"bounty-watchtower", "Clear the Ruined Watchtower — 25 gp"),
			},
			new[] { new CommandViewModel("close", "Leave", "C") });
	}
}
