using Silksong.TheHuntIsOn.SsmpAddon;
using Silksong.TheHuntIsOn.SsmpAddon.PacketUtil;
using Silksong.TheHuntIsOn.Util;
using SSMP.Networking.Packet;
using System.Collections.Generic;

namespace Silksong.TheHuntIsOn.Modules.Lib;

/// <summary>
/// Data representing which modules are enabled, with what settings, for which roles.
/// Stored in json in global save data, serialized in binary for SSMP.
/// </summary>
internal class ModuleDataset : Dictionary<string, ModuleData>, INetworkedCloneable<ModuleDataset>, IIdentifiedPacket<ClientPacketId>, IIdentifiedPacket<ServerPacketId>
{
    public ModuleDataset() { }
    public ModuleDataset(IReadOnlyDictionary<string, ModuleData> data) : base(data.CloneDictDeep()) { }

    ClientPacketId IIdentifiedPacket<ClientPacketId>.Identifier => ClientPacketId.ModuleDataset;

    ServerPacketId IIdentifiedPacket<ServerPacketId>.Identifier => ServerPacketId.ModuleDataset;

    public bool Single => true;

    public bool IsReliable => true;

    public bool DropReliableDataIfNewerExists => true;

    public void WriteData(IPacket packet) => this.WriteData(packet, (packet, value) => value.WriteData(packet));

    public void ReadData(IPacket packet) => this.ReadData(packet, packet => packet.ReadString());

    public ModuleDataset Clone() => new(this);

    public INetworkedCloneable CloneRaw() => Clone();

    ICloneable ICloneable.CloneRaw() => Clone();
}
