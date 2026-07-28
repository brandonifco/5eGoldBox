// One method per §10.2 "Modals" minimum case.
internal static class MockModalScenarios
{
	public static ModalViewModel Inventory()
	{
		return new ModalViewModel(
			"Inventory",
			ListItems: new[]
			{
				new CommandViewModel("longsword", "Longsword"),
				new CommandViewModel("shortbow", "Shortbow"),
				new CommandViewModel("healing-potion", "Potion of Healing"),
			},
			Commands: new[] { new CommandViewModel("close", "Close") });
	}

	public static ModalViewModel Spellbook()
	{
		return new ModalViewModel(
			"Spellbook",
			ListItems: new[]
			{
				new CommandViewModel("bless", "Bless"),
				new CommandViewModel("magic-missile", "Magic Missile"),
			},
			Commands: new[] { new CommandViewModel("close", "Close") });
	}

	public static ModalViewModel CharacterView()
	{
		return new ModalViewModel(
			"Character",
			"Fighter, level 1 human. Strength 16, Dexterity 13.",
			Commands: new[] { new CommandViewModel("close", "Close") });
	}

	public static ModalViewModel Encamp()
	{
		return new ModalViewModel(
			"Encamp",
			"Rest here and recover?",
			Commands: new[]
			{
				new CommandViewModel("rest", "Rest"),
				new CommandViewModel("close", "Cancel"),
			});
	}

	public static ModalViewModel Journal()
	{
		return new ModalViewModel(
			"Journal",
			ListItems: new[]
			{
				new CommandViewModel("obj-1", "Investigate the Ruined Watchtower"),
			},
			Commands: new[] { new CommandViewModel("close", "Close") });
	}

	public static ModalViewModel Shop()
	{
		return new ModalViewModel(
			"General Store",
			ListItems: new[]
			{
				new CommandViewModel("torch", "Torch (5 gp)"),
				new CommandViewModel("rope", "Rope, 50ft (2 gp)"),
			},
			Commands: new[] { new CommandViewModel("close", "Leave") });
	}

	public static ModalViewModel Inn()
	{
		return new ModalViewModel(
			"The Weary Traveler Inn",
			"A room for the night?",
			Commands: new[]
			{
				new CommandViewModel("rent-room", "Rent a Room"),
				new CommandViewModel("close", "Leave"),
			});
	}

	public static ModalViewModel Temple()
	{
		return new ModalViewModel(
			"Temple of the Silver Dawn",
			"The clergy offer healing for a small donation.",
			Commands: new[]
			{
				new CommandViewModel("heal", "Request Healing"),
				new CommandViewModel("close", "Leave"),
			});
	}

	public static ModalViewModel SaveLoad()
	{
		return new ModalViewModel(
			"Save / Load",
			ListItems: new[]
			{
				new CommandViewModel("slot-1", "Slot 1 — Ruined Watchtower"),
				new CommandViewModel("slot-2", "Slot 2 — (empty)", Enabled: false),
			},
			Commands: new[]
			{
				new CommandViewModel("save", "Save"),
				new CommandViewModel("load", "Load"),
				new CommandViewModel("close", "Cancel"),
			});
	}

	public static ModalViewModel Confirmation()
	{
		return new ModalViewModel(
			"Leave Combat?",
			"Retreating now may have consequences.",
			Commands: new[]
			{
				new CommandViewModel("confirm", "Confirm"),
				new CommandViewModel("cancel", "Cancel"),
			});
	}
}
