using Silksong.TheHuntIsOn.Util;
using SSMP.Networking.Packet;
using System;
using System.Collections.Generic;

namespace Silksong.TheHuntIsOn.SsmpAddon.PacketUtil;

internal class EnumSet<T> : HashSet<T>, INetworkedCloneable<EnumSet<T>> where T : Enum
{
    public EnumSet<T> Clone() => [.. this];

    public void ReadData(IPacket packet) => this.ReadData(packet, packet => packet.ReadEnum<T>());

    public void WriteData(IPacket packet) => this.WriteData(packet, (packet, value) => value.WriteData(packet));

    INetworkedCloneable INetworkedCloneable.CloneRaw() => Clone();

    Util.ICloneable Util.ICloneable.CloneRaw() => Clone();
}
