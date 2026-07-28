// Dev-only (M5d), reached through ShellInputRouter's #if DEBUG shortcut
// block: cycles MockScenarioPicker and prints the result through the
// already-live MessageLog. Deliberately not a full render of the
// scenario through real components — that pipeline is M6's job; this
// makes every one of the 50 catalog entries inspectable in seconds
// without editing code, which is what M5's acceptance gate asks for.
public partial class AppShell
{
	private void ShowNextMockScenario()
	{
		DisplayMockScenario(_scenarioPicker.Next());
	}

	private void ShowPreviousMockScenario()
	{
		DisplayMockScenario(_scenarioPicker.Previous());
	}

	private void DisplayMockScenario((string Label, string Description) scenario)
	{
		_presentationController.SetMessage(
			$"[{scenario.Label}]\n{scenario.Description}");
	}

	private void CycleResolutionPreset()
	{
		UiResolutionPreset preset = _layoutController.CycleResolutionPreset();
		_presentationController.SetMessage(
			$"Resolution: {preset.DisplayName} ({preset.Size.X}x{preset.Size.Y})");
	}
}
