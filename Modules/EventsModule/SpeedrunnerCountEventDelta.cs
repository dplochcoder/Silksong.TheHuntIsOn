using Silksong.TheHuntIsOn.SsmpAddon.PacketUtil;
using SSMP.Networking.Packet;

namespace Silksong.TheHuntIsOn.Modules.EventsModule;

internal class SpeedrunnerCountEventDelta : IWireInterface
{
    public SpeedrunnerCountEventType Type;
    public int PrevCount;
    public int NewCount;

    public void ReadData(IPacket packet)
    {
        Type = packet.ReadEnum<SpeedrunnerCountEventType>();
        PrevCount = packet.ReadVarint();
        NewCount = packet.ReadVarint();
    }

    public void WriteData(IPacket packet)
    {
        Type.WriteData(packet);
        packet.WriteVarint(PrevCount);
        packet.WriteVarint(NewCount);
    }
}
