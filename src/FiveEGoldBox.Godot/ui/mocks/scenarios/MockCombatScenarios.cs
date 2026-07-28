using System.Collections.Generic;
using System.Linq;

// One method per §10.2 "Combat" minimum case.
internal static class MockCombatScenarios
{
	public static CombatViewModel SmallGrid()
	{
		return new CombatViewModel(
			8,
			8,
			new[]
			{
				Combatant("fighter", "Fighter", 2, 2),
				Combatant("raider", "Raider", 4, 3),
			});
	}

	public static CombatViewModel LargeGrid()
	{
		return new CombatViewModel(
			24,
			24,
			new[]
			{
				Combatant("fighter", "Fighter", 2, 2),
				Combatant("raider", "Raider", 20, 20),
			});
	}

	public static CombatViewModel ManyCombatants()
	{
		return new CombatViewModel(
			16,
			16,
			Enumerable.Range(0, 10)
				.Select(index => Combatant(
					$"combatant-{index}",
					$"Combatant {index + 1}",
					index % 16,
					index / 4))
				.ToList());
	}

	public static CombatViewModel ActiveActor()
	{
		return new CombatViewModel(
			10,
			10,
			new[]
			{
				Combatant("fighter", "Fighter", 2, 2, active: true),
				Combatant("raider", "Raider", 4, 3),
			},
			ActiveCombatantId: "fighter");
	}

	public static CombatViewModel ValidInvalidHighlights()
	{
		return new CombatViewModel(
			10,
			10,
			new[]
			{
				Combatant("fighter", "Fighter", 2, 2, active: true),
				Combatant("raider", "Raider", 4, 3),
			},
			ActiveCombatantId: "fighter",
			Highlights: new[]
			{
				new CombatHighlightViewModel(3, 2, "move-range"),
				new CombatHighlightViewModel(4, 2, "move-range"),
				new CombatHighlightViewModel(4, 3, "valid-target"),
				new CombatHighlightViewModel(5, 5, "invalid-target"),
			});
	}

	public static CombatViewModel TargetSelection()
	{
		return new CombatViewModel(
			10,
			10,
			new[]
			{
				Combatant("fighter", "Fighter", 2, 2, active: true),
				Combatant("raider", "Raider", 4, 3, selected: true),
			},
			ActiveCombatantId: "fighter",
			SelectedCombatantId: "raider",
			Highlights: new[]
			{
				new CombatHighlightViewModel(4, 3, "valid-target"),
			});
	}

	public static CombatViewModel CompletedEncounterDisplay()
	{
		return new CombatViewModel(
			10,
			10,
			new[]
			{
				Combatant("fighter", "Fighter", 2, 2),
			},
			InformationalOverlays: new[] { "Victory! The raiders have fled." });
	}

	private static CombatantMarkerViewModel Combatant(
		string id,
		string label,
		int gridX,
		int gridY,
		bool active = false,
		bool selected = false)
	{
		return new CombatantMarkerViewModel(id, label, gridX, gridY, active, selected);
	}
}
