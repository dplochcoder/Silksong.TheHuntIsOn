using SSMP.Networking.Packet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Silksong.TheHuntIsOn.SsmpAddon.Packets;

internal static class WireInterfaceExtensions
{
    internal static void WriteEnum<T>(this IPacket self, T value) where T : Enum => self.Write(Convert.ToInt32(value));

    internal static T ReadEnum<T>(this IPacket self) where T : Enum => (T)Enum.ToObject(typeof(T), self.ReadInt());

    internal static void WriteDict<V>(this IPacket self, IReadOnlyDictionary<string, V> data) where V : IWireInterface
    {
        self.Write(data.Count);
        foreach (var e in data)
        {
            self.Write(e.Key);
            e.Value.WriteData(self);
        }
    }

    internal static Dictionary<string, V> ReadDict<V>(this IPacket self) where V : IWireInterface, new()
    {
        Dictionary<string, V> dict = [];

        int count = self.ReadInt();
        for (int i = 0; i < count; i++)
        {
            var key = self.ReadString();
            V value = new();
            value.ReadData(self);
            dict.Add(key, value);
        }

        return dict;
    }

    internal static void WriteDynamic<T>(this IPacket self, T? value) where T : IWireInterface
    {
        self.Write(value != null);
        if (value == null) return;

        self.Write(value.GetType().FullName);
        value.WriteData(self);
    }

    internal static void ReadDynamic<T>(this IPacket self, out T? value) where T : IWireInterface
    {
        bool exists = self.ReadBool();
        if (!exists)
        {
            value = default;
            return;
        }

        var type = Type.GetType(self.ReadString());
        value = (T)type.GetConstructor([]).Invoke([]);
        value.ReadData(self);
    }

    private static readonly Dictionary<Type, IEnumerable<FieldInfo>> fieldsByType = [];
    private static IEnumerable<FieldInfo> GetFields(Type type)
    {
        if (!fieldsByType.TryGetValue(type, out var fields))
        {
            fields = [.. type.GetFields(BindingFlags.Instance | BindingFlags.Public).OrderBy(f => f.Name)];
            fieldsByType.Add(type, fields);
        }

        return fields;
    }

    private static readonly Dictionary<Type, ConstructorInfo> constructors = [];
    private static object Instantiate(Type type)
    {
        if (!constructors.TryGetValue(type, out var constructor))
        {
            constructor = type.GetConstructor([]);
            constructors.Add(type, constructor);
        }

        return constructor.Invoke([]);
    }

    internal static void WriteUsingReflection(this IPacket self, object value)
    {
        var type = value.GetType();

        if (type.IsEnum) self.Write(Convert.ToInt32(value));
        else if (type == typeof(int)) self.Write((int)value);
        else if (type == typeof(bool)) self.Write((bool)value);
        else if (type == typeof(float)) self.Write((float)value);
        else if (type == typeof(string)) self.Write((string)value);
        else if (typeof(IWireInterface).IsAssignableFrom(type)) ((IWireInterface)value).WriteData(self);
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
        {
            var list = (IList)value;
            self.Write(list.Count);
            foreach (var obj in list) self.WriteUsingReflection(obj);
        }
        else if (type.IsClass)
        {
            foreach (var field in GetFields(type)) self.WriteUsingReflection(field.GetValue(value));
        }
        else throw new ArgumentException($"Unsupported type: {type.FullName}");
    }

    internal static void ReadUsingReflection(this IPacket self, object value)
    {
        var type = value.GetType();
        if (typeof(IWireInterface).IsAssignableFrom(type)) ((IWireInterface)value).ReadData(self);
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
        {
            var paramType = type.GetGenericArguments()[0];
            var list = (IList)value;
            list.Clear();

            int count = self.ReadInt();
            for (int i = 0; i < count; i++)
            {
                var element = Instantiate(paramType);
                self.ReadUsingReflection(element);
                list.Add(element);
            }
        }
        else if (type.IsClass)
        {
            foreach (var field in GetFields(type))
            {
                var fieldType = field.FieldType;

                if (fieldType.IsEnum) field.SetValue(value, Enum.ToObject(fieldType, self.ReadInt()));
                else if (fieldType == typeof(int)) field.SetValue(value, self.ReadInt());
                else if (fieldType == typeof(bool)) field.SetValue(value, self.ReadBool());
                else if (fieldType == typeof(float)) field.SetValue(value, self.ReadFloat());
                else if (fieldType == typeof(string)) field.SetValue(value, self.ReadString());
                else if (fieldType.IsClass) self.ReadUsingReflection(field.GetValue(value));
            }
        }
    }
}
