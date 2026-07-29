internal enum CompassDirection
{
	North,
	East,
	South,
	West,
}

// Rotation and display text for CompassDirection — colocated here rather
// than as instance methods since the enum itself can't carry behavior.
internal static class CompassDirectionPresentation
{
	public static CompassDirection TurnLeft(CompassDirection facing)
	{
		return facing switch
		{
			CompassDirection.North => CompassDirection.West,
			CompassDirection.West => CompassDirection.South,
			CompassDirection.South => CompassDirection.East,
			CompassDirection.East => CompassDirection.North,
			_ => facing,
		};
	}

	public static CompassDirection TurnRight(CompassDirection facing)
	{
		return facing switch
		{
			CompassDirection.North => CompassDirection.East,
			CompassDirection.East => CompassDirection.South,
			CompassDirection.South => CompassDirection.West,
			CompassDirection.West => CompassDirection.North,
			_ => facing,
		};
	}

	public static string ToFacingText(CompassDirection facing)
	{
		return $"Facing {facing}";
	}

	public static string ToCompassLetter(CompassDirection facing)
	{
		return facing switch
		{
			CompassDirection.North => "N",
			CompassDirection.East => "E",
			CompassDirection.South => "S",
			CompassDirection.West => "W",
			_ => "?",
		};
	}
}
