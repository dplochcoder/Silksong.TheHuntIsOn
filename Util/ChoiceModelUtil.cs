using Silksong.ModMenu.Models;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Silksong.TheHuntIsOn.Util;

internal static class ChoiceModelUtil
{
    internal static IntRange IntRange(int minInclusive, int maxInclusive) => new(minInclusive, maxInclusive);

    internal static ListChoiceModel<int> IntRangeModel(int minInclusive, int maxInclusive) => ChoiceModels.ForValues([.. IntRange(minInclusive, maxInclusive)]);

    internal static ListChoiceModel<float> FormatPercent(this ListChoiceModel<float> self)
    {
        self.DisplayFn = (_, value) => $"{value * 100:0.#}%";
        return self;
    }

    internal static ListChoiceModel<int> FormatIntDelta(this ListChoiceModel<int> self, int defaultValue)
    {
        self.DisplayFn = (_, value) =>
        {
            if (value == defaultValue) return $"{value}";
            else if (value > defaultValue) return $"{value} (+{value - defaultValue})";
            else return $"{value} (-{defaultValue - value})";
        };
        return self;
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
