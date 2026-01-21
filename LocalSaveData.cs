using Silksong.TheHuntIsOn.Util;
using System.Collections.Generic;

namespace Silksong.TheHuntIsOn;

internal class LocalSaveData : Cloneable<LocalSaveData>
{
    public Dictionary<string, Cloneable> LocalData = [];

    internal override Cloneable Clone()
    {
        LocalSaveData clone = new();
        foreach (var e in LocalData) clone.LocalData.Add(e.Key, e.Value.Clone());
        return clone;
    }
}
