using SSMP.Networking.Packet;

namespace Silksong.TheHuntIsOn.Util;

internal class EmptyNetworkedCloneable<T> : NetworkedCloneable<T> where T : EmptyNetworkedCloneable<T>
{
    public override T Clone() => (T)this;

    public override void ReadData(IPacket packet) { }

    public override void WriteData(IPacket packet) { }
}
