using System;
using SSMP.Networking.Packet;

namespace Silksong.TheHuntIsOn.SsmpAddon.PacketUtil;

internal interface IIdentifiedPacket<E> : IPacketData
    where E : Enum
{
    E Identifier { get; }

    bool Single { get; }
}
