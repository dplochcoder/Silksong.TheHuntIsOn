using Silksong.PurenailUtil.Collections;
using SSMP.Networking.Packet;
using System;
using System.Collections.Generic;

namespace Silksong.TheHuntIsOn.SsmpAddon.PacketUtil;

internal static class IWireInterfaceExtensions
{
    internal static void ReadData(this ref bool self, IPacket packet) => self = packet.ReadBool();
    internal static void WriteData(this bool self, IPacket packet) => packet.Write(self);

    internal static void ReadData(this ref int self, IPacket packet) => self = packet.ReadInt();
    internal static void WriteData(this int self, IPacket packet) => packet.Write(self);

    internal static void ReadData(this ref int? self, IPacket packet) => self = packet.ReadBool() ? packet.ReadInt() : null;
    internal static void WriteData(this int? self, IPacket packet)
    {
        packet.Write(self.HasValue);
        if (self.HasValue) packet.Write(self.Value);
    }

    internal static int ReadVarint(this IPacket self)
    {
        int sum = 0;
        while (true)
        {
            var b = self.ReadByte();
            sum += b & 0x7f;

            if ((b & 0x80) != 0) sum *= 0x80;
            else break;
        }
        return sum;
    }

    internal static void WriteVarint(this IPacket self, int value)
    {
        if (value < 0) throw new ArgumentException($"{nameof(value)}: {value}");
        if (value == 0)
        {
            self.Write((byte)0);
            return;
        }

        while (value > 0)
        {
            byte b = (byte)(value & 0x7f);
            if (value >= 0x80)
            {
                b |= 0x80;
                self.Write(b);
            }
            else
            {
                self.Write(b);
                break;
            }
        }
    }

    internal static void ReadData(this ref float self, IPacket packet) => self = packet.ReadFloat();
    internal static void WriteData(this float self, IPacket packet) => packet.Write(self);

    internal static void ReadData(this ref long self, IPacket packet) => self = packet.ReadLong();
    internal static void WriteData(this long self, IPacket packet) => packet.Write(self);

    internal static void ReadData(this ref long? self, IPacket packet) => self = packet.ReadBool() ? packet.ReadLong() : null;
    internal static void WriteData(this long? self, IPacket packet)
    {
        packet.Write(self.HasValue);
        if (self.HasValue) packet.Write(self.Value);
    }

    internal static void WriteData(this string self, IPacket packet) => packet.Write(self);

    internal static T ReadEnum<T>(this IPacket self) where T : Enum => (T)Enum.ToObject(typeof(T), self.ReadVarint());
    internal static void WriteData<T>(this T self, IPacket packet) where T : Enum => packet.WriteVarint(Convert.ToInt32(self));

    private static T ReadIntoNew<T>(this IPacket self) where T : IWireInterface, new()
    {
        T item = new();
        item.ReadData(self);
        return item;
    }

    private static void Write<T>(this IPacket self, T value) where T : IWireInterface => value.WriteData(self);

    internal static T ReadDynamic<E, T, F>(this IPacket self) where E : Enum where T : IDynamicValue<E, T, F> where F : IDynamicValueFactory<E, T, F>, new()
    {
        var type = self.ReadEnum<E>();
        F factory = new();
        T instance = factory.Create(type);
        instance.ReadDynamicData(self);
        return instance;
    }

    internal static T? ReadOptionalDynamic<E, T, F>(this IPacket self) where E : Enum where T : class, IDynamicValue<E, T, F> where F : IDynamicValueFactory<E, T, F>, new() => self.ReadBool() ? self.ReadDynamic<E, T, F>() : null;

    internal static void WriteDynamic<E, T, F>(this IDynamicValue<E, T, F> self, IPacket packet) where E : Enum where T : IDynamicValue<E, T, F> where F : IDynamicValueFactory<E, T, F>, new()
    {
        self.DynamicType.WriteData(packet);
        self.WriteDynamicData(packet);
    }

    internal static void WriteOptionalDynamic<E, T, F>(this IDynamicValue<E, T, F>? self, IPacket packet) where E : Enum where T : class, IDynamicValue<E, T, F> where F : IDynamicValueFactory<E, T, F>, new()
    {
        packet.Write(self != null);
        if (self == null) return;

        self.WriteDynamic(packet);
    }

