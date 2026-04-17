using Silksong.TheHuntIsOn.SsmpAddon.PacketUtil;
using SSMP.Networking.Packet;

namespace Silksong.TheHuntIsOn.SsmpAddon;

internal class ReadyUp : IIdentifiedPacket<ServerPacketId>
{
    public ServerPacketId Identifier => ServerPacketId.ReadyUp;

    public bool Single => false;

    public bool IsReliable => true;

    public bool DropReliableDataIfNewerExists => false;

    public void ReadData(IPacket packet) { }

    public void WriteData(IPacket packet) { }
}
