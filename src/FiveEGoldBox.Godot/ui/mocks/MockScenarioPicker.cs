using System;
using System.Collections.Generic;

// Dev-tool cycling over every entry in MockScenarioCatalog, flattened
// into one ordered list. Type-erased to (label, description) only here,
// at the point a picker inherently needs to treat all seven families
// uniformly — the catalog itself stays strongly typed per family.
// Description uses each record's auto-generated ToString() rather than a
// hand-written dumper per family: records already print every property
// deterministically, which is exactly what a "which mock am I looking
// at" dev readout needs.
internal sealed class MockScenarioPicker
{
	private readonly List<(string Label, Func<string> Describe)> _entries = new();
	private int _index;

	public MockScenarioPicker()
	{
		AddFamily("party", MockScenarioCatalog.Party);
		AddFamily("commands", MockScenarioCatalog.Commands);
		AddFamily("messages", MockScenarioCatalog.Messages);
		AddFamily("exploration", MockScenarioCatalog.Exploration);
		AddFamily("regional-map", MockScenarioCatalog.RegionalMap);
		AddFamily("combat", MockScenarioCatalog.Combat);
		AddFamily("modals", MockScenarioCatalog.Modals);
	}

	public int Count => _entries.Count;

	public (string Label, string Description) Current()
	{
		(string label, Func<string> describe) = _entries[_index];
		return (label, describe());
	}

	public (string Label, string Description) Next()
	{
		_index = (_index + 1) % _entries.Count;
		return Current();
	}

	public (string Label, string Description) Previous()
	{
		_index = (_index - 1 + _entries.Count) % _entries.Count;
		return Current();
	}

	private void AddFamily<T>(
		string familyName,
		IReadOnlyDictionary<string, Func<T>> family)
	{
		foreach ((string id, Func<T> factory) in family)
		{
			_entries.Add(($"{familyName}.{id}", () => factory()!.ToString()!));
		}
	}
}
