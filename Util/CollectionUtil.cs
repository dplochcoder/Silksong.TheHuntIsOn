using System;
using System.Collections.Generic;
using System.Linq;

namespace Silksong.TheHuntIsOn.Util;

internal static class CollectionUtil
{
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
