using Silksong.PurenailUtil.Collections;
using SSMP.Networking.Packet;
using System;

namespace Silksong.TheHuntIsOn.SsmpAddon.PacketUtil;

internal class EnumMultiset<T> : HashMultiset<T>, INetworkedCloneable<EnumMultiset<T>> where T : Enum
{
    public EnumMultiset() { }
    public EnumMultiset(EnumMultiset<T> clone) : base(clone) { }

#pragma warning disable IDE0028 // Simplify collection initialization
    public EnumMultiset<T> Clone() => new(this);
#pragma warning restore IDE0028 // Simplify collection initialization

    public void ReadData(IPacket packet) => this.ReadData(packet, packet => packet.ReadEnum<T>());

    public void WriteData(IPacket packet) => this.WriteData(packet, (packet, value) => value.WriteData(packet));

    INetworkedCloneable INetworkedCloneable.CloneRaw() => Clone();

    Util.ICloneable Util.ICloneable.CloneRaw() => Clone();
}
