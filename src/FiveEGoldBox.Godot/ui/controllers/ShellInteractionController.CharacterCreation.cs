using System;
using System.Collections.Generic;
using System.Linq;
using FiveEGoldBox.Application.Parties;
using Godot;

// The title screen and the character-creation wizard behind it.
//
// Both render through the same ModalScreenView every M9 secondary screen
// already uses rather than a bespoke scene: a wizard step is a title, a
// breadcrumb, some body text, a list of choices and a row of buttons, which
// is exactly the ModalViewModel shape. The one thing that shape could not
// express was a free-text field, so ModalViewModel gained an optional
// TextEntry the same way it gained CharacterSheet -- an optional section on
// the shared card, not a second card.
//
// Every rule the wizard appears to enforce is a convenience: the finished
// party goes through CharacterCreationRules.CreateParty, which re-validates
// everything, and a rejection surfaces as a real message rather than a
// silently broken character.
internal sealed partial class ShellInteractionController
{
	private const string CreationRulesetId = "ruleset.campaign";

	// Matches the campaign's own ActivePartySize. Read from content rather
	// than hardcoded would be better, but CampaignDefinition is internal to
	// Application and nothing public reports it yet -- flagged rather than
	// worked around with a second InternalsVisibleTo grant.
	private const int CreationPartySize = 4;

	private CharacterCreationSession? _creationSession;

	// Set while the wizard is deliberately replacing one step's screen with
	// the next. ModalScreenView closes the open screen before opening the
	// new one, which fires onClosed -- without this flag every step change
	// would look exactly like the player pressing Escape.
	private bool _creationAdvancing;

	public void ShowTitleScreen()
	{
		_creationSession = null;

		ShowModalScreen(
			new ModalViewModel(
				"5E Gold Box",
				"A party of four sets out for the frontier.",
				ListItems:
				[
					new CommandViewModel(
						"title.premade",
						"Adventure with the pre-made party",
						TooltipText: "The campaign's own roster: a fighter, "
							+ "a rogue, a cleric and a wizard."),
					new CommandViewModel(
						"title.create",
						"Create your own party",
						TooltipText: $"Build {CreationPartySize} characters "
							+ "of your own.")
				],
				Commands: []),
			new Dictionary<string, Action>(),
			onRowActivated: HandleTitleChoice,
			onClosed: RestoreTitleScreenIfAbandoned);
	}

	private void HandleTitleChoice(string choiceId)
	{
		switch (choiceId)
		{
			case "title.premade":
				_creationAdvancing = true;
				CloseModalScreen();
				_startPremadeGame();
				break;

			case "title.create":
				_creationSession = new CharacterCreationSession(
					CreationRulesetId,
					CreationPartySize);

				ShowCreationStep();
				break;
		}
	}

	/// Escape or a backdrop click anywhere in this flow abandons it. There
	/// is no game running behind these screens, so closing without a choice
	/// has to land somewhere -- it returns to the title rather than leaving
	/// a blank shell with no way forward.
	private void RestoreTitleScreenIfAbandoned()
	{
		if (_creationAdvancing)
		{
			_creationAdvancing = false;
			return;
		}

		Callable.From(ShowTitleScreen).CallDeferred();
	}

	private void ShowCreationStep(string? errorText = null)
	{
		CharacterCreationSession session = _creationSession!;
		CharacterCreationStepView step = session.Describe();

		string body = errorText is null
			? step.BodyText
			: $"{step.BodyText}\n\n{errorText}";

		if (!step.CanAdvance && step.BlockedReason is not null)
		{
			body = $"{body}\n({step.BlockedReason})";
		}

		_creationAdvancing = true;

		_modalScreen.ShowScreen(
			new ModalViewModel(
				step.Title,
				body,
				ListItems: BuildCreationRows(step),
				Commands: BuildCreationCommands(step),
				// Re-entering a step (Back, or returning after Next was
				// blocked) starts the cursor on whatever's already chosen
				// rather than the top of the list, and — since this also
				// drives HandleCreationRowFocused below — shows that
				// option's own description immediately instead of the
				// step's generic intro text.
				SelectedRowId: step.SelectedOptionIds.FirstOrDefault(),
				BreadcrumbText: step.Breadcrumb,
				TextEntry: step.IsTextEntry
					? new ModalTextEntryViewModel(
						step.TextValue,
						"Enter a name")
					: null,
				// Small, content-sized buttons that pack left and wrap,
				// sitting directly in the same page as the description
				// above them -- not a separate scrollable boxed list. See
				// SelectionList.CompactLayout.
				CompactOptionsLayout: true),
			BuildCreationHandlers(),
			onRowFocused: HandleCreationRowFocused,
			onRowActivated: HandleCreationRow,
			onClosed: RestoreTitleScreenIfAbandoned,
			onTextChanged: HandleCreationTextChanged,
			onTextSubmitted: CreationNext);

		PushContext(ShellInteractionContext.ModalScreen);
		_commandBarController.SuppressShortcuts();
	}

	private Dictionary<string, Action> BuildCreationHandlers()
	{
		return new(StringComparer.Ordinal)
		{
			["creation.back"] = CreationBack,
			["creation.next"] = CreationNext,
			["creation.cancel"] = () =>
			{
				_creationAdvancing = true;
				CloseModalScreen();
				ShowTitleScreen();
			}
		};
	}

