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

		Dictionary<string, Action> handlers = new(StringComparer.Ordinal)
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

		_creationAdvancing = true;

		_modalScreen.ShowScreen(
			new ModalViewModel(
				step.Title,
				body,
				ListItems: BuildCreationRows(step),
				Commands: BuildCreationCommands(step),
				BreadcrumbText: step.Breadcrumb,
				TextEntry: step.IsTextEntry
					? new ModalTextEntryViewModel(
						step.TextValue,
						"Enter a name")
					: null),
			handlers,
			onRowFocused: null,
			onRowActivated: HandleCreationRow,
			onClosed: RestoreTitleScreenIfAbandoned,
			onTextChanged: text => _creationSession?.SetName(text),
			onTextSubmitted: CreationNext);

		PushContext(ShellInteractionContext.ModalScreen);
		_commandBarController.SuppressShortcuts();
	}

	// A multi-select step marks what is already chosen inline, since
	// SelectionList has no checkbox of its own and a row is only ever a
	// label to it.
	private static IReadOnlyList<CommandViewModel> BuildCreationRows(
		CharacterCreationStepView step)
	{
		return step.Options
			.Select(option => step.IsMultiSelect
				? option with
				{
					Label = step.SelectedOptionIds.Contains(option.CommandId)
						? $"[x] {option.Label}"
						: $"[ ] {option.Label}"
				}
				: option)
			.ToArray();
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

	private void HandleCreationRow(string optionId)
	{
		CharacterCreationSession session = _creationSession!;
		CharacterCreationStepView step = session.Describe();

		session.SelectOption(optionId);

		// A single-choice row is the choice, so taking it moves on; a
		// multi-select one only toggles, and the player says when they are
		// done via Next.
		if (!step.IsMultiSelect
			&& step.Step != CharacterCreationStep.AbilityScores)
		{
			CreationNext();
			return;
		}

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
