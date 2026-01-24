using Silksong.TheHuntIsOn.SsmpAddon.PacketUtil;
using SSMP.Networking.Packet;

namespace Silksong.TheHuntIsOn.Util;

internal interface INetworkedCloneable : ICloneable, IWireInterface
{
    new INetworkedCloneable CloneRaw();
}

internal interface INetworkedCloneable<T> : ICloneable<T>, INetworkedCloneable where T : INetworkedCloneable<T> { }

internal abstract class NetworkedCloneable<T> : Cloneable<T>, INetworkedCloneable<T> where T : NetworkedCloneable<T>
{
    public new INetworkedCloneable CloneRaw() => Clone();

    public abstract void ReadData(IPacket packet);

    public abstract void WriteData(IPacket packet);
}
