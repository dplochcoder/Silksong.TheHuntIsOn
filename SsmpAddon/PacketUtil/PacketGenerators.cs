using SSMP.Networking.Packet;
using SSMP.Networking.Packet.Data;
using System;
using System.Collections.Generic;

namespace Silksong.TheHuntIsOn.SsmpAddon.PacketUtil;

internal class PacketGenerators<E> where E : Enum
{
    private readonly Dictionary<E, Func<IPacketData>> generators = [];

    public void Register<T>() where T : IIdentifiedPacket<E>, new()
    {
        T template = new();
        generators.Add(template.Identifier, template.Single ? () => new T() : () => new PacketDataCollection<T>());
    }

    public IPacketData Instantiate(E id) => generators.TryGetValue(id, out var gen) ? gen() : throw new ArgumentException($"Unsupported id: {id}");
}
