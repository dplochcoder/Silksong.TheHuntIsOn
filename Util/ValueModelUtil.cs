using Silksong.ModMenu.Models;

namespace Silksong.TheHuntIsOn.Util;

internal static class ValueModelUtil
{
    internal static void FormatPercent(this IValueModel<float> self)
    {
        if (self is not ListChoiceModel<float> listModel)
            return;

        listModel.DisplayFn = (_, value) => $"{value * 100:0.#}%";
    }

    internal static void FormatIntDelta(this IValueModel<int> self, int defaultValue)
    {
        if (self is not ListChoiceModel<int> listModel)
            return;

        listModel.DisplayFn = (_, value) =>
        {
            if (value == defaultValue)
                return $"{value}";
            else if (value > defaultValue)
                return $"{value} (+{value - defaultValue})";
            else
                return $"{value} (-{defaultValue - value})";
        };
    }
}
