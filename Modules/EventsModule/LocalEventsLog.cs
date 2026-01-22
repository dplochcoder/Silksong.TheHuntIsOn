using Silksong.TheHuntIsOn.SsmpAddon.Packets;
using Silksong.TheHuntIsOn.Util;
using SSMP.Networking.Packet;
using System.Collections.Generic;

namespace Silksong.TheHuntIsOn.Modules.EventsModule;

internal class LocalEventsLog : NetworkedCloneable<LocalEventsLog>
{
    public HashSet<SpeedrunnerBoolEvent> RecordedBoolEvents = [];
    public HashSet<SpeedrunnerBoolEvent> PublishedBoolEvents = [];
    public Dictionary<SpeedrunnerCountType, int> RecordedCountEvents = [];
    public Dictionary<SpeedrunnerCountType, int> PublishedCountEvents = [];
    public HashSet<string> GrantedRewards = [];

    public override void ReadData(IPacket packet)
    {
        RecordedBoolEvents.ReadData(packet, packet => packet.ReadEnum<SpeedrunnerBoolEvent>());
        PublishedBoolEvents.ReadData(packet, packet => packet.ReadEnum<SpeedrunnerBoolEvent>());
        RecordedCountEvents.ReadData(packet, packet => packet.ReadEnum<SpeedrunnerCountType>(), packet => packet.ReadInt());
        PublishedCountEvents.ReadData(packet, packet => packet.ReadEnum<SpeedrunnerCountType>(), packet => packet.ReadInt());
        GrantedRewards.ReadData(packet, packet => packet.ReadString());
    }

    public override void WriteData(IPacket packet)
    {
        RecordedBoolEvents.WriteData(packet, (packet, value) => value.WriteData(packet));
        PublishedBoolEvents.WriteData(packet, (packet, value) => value.WriteData(packet));
        RecordedCountEvents.WriteData(packet, (packet, value) => value.WriteData(packet), (packet, value) => packet.Write(value));
        PublishedCountEvents.WriteData(packet, (packet, value) => value.WriteData(packet), (packet, value) => packet.Write(value));
        GrantedRewards.WriteData(packet, (packet, value) => value.WriteData(packet));
    }

    internal override NetworkedCloneable Clone() => new LocalEventsLog()
    {
        RecordedBoolEvents = [.. RecordedBoolEvents],
        PublishedBoolEvents = [.. PublishedBoolEvents],
        RecordedCountEvents = CloneDict(RecordedCountEvents),
        PublishedCountEvents = CloneDict(PublishedCountEvents),
        GrantedRewards = [.. GrantedRewards],
    };
}
