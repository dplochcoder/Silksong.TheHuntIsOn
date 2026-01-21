using SSMP.Networking.Packet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection; 

namespace Silksong.TheHuntIsOn.SsmpAddon.Packets;

internal class ReflectionPacketHandler<T>
{
    private readonly List<FieldInfo> fields = [];

    internal ReflectionPacketHandler() => fields = [.. typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance).OrderBy(f => f.Name)];

    internal void WriteData(T instance, IPacket data)
    {
        foreach (var field in fields)
        {
            var type = field.FieldType;
            var value = field.GetValue(instance);

            if (type.IsEnum) data.Write(Convert.ToInt32(value));
            else if (type == typeof(int)) data.Write((int)value);
            else if (type == typeof(bool)) data.Write((bool)value);
            else if (type == typeof(float)) data.Write((float)value);
            else throw new ArgumentException($"Unsupported type: {type.FullName}");
        }
    }

    internal void ReadData(T instance, IPacket data)
    {
        foreach (var field in fields)
        {
            var type = field.FieldType;

            if (type.IsEnum) field.SetValue(instance, Enum.ToObject(type, data.ReadInt()));
            else if (type == typeof(int)) field.SetValue(instance, data.ReadInt());
            else if (type == typeof(bool)) field.SetValue(instance, data.ReadBool());
            else if (type == typeof(float)) field.SetValue(instance, data.ReadFloat());
            else throw new ArgumentException($"Unsupported type: {type.FullName}");
        }
    }
}
