using Silksong.TheHuntIsOn.SsmpAddon;
using Silksong.TheHuntIsOn.SsmpAddon.PacketUtil;
using Silksong.TheHuntIsOn.Util;
using SSMP.Networking.Packet;
using System.Collections.Generic;

namespace Silksong.TheHuntIsOn.Modules.EventsModule;

internal class HunterItemGrantsDelta : IDelta<HunterItemGrants, HunterItemGrantsDelta>, IIdentifiedPacket<ClientPacketId>
{
    public ClientPacketId Identifier => ClientPacketId.HunterItemGrantsDelta;

    public bool Single => false;

    public bool IsReliable => true;

    public bool DropReliableDataIfNewerExists => false;

    // Items to grant to hunter(s).
    public Dictionary<string, EnumList<HunterItemGrantType>> Grants = [];
    // A synchronization count for the total number of grants this session.
    public int? TotalGrants;

    public bool IsEmpty => Grants.Count == 0;

    public void ReadData(IPacket packet)
    {
        Grants.ReadData(packet, packet => packet.ReadString());
        TotalGrants.ReadData(packet);
    }

    public void WriteData(IPacket packet)
    {
        Grants.WriteData(packet, (packet, value) => value.WriteData(packet));
        TotalGrants.WriteData(packet);
    }
}
