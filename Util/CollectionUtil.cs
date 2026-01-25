using Silksong.ModMenu.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Silksong.TheHuntIsOn.Util;

internal static class CollectionUtil
{
    internal static IntRange IntRange(int minInclusive, int maxInclusive) => new(minInclusive, maxInclusive);

    internal static ListChoiceModel<int> IntRangeModel(int minInclusive, int maxInclusive) => ChoiceModels.ForValues([.. IntRange(minInclusive, maxInclusive)]);

    internal static void Compare<K, V>(IReadOnlyDictionary<K, V> left, IReadOnlyDictionary<K, V> right, Action<K, V?, V?> callback) where V : struct
    {
        foreach (var key in left.Keys.Concat(right.Keys).Distinct())
        {
            callback(key, left.TryGetValue(key, out var leftValue) ? leftValue : null, right.TryGetValue(key, out var rightValue) ? rightValue : null);
        }
    }

    internal static void Compare<K, V>(IReadOnlyDictionary<K, V> left, IReadOnlyDictionary<K, V> right, Action<K, V?, V?> callback) where V : class
    {
        foreach (var key in left.Keys.Concat(right.Keys).Distinct())
        {
            callback(key, left.TryGetValue(key, out var leftValue) ? leftValue : null, right.TryGetValue(key, out var rightValue) ? rightValue : null);
        }
    }
}

internal class IntRange(int minInclusive, int maxInclusive) : IReadOnlyList<int>
{
    public int this[int index] => (index >= 0 && index < Count) ? minInclusive + index : throw new IndexOutOfRangeException($"{index}; Count: {Count}");

    public int Count => maxInclusive + 1 - minInclusive;

    public IEnumerator<int> GetEnumerator()
    {
        for (int i = 0; i < Count; i++) yield return this[i];
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        for (int i = 0; i < Count; i++) yield return this[i];
    }
}
