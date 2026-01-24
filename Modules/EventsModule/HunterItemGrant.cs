using Silksong.TheHuntIsOn.SsmpAddon.PacketUtil;
using SSMP.Networking.Packet;

namespace Silksong.TheHuntIsOn.Modules.EventsModule;

internal class HunterItemGrant : IWireInterface
{
    public string GrantId = "";
    public EnumList<HunterItemGrantType> Types = [];

    public void ReadData(IPacket packet)
    {
        GrantId = packet.ReadString();
        Types.ReadData(packet);
    }

    public void WriteData(IPacket packet)
    {
        GrantId.WriteData(packet);
        Types.WriteData(packet);
    }
}