    internal static void ReadData<T>(this ICollection<T> self, IPacket packet, Func<IPacket, T> read)
    {
        self.Clear();

        int count = packet.ReadVarint();
        for (int i = 0; i < count; i++) self.Add(read(packet));
    }

    internal static void ReadData<T>(this ICollection<T> self, IPacket packet) where T : IWireInterface, new() => self.ReadData(packet, ReadIntoNew<T>);

    internal static void WriteData<T>(this IReadOnlyCollection<T> self, IPacket packet, Action<IPacket, T> write)
    {
        packet.WriteVarint(self.Count);
        foreach (var item in self) write(packet, item);
    }

    internal static void WriteData<T>(this IReadOnlyCollection<T> self, IPacket packet) where T : IWireInterface => self.WriteData(packet, Write<T>);

    internal static void ReadData<T>(this HashMultiset<T> self, IPacket packet, Func<IPacket, T> read)
    {
        self.Clear();

        int distinct = packet.ReadVarint();
        for (int i = 0; i < distinct; i++)
        {
            var item = read(packet);
            var count = packet.ReadVarint();
            self.Add(item, count);
        }
    }

    internal static void ReadData<T>(this HashMultiset<T> self, IPacket packet) where T : IWireInterface, new() => self.ReadData(packet, ReadIntoNew<T>);

    internal static void WriteData<T>(this HashMultiset<T> self, IPacket packet, Action<IPacket, T> write)
    {
        packet.WriteVarint(self.Distinct.Count);
        foreach (var (item, count) in self.Counts)
        {
            write(packet, item);
            packet.WriteVarint(count);
        }
    }

    internal static void ReadData<K, V>(this IDictionary<K, V> self, IPacket packet, Func<IPacket, K> readKey, Func<IPacket, V> readValue)
    {
        self.Clear();

        int count = packet.ReadVarint();
        for (int i = 0; i < count; i++)
        {
            var key = readKey(packet);
            var value = readValue(packet);
            self.Add(key, value);
        }
    }

    internal static void ReadData<K, V>(this IDictionary<K, V> self, IPacket packet, Func<IPacket, K> readKey) where V : IWireInterface, new() => self.ReadData(packet, readKey, ReadIntoNew<V>);

    internal static void WriteData<K, V>(this IReadOnlyDictionary<K, V> self, IPacket packet, Action<IPacket, K> writeKey, Action<IPacket, V> writeValue)
    {
        packet.WriteVarint(self.Count);
        foreach (var e in self)
        {
            writeKey(packet, e.Key);
            writeValue(packet, e.Value);
        }
    }

    internal static void WriteData<K, V>(this IReadOnlyDictionary<K, V> self, IPacket packet, Action<IPacket, K> writeKey) where V : IWireInterface => self.WriteData(packet, writeKey, Write<V>);

    internal static void ReadData<K, V>(this ListMultimap<K, V> self, IPacket packet, Func<IPacket, K> readKey, Func<IPacket, V> readValue)
    {
        int count = packet.ReadVarint();
        for (int i = 0; i < count; i++)
        {
            var key = readKey(packet);
            List<V> values = [];
            values.ReadData(packet, readValue);
            self.Add(key, values);
        }
    }

    internal static void ReadData<K, V>(this ListMultimap<K, V> self, IPacket packet, Func<IPacket, K> readKey) where V : IWireInterface, new() => self.ReadData(packet, readKey, ReadIntoNew<V>);

    internal static void WriteData<K, V>(this ListMultimap<K, V> self, IPacket packet, Action<IPacket, K> writeKey, Action<IPacket, V> writeValue)
    {
        packet.WriteVarint(self.Keys.Count);
        foreach (var (key, values) in self)
        {
            writeKey(packet, key);
            values.WriteData(packet, writeValue);
        }
    }

    internal static void WriteData<K, V>(this ListMultimap<K, V> self, IPacket packet, Action<IPacket, K> writeKey) where V : IWireInterface, new() => self.WriteData(packet, writeKey, Write<V>);
}
