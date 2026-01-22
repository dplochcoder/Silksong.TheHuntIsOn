using Silksong.TheHuntIsOn.SsmpAddon.Packets;
using Silksong.TheHuntIsOn.Util;
using SSMP.Networking.Packet;
using System.Collections.Generic;

namespace Silksong.TheHuntIsOn;

internal class LocalSaveData : NetworkedCloneable<LocalSaveData>
{
    public Dictionary<string, Dynamic<NetworkedCloneable>> LocalData = [];

    public override void ReadData(IPacket packet) => LocalData.ReadData(packet, packet => packet.ReadString());

    public override void WriteData(IPacket packet) => LocalData.WriteData(packet, (packet, value) => value.WriteData(packet));

    internal override NetworkedCloneable Clone()
    {
        LocalSaveData clone = new();
        foreach (var e in LocalData) clone.LocalData.Add(e.Key, e.Value.CloneTyped());
        return clone;
    }
}
