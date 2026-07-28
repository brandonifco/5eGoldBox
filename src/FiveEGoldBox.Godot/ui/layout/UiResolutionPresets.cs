using System.Collections.Generic;
using Godot;

internal static class UiResolutionPresets
{
	public static UiResolutionPreset Minimum { get; } = new(
		"minimum-16x9",
		"Minimum 16:9",
		new Vector2I(1024, 576));

	public static UiResolutionPreset Baseline { get; } = new(
		"baseline-720p",
		"Baseline 720p",
		new Vector2I(1280, 720));

	public static UiResolutionPreset FullHd { get; } = new(
		"full-hd-1080p",
		"Full HD 1080p",
		new Vector2I(1920, 1080));

	public static UiResolutionPreset Qhd { get; } = new(
		"qhd-1440p",
		"QHD 1440p",
		new Vector2I(2560, 1440));

	public static UiResolutionPreset Ultrawide { get; } = new(
		"ultrawide-21x9",
		"Ultrawide 21:9",
		new Vector2I(2560, 1080));

	public static UiResolutionPreset SixteenByTen { get; } = new(
		"desktop-16x10",
		"Desktop 16:10",
		new Vector2I(1920, 1200));

	public static UiResolutionPreset FourByThreeFallback { get; } = new(
		"fallback-4x3",
		"Fallback 4:3",
		new Vector2I(1024, 768));

	public static IReadOnlyList<UiResolutionPreset> All { get; } =
		new[]
		{
			Minimum,
			Baseline,
			FullHd,
			Qhd,
			Ultrawide,
			SixteenByTen,
			FourByThreeFallback,
		};
}
