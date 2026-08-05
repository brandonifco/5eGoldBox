using System;
using System.Collections.Generic;
using System.Linq;
using FiveEGoldBox.Application.Sessions;
using Godot;

// The real integration seam's own command/movement handling — split out
// of ShellInteractionController.cs (see that file's header) once this
// pushed the combined class past the governing plan's 250-line review
// threshold. Mirrors .Exploration.cs/.RegionalMap.cs/.Combat.cs's role
// for their own modes.
internal sealed partial class ShellInteractionController
{
	// SessionView's own literal DisplayName strings for the three movement
	// actions it always offers unconditionally in Exploration mode — the
	// seam that lets ShowRealCommands collapse them into one "Move" entry
	// driving the existing DirectMovement UX (M4) instead of three flat
	// buttons. No Kind is available at this layer (RealGameSession already
	// projected SessionAction into CommandViewModel by here), so this
	// matches by the same stable content strings the player sees.
	private const string MoveForwardLabel = "Move forward";
	private const string TurnLeftLabel = "Turn left";
	private const string TurnRightLabel = "Turn right";

	private RealGameSession? _activeRealMovementSession;
	private RealGameSession? _activeAreaMapSession;

	// True whenever a real session is the one currently on screen --
	// mirrors ShellPresentationController's own _regionalMapIsRealSession
	// guard, generalized to hold the session itself rather than just a
	// bool, since ShowInventoryScreen needs the real object to describe
	// from. Set here on every real render; cleared by the three mock
	// top-level entry points (ShowExploration/ShowRegionalMap/ShowCombat
	// in ShellInteractionController.cs) the same way
	// _regionalMapIsRealSession resets to false in the mock
	// ShowRegionalMap(). Lets ShowInventoryScreen (reached via the same
	// "inventory" command ID from both mock and real command sets) render
	// real party currency/items instead of MockSecondaryScreenContent's
	// demo list whenever a real session is behind it.
	private RealGameSession? _activeRealSession;

	// The real integration seam. Renders whatever RealGameSession.Describe()
	// reports, rather than the mock content ShowExploration()/ShowCombat()
	// otherwise fall back to. overrideMessage carries a just-submitted
	// command's real result text (e.g. "Moved forward.") so re-rendering
	// after an action doesn't immediately clobber it with the generic
	// status line.
	//
	// Scenario conclusion opens the real ShowConclusionScreen modal
	// (the scenario's own authored epilogue), layered on top of this same
	// background render -- see the ScenarioConclusion case below.
	public void ShowRealSession(
		RealGameSession session,
		string? overrideMessage = null)
	{
		ResetContext();

		_activeRealSession = session;
		RefreshPartyPreview();

		RealSessionSnapshot snapshot = session.Describe();

		switch (snapshot.Mode)
		{
			case ApplicationMode.Encounter:
				_activeCombatGameSession = session;
				_activeCombatSession ??=
					new RealCombatSession(session.HandOffToCombat());
				ShowRealCombat(snapshot, overrideMessage);
				return;
			case ApplicationMode.ScenarioConclusion:
				_presentationController.ShowExploration(
					ExplorationSceneKeys.OutpostEntrance,
					snapshot.LocationDisplayName,
					snapshot.IsSuccess == true ? "Victory" : "Defeat",
					overrideMessage ?? snapshot.StatusMessage);
				_presentationController.ConfigureExplorationCorridor(null);
				_commandBarController.ShowCommands();
				ShowConclusionScreen();
				return;
			case ApplicationMode.RegionalTravel:
				_presentationController.ShowRegionalMap();

				if (snapshot.Map is not null)
				{
					_presentationController.ConfigureRegionalMap(
						snapshot.Map);
				}

				_presentationController.SetHeader(
					snapshot.LocationDisplayName,
					snapshot.ModeLabel);
				_presentationController.SetMessage(
					overrideMessage ?? snapshot.StatusMessage);
				break;
			case ApplicationMode.Outpost:
				_presentationController.ShowExploration(
					ExplorationSceneKeys.OutpostEntrance,
					snapshot.LocationDisplayName,
					snapshot.ModeLabel,
					overrideMessage ?? snapshot.StatusMessage);
				_presentationController.ConfigureExplorationCorridor(null);
				break;
			default:
				_presentationController.ShowExploration(
					ExplorationSceneKeys.DungeonCorridor,
					snapshot.LocationDisplayName,
					snapshot.ModeLabel,
					overrideMessage ?? snapshot.StatusMessage);
				_presentationController.ConfigureExplorationCorridor(
					session.DescribeAreaMap());
				break;
		}

		ShowRealCommands(session, snapshot);
	}

