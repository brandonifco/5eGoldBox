using Godot;

internal sealed class ShellThemeController
{
	private readonly Control _root;
	private readonly Theme _standardTheme;
	private readonly Theme _highContrastTheme;

	public ShellThemeController(
		Control root,
		Theme standardTheme,
		Theme highContrastTheme)
	{
		_root = root;
		_standardTheme = standardTheme;
		_highContrastTheme = highContrastTheme;
	}

	public UiThemeSettings Settings { get; private set; } =
		UiThemeSettings.Default;

	public bool AnimationsEnabled =>
		Settings.MotionPreference == UiMotionPreference.Full;

	public void Initialize()
	{
		Apply(UiThemeSettings.Default);
	}

	public void ToggleHighContrast()
	{
		UiThemeVariant nextVariant =
			Settings.ThemeVariant == UiThemeVariant.Standard
				? UiThemeVariant.HighContrast
				: UiThemeVariant.Standard;

		Apply(Settings with { ThemeVariant = nextVariant });
	}

	public void ToggleReducedMotion()
	{
		UiMotionPreference nextPreference =
			Settings.MotionPreference == UiMotionPreference.Full
				? UiMotionPreference.Reduced
				: UiMotionPreference.Full;

		Apply(Settings with { MotionPreference = nextPreference });
	}

	private void Apply(UiThemeSettings settings)
	{
		Settings = settings;
		_root.Theme = settings.ThemeVariant == UiThemeVariant.HighContrast
			? _highContrastTheme
			: _standardTheme;
	}
}
