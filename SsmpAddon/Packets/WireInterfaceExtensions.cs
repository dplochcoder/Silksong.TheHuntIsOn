using SSMP.Networking.Packet;
using System;
using System.Collections.Generic;

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
}
