using SSMP.Networking.Packet;
using System;

namespace Silksong.TheHuntIsOn.SsmpAddon.Packets;

internal interface IIdentifiedPacket<E> : IPacketData where E : Enum
{
    E Identifier { get; }

    bool Single { get; }
}
