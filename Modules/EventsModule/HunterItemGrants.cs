using Silksong.TheHuntIsOn.SsmpAddon;
using Silksong.TheHuntIsOn.SsmpAddon.PacketUtil;
using Silksong.TheHuntIsOn.Util;
using SSMP.Networking.Packet;
using System.Collections.Generic;

namespace Silksong.TheHuntIsOn.Modules.EventsModule;

internal class HunterItemGrants : IDeltaBase<HunterItemGrants, HunterItemGrantsDelta>, IIdentifiedPacket<ClientPacketId>
{
    public ClientPacketId Identifier => ClientPacketId.HunterItemGrants;

    public bool Single => false;

    public bool IsReliable => true;

    public bool DropReliableDataIfNewerExists => false;

    // Items to grant to hunter(s).
    public Dictionary<string, EnumList<HunterItemGrantType>> Grants = [];

    public void ReadData(IPacket packet) => Grants.ReadData(packet, packet => packet.ReadString());

    public void WriteData(IPacket packet) => Grants.WriteData(packet, (packet, value) => value.WriteData(packet));

    public void Clear() => Grants.Clear();

    public bool Update(HunterItemGrantsDelta delta, out bool desynced)
    {
        bool changed = false;

        foreach (var e in delta.Grants)
        {
            if (Grants.ContainsKey(e.Key)) continue;
            
            changed = true;
            Grants.Add(e.Key, e.Value.Clone());
        }

        desynced = delta.TotalGrants.HasValue && Grants.Count != delta.TotalGrants.Value;
        return changed;
    }

    public bool Update(HunterItemGrantsDelta delta) => Update(delta, out _);

    public HunterItemGrantsDelta DeltaFrom(HunterItemGrants deltaBase)
    {
        HunterItemGrantsDelta delta = new() { TotalGrants = Grants.Count };
        foreach (var e in Grants)
        {
            if (deltaBase.Grants.ContainsKey(e.Key)) continue;
            delta.Grants.Add(e.Key, e.Value.Clone());
        }
        return delta;
    }
}
