using Godot;

public partial class SelectionListDemo : Control
{
	[Export]
	public Theme StandardTheme { get; set; } = null!;

	[Export]
	public Theme HighContrastTheme { get; set; } = null!;

	private SelectionList _selectionList = null!;
	private SelectionList _emptyList = null!;
	private Label _statusLabel = null!;
	private bool _highContrast;

	public override void _Ready()
	{
		_selectionList = GetNode<SelectionList>("%DemoSelectionList");
		_emptyList = GetNode<SelectionList>("%EmptySelectionList");
		_statusLabel = GetNode<Label>("%StatusLabel");

		_selectionList.SelectionChanged += OnSelectionChanged;
		_selectionList.SelectionActivated += OnSelectionActivated;

		_selectionList.SetItems(CreateDemoEntries());
		_emptyList.ClearItems();
		UpdateStatus("Ready — use mouse or keyboard.");
		_selectionList.FocusSelected();
	}

	public override void _UnhandledKeyInput(InputEvent inputEvent)
	{
		if (inputEvent is not InputEventKey keyEvent ||
			!keyEvent.Pressed ||
			keyEvent.Echo)
		{
			return;
		}

		if (keyEvent.CtrlPressed &&
			keyEvent.AltPressed &&
			keyEvent.Keycode == Key.H)
		{
			_highContrast = !_highContrast;
			Theme = _highContrast
				? HighContrastTheme
				: StandardTheme;

			UpdateStatus(
				_highContrast
					? "High-contrast theme enabled."
					: "Standard theme enabled.");

			GetViewport().SetInputAsHandled();
		}
	}

	private void OnSelectionChanged(
		int index,
		string itemId)
	{
		UpdateStatus(
			$"Selected row {index + 1}: {itemId}");
	}

	private void OnSelectionActivated(
		int index,
		string itemId)
	{
		UpdateStatus(
			$"Activated row {index + 1}: {itemId}");
	}

	private void UpdateStatus(string message)
	{
		_statusLabel.Text = message;
	}

	private static SelectionListEntry[] CreateDemoEntries()
	{
		return new SelectionListEntry[]
		{
			new("outpost", "Return to the outpost"),
			new("wilderness", "Travel into the wilderness"),
			new("camp", "Make camp and review the party"),
			new("locked", "Locked destination — disabled", Disabled: true),
			new(
				"long-entry",
				"A deliberately long entry that wraps across multiple lines " +
				"to verify constrained-width and long-label behavior."),
			new("journal", "Review journal entries"),
			new("inventory", "Open party inventory"),
			new("spells", "Review prepared spells"),
			new("formation", "Change party formation"),
			new("save", "Save the current session"),
			new("load", "Load an existing session"),
			new("options", "Open interface options"),
			new("help", "Review keyboard commands"),
			new("quit", "Return to the title screen"),
		};
	}
}
