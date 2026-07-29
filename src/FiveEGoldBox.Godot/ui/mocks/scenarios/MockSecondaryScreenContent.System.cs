using System.Linq;

// Options/Help/Controls and mock Save/Load content (M9e) — split out of
// MockSecondaryScreenContent.cs (see that file's header) once the
// combined class crossed the governing plan's 250-line review threshold.
internal static partial class MockSecondaryScreenContent
{
	// The one screen whose body text is genuinely live rather than
	// static flavor — real key bindings (PlayerInputActions) and real
	// current theme/motion state (ShellThemeController), even though
	// this shell doesn't yet let the player toggle them from within it
	// (Save/Load is the one real, reachable action).
	public static ModalViewModel Options(bool highContrast, bool reducedMotion)
	{
		string body =
			"Controls: Arrows/Numpad move, 4/6 turn, Esc/Space exits " +
				"movement. F11 toggles this immersive/standard layout. " +
				$"High Contrast theme is currently {(highContrast ? "on" : "off")} " +
				$"(Ctrl+Alt+H). Reduced Motion is currently " +
				$"{(reducedMotion ? "on" : "off")} (Ctrl+Alt+M).";

		return new ModalViewModel(
			"Options",
			body,
			ListItems: null,
			Commands: new[]
			{
				new CommandViewModel("save-load", "Save / Load", "S"),
				new CommandViewModel("close", "Close", "C"),
			});
	}

	// So ShellInteractionController (which tracks the currently selected
	// slot across the Save/Load screen's own onRowFocused updates) has one
	// place to read the initial selection from, rather than repeating the
	// "slot-1" literal.
	public static string DefaultSaveSlotId => SaveSlots[0].Id;

	private static readonly (string Id, string Label, bool Empty)[] SaveSlots =
	{
		("slot-1", "Slot 1 — Frontier Outpost", false),
		("slot-2", "Slot 2 — (empty)", true),
		("slot-3", "Slot 3 — (empty)", true),
	};

	public static ModalViewModel SaveLoad(string? selectedSlotId = null)
	{
		string activeId = selectedSlotId ?? SaveSlots[0].Id;

		return new ModalViewModel(
			"Save / Load",
			DescribeSaveSlot(activeId),
			SaveSlots
				.Select(slot => new CommandViewModel(slot.Id, slot.Label))
				.ToArray(),
			new[]
			{
				new CommandViewModel("save", "Save", "S"),
				new CommandViewModel("load", "Load", "L"),
				new CommandViewModel("close", "Close", "C"),
			},
			SelectedRowId: activeId,
			BreadcrumbText: "Options > Save / Load");
	}

	public static string DescribeSaveSlot(string slotId)
	{
		(string Id, string Label, bool Empty) slot =
			SaveSlots.First(candidate => candidate.Id == slotId);

		return slot.Empty
			? $"{slot.Label}. Nothing saved here yet."
			: $"{slot.Label}. Saving and loading are not connected to " +
				"the real engine yet.";
	}

	public static string SaveLoadActionMessage(string action, string slotId)
	{
		(string Id, string Label, bool Empty) slot =
			SaveSlots.First(candidate => candidate.Id == slotId);

		return $"You {action} {slot.Label}. Saving and loading are not " +
			"connected to the real engine yet.";
	}
}
