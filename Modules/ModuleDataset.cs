using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Silksong.TheHuntIsOn.Modules;

internal class ModuleDataset : IEnumerable<(string, ModuleData)>
{
    internal ModuleDataset() { }
    internal ModuleDataset(ModuleDataset copy) {
        foreach (var (name, data) in copy) ModuleData[name] = new(data);
    }

    internal Dictionary<string, ModuleData> ModuleData = [];

    private IEnumerable<(string, ModuleData)> Enumerate() => ModuleData.Select(e => (e.Key, e.Value));

    public IEnumerator<(string, ModuleData)> GetEnumerator() => Enumerate().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => Enumerate().GetEnumerator();
}
