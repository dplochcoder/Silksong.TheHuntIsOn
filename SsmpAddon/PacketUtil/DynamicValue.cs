using SSMP.Networking.Packet;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Silksong.TheHuntIsOn.SsmpAddon.PacketUtil;

// Wrapper around a value with a dynamic type.
internal class DynamicValue : NetworkedCloneable<DynamicValue>
{
    public INetworkedCloneable? Value;

    public DynamicValue() { }
    public DynamicValue(INetworkedCloneable? value) => Value = value;

    private static readonly Dictionary<Type, ConstructorInfo> constructors = [];
    private static INetworkedCloneable Construct(Type type)
    {
        if (!constructors.TryGetValue(type, out var constructor))
        {
            constructor = type.GetConstructor([]);
            constructors.Add(type, constructor);
        }

        return (INetworkedCloneable)constructor.Invoke([]);
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

    public override DynamicValue Clone() => new(Value?.CloneRaw());
}
