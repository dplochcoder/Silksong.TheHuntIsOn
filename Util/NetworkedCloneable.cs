using Silksong.TheHuntIsOn.SsmpAddon.Packets;
using SSMP.Networking.Packet;
using System;

namespace Silksong.TheHuntIsOn.Util;

internal abstract class NetworkedCloneable : IWireInterface
{
    internal virtual NetworkedCloneable Clone() => (NetworkedCloneable)MemberwiseClone();

    public abstract void ReadData(IPacket packet);

    public abstract void WriteData(IPacket packet);
}

internal abstract class NetworkedCloneable<T> : NetworkedCloneable where T : NetworkedCloneable<T>
{
    internal virtual T CloneTyped() => (T)Clone();

    public override void ReadData(IPacket packet) => packet.ReadUsingReflection(this);

    public override void WriteData(IPacket packet) => packet.WriteUsingReflection(this);

    public T With(Action<T> edit)
    {
        T clone = CloneTyped();
        edit(clone);
        return clone;
    }
}
