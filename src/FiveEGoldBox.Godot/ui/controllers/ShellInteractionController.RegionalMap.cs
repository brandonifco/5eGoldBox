using System;
using System.Collections.Generic;

// Regional-map command shells (Travel/Search/Enter/Camp/Inventory/
// Journal) — mirrors ShellInteractionController.Exploration.cs's role
// for the exploration side. M7d/M7e.
internal sealed partial class ShellInteractionController
{
	// M7d: driven from CommandSetViewModel data, same translation
	// pipeline M6c built for exploration (CommandViewModelTranslator is
	// shared, not exploration-specific).
	private void ShowRegionalMapCommands()
	{
		CommandSetViewModel commandSet = MockRegionalMapCommandContent.Current();
		Dictionary<string, Action> handlers = BuildRegionalMapCommandHandlers();
		List<CommandDefinition> commands = new();

		foreach (CommandViewModel commandViewModel in commandSet.Commands)
		{
			commands.Add(CommandViewModelTranslator.ToCommandDefinition(
				commandViewModel,
				handlers[commandViewModel.CommandId]));
		}

		_commandBarController.ShowCommands(commands.ToArray());
	}

	private Dictionary<string, Action> BuildRegionalMapCommandHandlers()
	{
		return new Dictionary<string, Action>
		{
			["travel"] = TravelToSelectedLocation,
			["search"] = () => _presentationController.SetMessage(
				"You search the surrounding wilderness but find nothing. " +
					"Search mechanics are not connected yet."),
			// M7e: the one command that actually changes presentation
			// mode rather than only printing a message — the deterministic
			// regional-map -> exploration transition.
			["enter"] = EnterSelectedLocation,
			// Reuses Exploration's own confirmation handler (.Exploration.cs)
			// rather than a second copy — the dialog mechanism and message
			// don't depend on which screen opened it.
			["camp"] = ShowEncampConfirmation,
			["inventory"] = () => _presentationController.SetMessage(
				"You check the party's inventory. " +
					"Inventory management is not connected yet."),
			["journal"] = () => _presentationController.SetMessage(
				"You review the party's journal. " +
					"Journal tracking is not connected yet."),
		};
	}

	private void TravelToSelectedLocation()
	{
		RegionalLocationDefinition? location = CurrentRegionalSelection();

		_presentationController.SetMessage(
			location is null
				? "Select a destination first."
				: $"You set out for {location.Label}. " +
					"Travel is not connected yet.");
	}

	private void EnterSelectedLocation()
	{
		string locationId =
			_presentationController.SelectedRegionalLocationId ??
				RegionalLocationIds.Outpost;

		EnterExploration(() => _presentationController.EnterRegionalLocation(locationId));
	}

	private RegionalLocationDefinition? CurrentRegionalSelection()
	{
		string? locationId = _presentationController.SelectedRegionalLocationId;

		return locationId is null ? null : MockRegionalMapContent.Find(locationId);
	}
}
