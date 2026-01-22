using SSMP.Networking.Packet;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Silksong.TheHuntIsOn.SsmpAddon.Packets;

internal class Optional<T> : IWireInterface where T : new()
{
    public T? Value;

    public Optional() { }
    public Optional(T value) => Value = value;

    public bool HasValue => Value != null;

    public bool Get([MaybeNullWhen(false)] out T value)
    {
        if (Value != null)
        {
            value = Value;
            return true;
        }

        value = default;
        return false;
    }

    public void Clear() => Value = default;

    public void ReadData(IPacket packet)
    {
        if (packet.ReadBool())
        {
            Value = new();
            packet.ReadUsingReflection(Value);
        }
        else Value = default;
    }

    public void WriteData(IPacket packet)
    {
        packet.Write(Value != null);
        if (Value != null) packet.WriteUsingReflection(Value);
    }

    public static implicit operator Optional<T>(T data) => new(data);
}
