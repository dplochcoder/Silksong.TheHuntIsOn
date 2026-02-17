using Silksong.TheHuntIsOn.Modules.Lib;
using Silksong.TheHuntIsOn.SsmpAddon.PacketUtil;
using SSMP.Networking.Packet;

namespace Silksong.TheHuntIsOn.SsmpAddon;

internal class SeedModuleDataset : IIdentifiedPacket<ServerPacketId>
{
    public ServerPacketId Identifier => ServerPacketId.SeedModuleDataset;

    public bool Single => true;

    public bool IsReliable => true;

    public bool DropReliableDataIfNewerExists => true;

    public ModuleDataset ModuleDataset = [];

    public void ReadData(IPacket packet) => ModuleDataset.ReadData(packet);

    public void WriteData(IPacket packet) => ModuleDataset.WriteData(packet);
}
