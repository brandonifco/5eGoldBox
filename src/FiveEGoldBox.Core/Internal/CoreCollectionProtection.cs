using System.Collections.ObjectModel;

namespace FiveEGoldBox.Core.Internal;

internal static class CoreCollectionProtection
{
    internal static IReadOnlyList<T> ProtectList<T>(
        IEnumerable<T> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        return Array.AsReadOnly(values.ToArray());
    }

    internal static IReadOnlyDictionary<TKey, TValue> ProtectDictionary<TKey, TValue>(
        IEnumerable<KeyValuePair<TKey, TValue>> values,
        IEqualityComparer<TKey>? comparer = null)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(values);

        Dictionary<TKey, TValue> dictionary = new(comparer);

        foreach (KeyValuePair<TKey, TValue> pair in values)
        {
            dictionary.Add(pair.Key, pair.Value);
        }

        return new ReadOnlyDictionary<TKey, TValue>(dictionary);
    }
}
