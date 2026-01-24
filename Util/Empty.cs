using Silksong.TheHuntIsOn.SsmpAddon.PacketUtil;
using SSMP.Networking.Packet;

namespace Silksong.TheHuntIsOn.Util;

internal class Empty : NetworkedCloneable<Empty>
{
    public override Empty Clone() => this;

    public override void ReadData(IPacket packet) { }

    public override void WriteData(IPacket packet) { }
}

internal class Empty<T> : NetworkedCloneable<T> where T : Empty<T>
{
    public override T Clone() => (T)this;

    public override void ReadData(IPacket packet) { }

    public override void WriteData(IPacket packet) { }
}
