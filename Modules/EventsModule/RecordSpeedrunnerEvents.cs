using Silksong.TheHuntIsOn.SsmpAddon;
using Silksong.TheHuntIsOn.SsmpAddon.Packets;
using Silksong.TheHuntIsOn.Util;
using SSMP.Networking.Packet;
using System.Collections.Generic;

namespace Silksong.TheHuntIsOn.Modules.EventsModule;

internal class RecordSpeedrunnerEvents : NetworkedCloneable<RecordSpeedrunnerEvents>, IIdentifiedPacket<ServerPacketId>
{
    public ServerPacketId Identifier => ServerPacketId.RecordSpeedrunnerEvents;

    public bool Single => true;

    public bool IsReliable => true;

    public bool DropReliableDataIfNewerExists => true;

    // Event just triggered by a speedrunner.
    public List<SpeedrunnerBoolEvent> BoolEvents = [];
    public List<SpeedrunnerCountEvent> CountEvents = [];

    public override void ReadData(IPacket packet)
    {
        BoolEvents.ReadData(packet, packet => packet.ReadEnum<SpeedrunnerBoolEvent>());
        CountEvents.ReadData(packet);
    }

    public override void WriteData(IPacket packet)
    {
        BoolEvents.WriteData(packet, (packet, value) => value.WriteData(packet));
        CountEvents.WriteData(packet);
    }
}