	private void ShowRealCommands(
		RealGameSession session,
		RealSessionSnapshot snapshot)
	{
		List<CommandDefinition> commands = new();
		bool offersMovement = false;

		foreach (CommandViewModel commandViewModel in
			snapshot.Commands.Commands)
		{
			if (commandViewModel.Label is MoveForwardLabel
				or TurnLeftLabel
				or TurnRightLabel)
			{
				offersMovement = true;
				continue;
			}

			string commandId = commandViewModel.CommandId;

			commands.Add(CommandViewModelTranslator.ToCommandDefinition(
				commandViewModel,
				() => SubmitRealCommand(session, commandId)));
		}

		if (offersMovement)
		{
			commands.Insert(
				0,
				new CommandDefinition(
					"Move",
					"[b]M[/b]ove",
					Key.M,
					() => EnterRealMovementMode(session)));
		}

		HashSet<char> usedHotkeys = snapshot.Commands.Commands
			.Select(command => char.ToUpperInvariant(command.Hotkey![0]))
			.ToHashSet();

		if (offersMovement)
		{
			usedHotkeys.Add('M');
		}

		// Area is a client-side view toggle, not a SessionAction — real
		// exploration content never offers it (RealGameSession's own
		// header comment says so), so it's synthesized here the same way
		// the collapsed "Move" entry above already is. Scoped to
		// Exploration only, not Outpost/RegionalTravel, matching the
		// user's own framing ("an Area view in exploration mode").
		if (snapshot.Mode == ApplicationMode.Exploration)
		{
			AddClientSideCommand(
				commands,
				usedHotkeys,
				"area-map",
				"Area",
				() => EnterAreaMapMode(session));
		}

		// View is likewise a client-side view toggle rather than a
		// SessionAction -- ShowCharacterScreen has been real-session-aware
		// since PR #226/227, but nothing here ever offered it, unlike Area
		// and Inventory below. There's nothing exploration-specific about
		// checking your own character sheet, so it's offered in every mode
		// this method is reached from, same reasoning as Inventory.
		AddClientSideCommand(
			commands, usedHotkeys, "view", "View", ShowCharacterScreen);

		// Cast is likewise a client-side view toggle, same reasoning as
		// View above -- reference-only (a caster's known spells), not
		// wired to actual casting; ShowSpellbookScreen reports "no
		// prepared spells" honestly for a highlighted member who isn't a
		// caster rather than hiding the command only for them.
		AddClientSideCommand(
			commands, usedHotkeys, "cast", "Cast", ShowSpellbookScreen);

		// Inventory is likewise a client-side view toggle rather than a
		// SessionAction, same reasoning as Area above -- unlike Area,
		// there's nothing exploration-specific about checking the party's
		// purse, so it's offered in every mode ShowRealCommands is reached
		// from (Outpost/RegionalTravel/Exploration; Encounter and
		// ScenarioConclusion both return out of ShowRealSession before
		// reaching here).
		AddClientSideCommand(
			commands,
			usedHotkeys,
			"inventory",
			"Inventory",
			ShowInventoryScreen);

		_commandBarController.ShowCommands(commands.ToArray());
	}

	// Every client-side view toggle (Area/View/Cast/Inventory above) was
	// the same four-step dance repeated with only the id/label/handler
	// varying -- assign a hotkey no earlier command already claimed,
	// record it as claimed, translate, add. Collapsed into one helper
	// rather than four near-identical blocks.
	private static void AddClientSideCommand(
		List<CommandDefinition> commands,
		HashSet<char> usedHotkeys,
		string commandId,
		string label,
		Action handler)
	{
		CommandViewModel commandViewModel = new(
			commandId,
			label,
			HotkeyAssigner.Assign(
				label.Select(char.ToUpperInvariant),
				usedHotkeys,
				label));

		usedHotkeys.Add(commandViewModel.Hotkey![0]);

		commands.Add(CommandViewModelTranslator.ToCommandDefinition(
			commandViewModel,
			handler));
	}

	// Look-only: opens the real area map, replacing the front-facing view
	// while it's shown. Null from DescribeAreaMap means there's no
	// explorable floor here (shouldn't happen while snapshot.Mode is
	// Exploration, but handled honestly rather than assumed away) — the
	// command simply does nothing rather than show an empty screen.
	// Opening the area map drops straight into the same DirectMovement
	// context/input pipeline EnterRealMovementMode uses for the
	// front-facing view — the user asked to move while looking at the
	// map, not to open a look-only screen and have to close it first to
	// walk. Setting _activeRealMovementSession alongside _activeAreaMapSession
	// means ShellInputRouter/ReportMovement need no changes at all to
	// start driving real movement here; ReportMovement itself just also
	// refreshes the map (RefreshAreaMap) instead of nothing, whenever
	// _activeAreaMapSession is set. Esc/Space's existing
	// ExitExplorationMovementMode path already returns to the
	// front-facing view via ShowRealSession once _activeRealMovementSession
	// is set, which doubles as "Close" — no separate close command needed.
	private void EnterAreaMapMode(RealGameSession session)
	{
		AreaMapViewModel? model = session.DescribeAreaMap();

		if (model is null)
		{
			return;
		}

		_activeAreaMapSession = session;
		_activeRealMovementSession = session;
		PushContext(ShellInteractionContext.DirectMovement);
		_presentationController.ShowAreaMap(model);
		_commandBarController.ShowMovementPrompt();
		_presentationController.SetMessage(
			"Viewing the area map. Arrows or numpad 8 move forward; " +
			"4/6 turn. Press Esc or Space to return.");
	}

	// Called from ReportMovement (ShellInteractionController.Exploration.cs)
	// after a real move/turn lands, whenever the area map is the active
	// view, so the grid/marker actually reflect the new position/facing
	// rather than going stale until the player closes and reopens it.
	private void RefreshAreaMap()
	{
		AreaMapViewModel? model = _activeAreaMapSession!.DescribeAreaMap();

		if (model is not null)
		{
			_presentationController.ShowAreaMap(model);
		}
	}

	private void SubmitRealCommand(RealGameSession session, string commandId)
	{
		string message = session.Submit(commandId);

		ShowRealSession(session, message);
	}

	private void EnterRealMovementMode(RealGameSession session)
	{
		_activeRealMovementSession = session;
		PushContext(ShellInteractionContext.DirectMovement);

		_commandBarController.ShowMovementPrompt();
		_presentationController.SetMessage(
			"Movement active: arrows or numpad 8 move forward; " +
			"4/6 turn. Press Esc or Space to return.");
	}
}
