internal readonly record struct UiThemeSettings(
	UiThemeVariant ThemeVariant,
	UiMotionPreference MotionPreference)
{
	public static UiThemeSettings Default => new(
		UiThemeVariant.Standard,
		UiMotionPreference.Full);
}
