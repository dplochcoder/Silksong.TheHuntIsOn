using SSMP.Networking.Packet;

namespace Silksong.TheHuntIsOn.SsmpAddon.Packets;

internal interface IWireInterface
{
    void WriteData(IPacket packet);

    void ReadData(IPacket packet);
}
