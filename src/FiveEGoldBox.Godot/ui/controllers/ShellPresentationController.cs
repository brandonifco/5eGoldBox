using Godot;

internal sealed class ShellPresentationController : IShellPresentation
{
	private readonly ExplorationView _explorationView;
	private readonly Control _regionalMapView;
	private readonly Control _combatView;
	private readonly HeaderBar _headerBar;
	private readonly HeaderBar _immersiveHeaderBar;
	private readonly MessageLog _messageLog;
	private readonly MessageLog _immersiveMessageLog;

	public ShellPresentationController(
		ExplorationView explorationView,
		Control regionalMapView,
		Control combatView,
		HeaderBar headerBar,
		HeaderBar immersiveHeaderBar,
		MessageLog messageLog,
		MessageLog immersiveMessageLog)
	{
		_explorationView = explorationView;
		_regionalMapView = regionalMapView;
		_combatView = combatView;
		_headerBar = headerBar;
		_immersiveHeaderBar = immersiveHeaderBar;
		_messageLog = messageLog;
		_immersiveMessageLog = immersiveMessageLog;
	}

	public PresentationMode CurrentMode { get; private set; }

	public void ShowExploration()
	{
		CurrentMode = PresentationMode.Exploration;

		_explorationView.Show();
		_regionalMapView.Hide();
		_combatView.Hide();

		_explorationView.Configure(new ExplorationViewModel("outpost-entrance"));
		SetHeader("Outpost", "Exploration");
		SetMessage("You stand at the entrance to the outpost.");
	}

	public void ShowRegionalMap()
	{
		CurrentMode = PresentationMode.RegionalMap;

		_explorationView.Hide();
		_regionalMapView.Show();
		_combatView.Hide();

		SetHeader("Wilderness", "Regional Travel");
		SetMessage("The surrounding region stretches before the party.");
	}

	public void ShowCombat()
	{
		CurrentMode = PresentationMode.Combat;

		_explorationView.Hide();
		_regionalMapView.Hide();
		_combatView.Show();

		SetHeader("Encounter", "Combat");
		SetMessage("Combat has begun.");
	}

	public void SetMessage(string message)
	{
		_messageLog.SetMessage(message);
		_immersiveMessageLog.SetMessage(message);
	}

	private void SetHeader(string location, string mode)
	{
		_headerBar.SetLocationAndMode(location, mode);
		_immersiveHeaderBar.SetLocationAndMode(location, mode);
	}
}
