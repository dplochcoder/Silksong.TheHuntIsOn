using Silksong.TheHuntIsOn.SsmpAddon.Packets;
using SSMP.Networking.Packet;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Silksong.TheHuntIsOn.Modules;

/// <summary>
/// Data representing which modules are enabled, with what settings, for which roles.
/// Stored in json in global save data, serialized in binary for SSMP.
/// </summary>
internal class ModuleDataset : IEnumerable<(string, ModuleData)>, IPacketData
{
    public ModuleDataset() { }
    internal ModuleDataset(ModuleDataset copy) {
        foreach (var (name, data) in copy) ModuleData[name] = new(data);
    }

    public Dictionary<string, ModuleData> ModuleData = [];

    private IEnumerable<(string, ModuleData)> Enumerate() => ModuleData.Select(e => (e.Key, e.Value));

    public IEnumerator<(string, ModuleData)> GetEnumerator() => Enumerate().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => Enumerate().GetEnumerator();

    public bool IsReliable => true;

    public bool DropReliableDataIfNewerExists => true;

    public void WriteData(IPacket packet) => packet.WriteDict(ModuleData);

    public void ReadData(IPacket packet) => ModuleData = packet.ReadDict<ModuleData>();
}
