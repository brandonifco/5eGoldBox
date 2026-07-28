using Godot;

public partial class ComponentGallery
{
	private void ConfigureChromeExamples()
	{
		_standardHeader.SetLocationAndMode("Outpost", "Exploration");
		_immersiveHeader.SetLocationAndMode("Encounter", "Combat");

		PopulateParty(_standardParty);
		PopulateParty(_immersiveParty);

		_standardMessage.SetMessage(
			"Normal state: the party reaches the northern gate.");
		_immersiveMessage.SetMessage(
			"Stress state: A deliberately long message wraps across multiple " +
			"lines while the immersive variant remains readable and scrollable.\n" +
			"Second line verifies retained spacing.\n" +
			"Third line verifies overflow behavior.");

		_standardCommands.Clear();
		AddCommand(
			_standardCommands,
			"View",
			"[b]V[/b]IEW",
			Key.V,
			enabled: true,
			enableShortcut: true);
		_focusedCommandButton = AddCommand(
			_standardCommands,
			"Move",
			"[b]M[/b]OVE",
			Key.M,
			enabled: true,
			enableShortcut: true);
		AddCommand(
			_standardCommands,
			"Disabled",
			"[b]D[/b]ISABLED",
			Key.D,
			enabled: false,
			enableShortcut: false);
		AddCommand(
			_standardCommands,
			"Long command",
			"[b]L[/b]ONG COMMAND LABEL",
			Key.L,
			enabled: true,
			enableShortcut: true);

		_immersiveCommands.Clear();
		AddCommand(
			_immersiveCommands,
			"Inspect",
			"[b]I[/b]NSPECT",
			Key.I,
			enabled: true,
			enableShortcut: false);
		AddCommand(
			_immersiveCommands,
			"Cancel",
			"[b]C[/b]ANCEL",
			Key.C,
			enabled: true,
			enableShortcut: false);
	}

	private static void PopulateParty(PartySidebar sidebar)
	{
		sidebar.ClearMembers();
		sidebar.AddMember("Aric", "12 / 12");
		sidebar.AddMember(
			"Brenna of the Deliberately Long Family Name",
			"10 / 10 — Blessed");
		sidebar.AddMember("Cael", "3 / 9 — Critical");
	}

	private HotkeyCommandButton AddCommand(
		CommandBar commandBar,
		string commandName,
		string formattedLabel,
		Key key,
		bool enabled,
		bool enableShortcut)
	{
		HotkeyCommandButton button =
			HotkeyCommandButtonScene.Instantiate<HotkeyCommandButton>();
		button.Configure(
			commandName,
			formattedLabel,
			key,
			() => UpdateStatus($"Activated gallery command: {commandName}."),
			enableShortcut);
		button.Disabled = !enabled;
		commandBar.AddContent(button);
		return button;
	}

	private void ConfigureSelectionExample()
	{
		_selectionList.SelectionChanged += OnSelectionChanged;
		_selectionList.SelectionActivated += OnSelectionActivated;
		_selectionList.SetItems(new SelectionListEntry[]
		{
			new("normal", "Normal selectable row"),
			new("selected", "Selected row retained without keyboard focus"),
			new("disabled", "Disabled row cannot focus or activate", Disabled: true),
			new(
				"long",
				"Long-label stress state that wraps cleanly without horizontal " +
				"scrolling or clipping."),
			new("overflow-1", "Overflow row 1"),
			new("overflow-2", "Overflow row 2"),
			new("overflow-3", "Overflow row 3"),
			new("overflow-4", "Overflow row 4"),
			new("overflow-5", "Overflow row 5"),
			new("overflow-6", "Overflow row 6"),
		});
		_selectionList.SelectId("selected");
	}

	private void ConfigureTooltipExample()
	{
		_interactiveTooltip.HideTooltip();
		_tooltipTrigger.MouseEntered += ShowInteractiveTooltip;
		_tooltipTrigger.FocusEntered += ShowInteractiveTooltip;
		_tooltipTrigger.MouseExited += HideInteractiveTooltipIfInactive;
		_tooltipTrigger.FocusExited += HideInteractiveTooltipIfInactive;
		_tooltipTrigger.Pressed += ShowInteractiveTooltip;
	}

	private void ShowInteractiveTooltip()
	{
		_interactiveTooltip.ShowTooltip(
			"Interactive TooltipPanel",
			"Hover, focus, or activate the trigger. This long text verifies " +
			"wrapping and reusable title/body updates.");
		UpdateStatus("Interactive tooltip shown.");
	}

	private void HideInteractiveTooltipIfInactive()
	{
		if (_tooltipTrigger.HasFocus() ||
			_tooltipTrigger.GetGlobalRect().HasPoint(
				GetViewport().GetMousePosition()))
		{
			return;
		}

		_interactiveTooltip.HideTooltip();
		UpdateStatus("Interactive tooltip hidden.");
	}

	private void OnSelectionChanged(int index, string itemId)
	{
		UpdateStatus($"Selected gallery row {index + 1}: {itemId}.");
	}

	private void OnSelectionActivated(int index, string itemId)
	{
		UpdateStatus($"Activated gallery row {index + 1}: {itemId}.");
	}
}
