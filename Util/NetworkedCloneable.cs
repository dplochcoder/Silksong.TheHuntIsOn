using Silksong.TheHuntIsOn.SsmpAddon.Packets;
using SSMP.Networking.Packet;

namespace Silksong.TheHuntIsOn.Util;

internal abstract class NetworkedCloneable : IWireInterface
{
    internal virtual NetworkedCloneable Clone() => (NetworkedCloneable)MemberwiseClone();

    public abstract void ReadData(IPacket packet);

    public abstract void WriteData(IPacket packet);
}

internal abstract class NetworkedCloneable<T> : NetworkedCloneable where T : NetworkedCloneable<T>
{
    private static readonly ReflectionPacketHandler<T> reflection = new();

    internal virtual T CloneTyped() => (T)Clone();

    public override void ReadData(IPacket packet) => reflection.ReadData((T)this, packet);

    public override void WriteData(IPacket packet) => reflection.WriteData((T)this, packet);
}
