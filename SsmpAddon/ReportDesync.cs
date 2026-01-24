using Silksong.TheHuntIsOn.SsmpAddon.PacketUtil;
using Silksong.TheHuntIsOn.Util;

namespace Silksong.TheHuntIsOn.SsmpAddon;

internal class ReportDesync : Empty<ReportDesync>, IIdentifiedPacket<ServerPacketId>
{
    public ServerPacketId Identifier => ServerPacketId.ReportDesync;

    public bool Single => true;

    public bool IsReliable => true;

    public bool DropReliableDataIfNewerExists => true;
}
