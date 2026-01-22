using Silksong.TheHuntIsOn.SsmpAddon;
using Silksong.TheHuntIsOn.SsmpAddon.Packets;
using Silksong.TheHuntIsOn.Util;
using SSMP.Networking.Packet;
using System.Collections.Generic;

namespace Silksong.TheHuntIsOn.Modules.EventsModule;

internal class GrantHunterItem : NetworkedCloneable<GrantHunterItem>
{
    public string GrantId = "";
    public HunterItemGrantType Type;

    public override void ReadData(IPacket packet)
    {
        GrantId = packet.ReadString();
        Type = packet.ReadEnum<HunterItemGrantType>();
    }

    public override void WriteData(IPacket packet)
    {
        GrantId.WriteData(packet);
        Type.WriteData(packet);
    }
}

internal class GrantHunterItems : NetworkedCloneable<GrantHunterItems>, IIdentifiedPacket<ClientPacketId>
{
    public ClientPacketId Identifier => ClientPacketId.GrantHunterItems;

    public bool Single => true;

    public bool IsReliable => true;

    public bool DropReliableDataIfNewerExists => true;

    // Items to grant to hunter(s).
    public List<GrantHunterItem> Grants = [];
    // Total number of items granted this session.
    public int TotalGrants;

    public override void ReadData(IPacket packet)
    {
        Grants.ReadData(packet);
        TotalGrants.ReadData(packet);
    }

    public override void WriteData(IPacket packet)
    {
        Grants.WriteData(packet);
        TotalGrants.WriteData(packet);
    }
}
