using Silksong.TheHuntIsOn.SsmpAddon;
using Silksong.TheHuntIsOn.SsmpAddon.PacketUtil;
using Silksong.TheHuntIsOn.Util;
using SSMP.Networking.Packet;

namespace Silksong.TheHuntIsOn.Modules.ArchitectModule;

internal class ArchitectLevelData : IIdentifiedPacket<ClientPacketId>
{
    public ClientPacketId Identifier => ClientPacketId.ArchitectLevelData;

    public bool Single => false;

    public bool IsReliable => true;

    public bool DropReliableDataIfNewerExists => false;

    public string ArchitectGroupId = "";
    public string SceneName = "";
    public string LevelData = "";
    public SHA1Hash LevelDataHash = new();

    public void ReadData(IPacket packet)
    {
        ArchitectGroupId = packet.ReadString();
        SceneName = packet.ReadString();
        LevelData = packet.ReadString();
        LevelDataHash.ReadData(packet);
    }

    public void WriteData(IPacket packet)
    {
        ArchitectGroupId.WriteData(packet);
        SceneName.WriteData(packet);
        LevelData.WriteData(packet);
        LevelDataHash.WriteData(packet);
    }
}