	// TextChanged fires on every keystroke on the Name step, and "Next"'s
	// own Enabled state (bound to CanAdvance, which the Name step computes
	// from whether anything has been typed) needs to track that live --
	// otherwise the button stays exactly as disabled as it was the moment
	// the step first rendered with an empty field, forever, regardless of
	// what gets typed afterward. Refreshes only the command row rather
	// than going through ShowCreationStep()'s full rebuild, which would
	// tear down and reinstantiate the card -- stealing focus from the
	// field the player is actively typing into.
	private void HandleCreationTextChanged(string text)
	{
		_creationSession?.SetName(text);

		if (_creationSession is null)
		{
			return;
		}

		CharacterCreationStepView step = _creationSession.Describe();
		_modalScreen.UpdateCommands(
			BuildCreationCommands(step),
			BuildCreationHandlers());
	}

	// Every step marks what is already chosen inline, not just multi-select
	// ones -- SelectionList has no checkbox of its own and a row is only
	// ever a label to it, and a single-choice step needs this exactly as
	// much now that HandleCreationRow no longer auto-advances the moment a
	// row is activated: without some visible mark, a player who picked
	// Human and is now just browsing Elf/Dwarf/Halfling for their
	// descriptions would have no way to tell which one is still their
	// actual choice.
	private static IReadOnlyList<CommandViewModel> BuildCreationRows(
		CharacterCreationStepView step)
	{
		return step.Options
			.Select(option => option with
			{
				Label = step.SelectedOptionIds.Contains(option.CommandId)
					? $"[x] {option.Label}"
					: $"[ ] {option.Label}"
			})
			.ToArray();
	}

	// Fired as the keyboard/mouse cursor moves to a different row, not
	// when one is chosen -- browsing options to read their descriptions
	// must never itself commit to one. SetBodyText is the same lighter,
	// no-full-rebuild update M9's Character screen already uses for
	// exactly this shape of change (switching which party member's sheet
	// is shown).
	//
	// Appends the option's own description below the step's own body text
	// rather than replacing it -- step.BodyText carries live state on some
	// steps (Assign Ability Scores says which score is being placed and
	// what's already been assigned), and an earlier version of this that
	// swapped the whole body for the focused option's TooltipText silently
	// discarded that the moment focus landed on any option that had one.
	private void HandleCreationRowFocused(string optionId)
	{
		CharacterCreationStepView step = _creationSession!.Describe();
		CommandViewModel? option = step.Options.FirstOrDefault(
			candidate => candidate.CommandId == optionId);

		_modalScreen.UpdateBody(option?.TooltipText is string tooltip
			? $"{step.BodyText}\n\n{tooltip}"
			: step.BodyText);
	}

	private IReadOnlyList<CommandViewModel> BuildCreationCommands(
		CharacterCreationStepView step)
	{
		string nextLabel = step.Step == CharacterCreationStep.Review
			? (_creationSession!.CharacterNumber == _creationSession.PartySize
				? "Begin"
				: "Confirm")
			: "Next";

		List<CommandViewModel> commands = [];

		if (!_creationSession!.IsAtFirstStep())
		{
			commands.Add(new CommandViewModel(
				"creation.back",
				"Back",
				Hotkey: "B"));
		}

		commands.Add(new CommandViewModel(
			"creation.next",
			nextLabel,
			Hotkey: "N",
			Enabled: step.CanAdvance,
			ReasonText: step.BlockedReason));

		commands.Add(new CommandViewModel(
			"creation.cancel",
			"Cancel",
			Hotkey: "C"));

		return commands;
	}

	// Activating a row -- Enter or a click, as opposed to merely focusing
	// it while browsing -- records the choice and re-renders the step, but
	// deliberately never advances on its own, for a single-choice step any
	// more than a multi-select one. Highlighting Elf to read its
	// description used to be indistinguishable from committing to Elf;
	// now every step works the same way multi-select already did: pick,
	// see it marked, and say "Next" only when actually ready to move on.
	private void HandleCreationRow(string optionId)
	{
		_creationSession!.SelectOption(optionId);
		ShowCreationStep();
	}

	private void CreationNext()
	{
		CharacterCreationSession session = _creationSession!;

		if (!session.TryAdvance(out string? error))
		{
			ShowCreationStep(error);
			return;
		}

		if (!session.IsComplete)
		{
			ShowCreationStep();
			return;
		}

		StartCreatedGame(session);
	}

	private void CreationBack()
	{
		_creationSession!.Back();
		ShowCreationStep();
	}

	private void StartCreatedGame(CharacterCreationSession session)
	{
		PartyState party;

		try
		{
			party = session.BuildParty("party.custom");
		}
		catch (InvalidOperationException exception)
		{
			// The engine refused a party the wizard thought was finished.
			// Say so rather than dropping the player into nothing.
			ShowCreationStep(exception.Message);
			return;
		}

		_creationAdvancing = true;
		CloseModalScreen();
		_creationSession = null;

		_startCreatedGame(party);
	}
}
