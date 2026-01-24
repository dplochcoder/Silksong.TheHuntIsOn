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

    internal static void ReadData<T>(this ICollection<T> self, IPacket packet, Func<IPacket, T> read)
    {
        self.Clear();

        int count = packet.ReadVarint();
        for (int i = 0; i < count; i++) self.Add(read(packet));
    }

    internal static void ReadData<T>(this ICollection<T> self, IPacket packet) where T : IWireInterface, new() => self.ReadData(packet, ReadIntoNew<T>);

    internal static void WriteData<T>(this ICollection<T> self, IPacket packet, Action<IPacket, T> write)
    {
        packet.WriteVarint(self.Count);
        foreach (var item in self) write(packet, item);
    }

    internal static void WriteData<T>(this ICollection<T> self, IPacket packet) where T : IWireInterface => self.WriteData(packet, Write<T>);

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

    internal static void WriteData<K, V>(this IDictionary<K, V> self, IPacket packet, Action<IPacket, K> writeKey, Action<IPacket, V> writeValue)
    {
        packet.WriteVarint(self.Count);
        foreach (var e in self)
        {
            writeKey(packet, e.Key);
            writeValue(packet, e.Value);
        }
    }

    internal static void WriteData<K, V>(this IDictionary<K, V> self, IPacket packet, Action<IPacket, K> writeKey) where V : IWireInterface => self.WriteData(packet, writeKey, Write<V>);
}
