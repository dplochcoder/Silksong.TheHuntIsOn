using Silksong.TheHuntIsOn.SsmpAddon;
using Silksong.TheHuntIsOn.SsmpAddon.Packets;
using SSMP.Networking.Packet;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Silksong.TheHuntIsOn.Modules.Lib;

/// <summary>
/// Data representing which modules are enabled, with what settings, for which roles.
/// Stored in json in global save data, serialized in binary for SSMP.
/// </summary>
internal class ModuleDataset : IEnumerable<(string, ModuleData)>, IIdentifiedPacket<ClientPacketId>, IIdentifiedPacket<ServerPacketId>
{
    public ModuleDataset() { }
    internal ModuleDataset(ModuleDataset copy) {
        foreach (var (name, data) in copy) ModuleData[name] = new(data);
    }

    public Dictionary<string, ModuleData> ModuleData = [];

    public void WriteData(IPacket packet) => ModuleData.WriteData(packet, (packet, value) => value.WriteData(packet));

    public void ReadData(IPacket packet) => ModuleData.ReadData(packet, packet => packet.ReadString());

    private IEnumerable<(string, ModuleData)> Enumerate() => ModuleData.Select(e => (e.Key, e.Value));

    public IEnumerator<(string, ModuleData)> GetEnumerator() => Enumerate().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => Enumerate().GetEnumerator();

    ClientPacketId IIdentifiedPacket<ClientPacketId>.Identifier => ClientPacketId.ModuleDataset;

    ServerPacketId IIdentifiedPacket<ServerPacketId>.Identifier => ServerPacketId.ModuleDataset;

    public bool Single => true;

    public bool IsReliable => true;

    public bool DropReliableDataIfNewerExists => true;
}
