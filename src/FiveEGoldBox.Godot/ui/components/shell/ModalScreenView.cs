using System;
using System.Collections.Generic;
using Godot;

// The live producer of ShellInteractionContext.ModalScreen — declared at
// M4b for whichever milestone built the first real list/menu screen, and
// left unwired until now. Mirrors ConfirmationDialog's own
// ModalBackdrop-wrapping shape exactly: a fresh ModalScreenCard is
// instantiated per ShowScreen call rather than reused, so a screen never
// carries stale state (list selection, focus) from whatever was shown
// before it.
public partial class ModalScreenView : Control
{
	[Export]
	public PackedScene CardScene { get; set; } = null!;

	public bool IsOpen => _modalBackdrop.IsModalOpen;

	private ModalBackdrop _modalBackdrop = null!;
	private ModalScreenCard? _card;
	private Action? _onClosed;

	public override void _Ready()
	{
		_modalBackdrop = GetNode<ModalBackdrop>("%ScreenBackdrop");
		_modalBackdrop.DismissRequested += Close;
		_modalBackdrop.Closed += HandleClosed;
	}

	internal void ShowScreen(
		ModalViewModel model,
		IReadOnlyDictionary<string, Action> commandHandlers,
		Action<string>? onRowFocused,
		Action<string>? onRowActivated,
		Action? onClosed,
		Action<string>? onTextChanged = null,
		Action? onTextSubmitted = null)
	{
		if (IsOpen)
		{
			Close();
		}

		_card = CardScene.Instantiate<ModalScreenCard>();
		_card.RowFocused += id => onRowFocused?.Invoke(id);
		_card.RowActivated += id => onRowActivated?.Invoke(id);
		_card.TextChanged += text => onTextChanged?.Invoke(text);
		_card.TextSubmitted += () => onTextSubmitted?.Invoke();

		_onClosed = onClosed;

		// Configure() must run after ShowModal(), not before: SelectionList
		// (the card's %ItemList) only rebuilds its buttons once it is
		// actually in the SceneTree (IsNodeReady()), and ShowModal is what
		// puts the card there. ShowModal's own initial-focus pick therefore
		// runs against a still-empty card, so a second, later-enqueued
		// deferred GrabFocus (below) is what actually lands focus on the
		// real first control — the last deferred call in a frame wins.
		_modalBackdrop.ShowModal(_card, initialFocus: null);
		_card.Configure(model, commandHandlers);

		ModalScreenCard cardToFocus = _card;
		Callable.From(() =>
		{
			if (GodotObject.IsInstanceValid(cardToFocus) && cardToFocus.IsInsideTree())
			{
				cardToFocus.FocusInitialControl();
			}
		}).CallDeferred();
	}

	internal void UpdateBody(string? bodyText)
	{
		_card?.SetBodyText(bodyText);
	}

	internal void UpdateCommands(
		IReadOnlyList<CommandViewModel> commands,
		IReadOnlyDictionary<string, Action> commandHandlers)
	{
		_card?.UpdateCommands(commands, commandHandlers);
	}

	public void Close()
	{
		if (!IsOpen)
		{
			return;
		}

		_modalBackdrop.CloseModal();
	}

	private void HandleClosed()
	{
		_card = null;

		Action? callback = _onClosed;
		_onClosed = null;
		callback?.Invoke();
	}
}
