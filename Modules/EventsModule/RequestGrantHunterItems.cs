using Silksong.TheHuntIsOn.SsmpAddon;
using Silksong.TheHuntIsOn.SsmpAddon.Packets;
using Silksong.TheHuntIsOn.Util;
using SSMP.Networking.Packet;

namespace Silksong.TheHuntIsOn.Modules.EventsModule;

// Request up-to-date grant information for the current game session.
internal class RequestGrantHunterItems : NetworkedCloneable<RequestGrantHunterItems>, IIdentifiedPacket<ServerPacketId>
{
    public ServerPacketId Identifier => ServerPacketId.RequestGrantHunterItems;

    public bool Single => true;

    public bool IsReliable => true;

    public bool DropReliableDataIfNewerExists => true;

    // Number of grants already received by this player.
    public int GrantsReceived;

    public override void ReadData(IPacket packet) => GrantsReceived.ReadData(packet);

    public override void WriteData(IPacket packet) => GrantsReceived.WriteData(packet);
}
