using Silksong.TheHuntIsOn.Util;
using System.Collections.Generic;

namespace Silksong.TheHuntIsOn;

internal class LocalSaveData : NetworkedCloneable<LocalSaveData>
{
    public Dictionary<string, NetworkedCloneable> LocalData = [];

    internal override NetworkedCloneable Clone()
    {
        LocalSaveData clone = new();
        foreach (var e in LocalData) clone.LocalData.Add(e.Key, e.Value.Clone());
        return clone;
    }
}
