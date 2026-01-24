using SSMP.Networking.Packet;

namespace Silksong.TheHuntIsOn.SsmpAddon.PacketUtil;

internal interface IWireInterface
{
    void WriteData(IPacket packet);

    void ReadData(IPacket packet);
}
