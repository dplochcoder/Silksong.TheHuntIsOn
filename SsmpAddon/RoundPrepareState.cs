using System.Collections.Generic;
using Silksong.TheHuntIsOn.SsmpAddon.PacketUtil;
using SSMP.Networking.Packet;

namespace Silksong.TheHuntIsOn.SsmpAddon;

internal class RoundPrepareState : IIdentifiedPacket<ClientPacketId>
{
    public ClientPacketId Identifier => ClientPacketId.RoundPrepareState;

    public bool Single => true;

    public bool IsReliable => true;

    public bool DropReliableDataIfNewerExists => true;

    public bool IsPreparing;
    public List<string> ReadyPlayerNames = [];

    public void ReadData(IPacket packet)
    {
        IsPreparing = packet.ReadBool();
        int count = packet.ReadInt();
        ReadyPlayerNames = new(count);
        for (int i = 0; i < count; i++)
            ReadyPlayerNames.Add(packet.ReadString());
    }

    public void WriteData(IPacket packet)
    {
        packet.Write(IsPreparing);
        packet.Write(ReadyPlayerNames.Count);
        foreach (var name in ReadyPlayerNames)
            name.WriteData(packet);
    }
}
