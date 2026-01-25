using System;

namespace Silksong.TheHuntIsOn.Util;

internal static class StringUtil
{
    internal static bool ConsumePrefix(this ReadOnlySpan<char> self, ReadOnlySpan<char> prefix, out ReadOnlySpan<char> suffix)
    {
        if (self.StartsWith(prefix))
        {
            suffix = self[prefix.Length..];
            return true;
        }
        else
        {
            suffix = [];
            return false;
        }
    }

    internal static bool ConsumeSuffix(this ReadOnlySpan<char> self, ReadOnlySpan<char> suffix, out ReadOnlySpan<char> prefix)
    {
        if (self.EndsWith(suffix))
        {
            prefix = self[..(self.Length - suffix.Length)];
            return true;
        }
        else
        {
            prefix = [];
            return false;
        }
    }

    internal static bool Split2(this ReadOnlySpan<char> self, char delimeter, out ReadOnlySpan<char> one, out ReadOnlySpan<char> two)
    {
        one = [];
        two = [];
        for (int i = 0; i < self.Length; i++)
        {
            if (self[i] == delimeter)
            {
                for (int j = i + 1; j < self.Length; j++)
                    if (self[j] == delimeter) return false;

                one = self[..i];
                two = self[(i + 1)..];
                return true;
            }
        }

        return false;
    }
}
