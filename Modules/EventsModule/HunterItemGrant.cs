using Silksong.TheHuntIsOn.SsmpAddon.PacketUtil;
using Silksong.TheHuntIsOn.Util;
using SSMP.Networking.Packet;

namespace Silksong.TheHuntIsOn.Modules.EventsModule;

internal class HunterItemGrant : NetworkedCloneable<HunterItemGrant>
{
    public string GrantId = "";
    public EnumList<HunterItemGrantType> Types = [];

    public override void ReadData(IPacket packet)
    {
        GrantId = packet.ReadString();
        Types.ReadData(packet);
    }

    public override void WriteData(IPacket packet)
    {
        GrantId.WriteData(packet);
        Types.WriteData(packet);
    }
}
