using Silksong.TheHuntIsOn.SsmpAddon;
using Silksong.TheHuntIsOn.SsmpAddon.PacketUtil;
using Silksong.TheHuntIsOn.Util;
using SSMP.Networking.Packet;
using System.Collections.Generic;

namespace Silksong.TheHuntIsOn.Modules.EventsModule;

internal class SpeedrunnerEventsDelta : IDelta<SpeedrunnerEvents, SpeedrunnerEventsDelta>, IIdentifiedPacket<ClientPacketId>, IIdentifiedPacket<ServerPacketId>
{
    ClientPacketId IIdentifiedPacket<ClientPacketId>.Identifier => ClientPacketId.SpeedrunnerEventsDelta;

    ServerPacketId IIdentifiedPacket<ServerPacketId>.Identifier => ServerPacketId.SpeedrunnerEventsDelta;

    public bool Single => false;

    public bool IsReliable => true;

    public bool DropReliableDataIfNewerExists => false;

    // Singular acquisitions.
    public EnumSet<SpeedrunnerBoolEvent> BoolEvents = [];
    // Repeatable acquisitions.
    public List<SpeedrunnerCountEventDelta> CountEvents = [];

    public bool IsEmpty => BoolEvents.Count == 0 && CountEvents.Count == 0;

    public void ReadData(IPacket packet)
    {
        BoolEvents.ReadData(packet);
        CountEvents.ReadData(packet);
    }

    public void WriteData(IPacket packet)
    {
        BoolEvents.WriteData(packet);
        CountEvents.WriteData(packet);
    }
}
