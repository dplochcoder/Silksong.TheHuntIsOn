using Silksong.TheHuntIsOn.SsmpAddon;
using Silksong.TheHuntIsOn.SsmpAddon.PacketUtil;
using SSMP.Networking.Packet;

namespace Silksong.TheHuntIsOn.Modules.ArchitectModule;

internal class RequestArchitectLevelData : IIdentifiedPacket<ServerPacketId>
{
    public ServerPacketId Identifier => ServerPacketId.RequestArchitectLevelData;

    public bool Single => false;

    public bool IsReliable => true;

    public bool DropReliableDataIfNewerExists => false;

    public string ArchitectGroupId = "";
    public string SceneName = "";

    public void ReadData(IPacket packet)
    {
        ArchitectGroupId = packet.ReadString();
        SceneName = packet.ReadString();
    }

    public void WriteData(IPacket packet)
    {
        ArchitectGroupId.WriteData(packet);
        SceneName.WriteData(packet);
    }
}
