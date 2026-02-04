using System.Collections.Generic;

namespace Silksong.TheHuntIsOn.Util;

internal class Updater<T>(T value) where T : struct
{
    public T Value { get; private set; } = value;

    public bool Update(T newValue)
    {
        if (EqualityComparer<T>.Default.Equals(this.Value, newValue)) return false;

        Value = newValue;
        return true;
    }
}
