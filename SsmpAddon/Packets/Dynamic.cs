using Silksong.TheHuntIsOn.Util;
using SSMP.Networking.Packet;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Silksong.TheHuntIsOn.SsmpAddon.Packets;

// Wrapper around a value with a dynamic type.
internal class Dynamic<T> : NetworkedCloneable<Dynamic<T>> where T : NetworkedCloneable
{
    public T? Value;

    public Dynamic() { }
    public Dynamic(T? value) => Value = value;

    private static readonly Dictionary<Type, ConstructorInfo> constructors = [];
    private static T Construct(Type type)
    {
        if (!constructors.TryGetValue(type, out var constructor))
        {
            constructor = type.GetConstructor([]);
            constructors.Add(type, constructor);
        }

        return (T)constructor.Invoke([]);
    }

    public override void ReadData(IPacket packet)
    {
        bool exists = packet.ReadBool();
        if (!exists)
        {
            Value = default;
            return;
        }

        Type type = Type.GetType(packet.ReadString());
        Value = Construct(type);
        Value.ReadData(packet);
    }

    public override void WriteData(IPacket packet)
    {
        packet.Write(Value != null);
        if (Value == null) return;

        packet.Write(Value.GetType().FullName);
        Value.WriteData(packet);
    }

    internal override NetworkedCloneable Clone() => new Dynamic<T>()
    {
        Value = Value != null ? (T)Value.Clone() : null
    };
}
