using Silksong.TheHuntIsOn.SsmpAddon.Packets;
using SSMP.Networking.Packet;
using System;
using System.Collections.Generic;

namespace Silksong.TheHuntIsOn.Util;

internal abstract class NetworkedCloneable : IWireInterface
{
    internal virtual NetworkedCloneable Clone() => (NetworkedCloneable)MemberwiseClone();

    public abstract void ReadData(IPacket packet);

    public abstract void WriteData(IPacket packet);
}

internal abstract class NetworkedCloneable<T> : NetworkedCloneable where T : NetworkedCloneable<T>
{
    internal virtual T CloneTyped() => (T)Clone();

    public T With(Action<T> edit)
    {
        T clone = CloneTyped();
        edit(clone);
        return clone;
    }

    protected static Dictionary<K, V> CloneDict<K, V>(IDictionary<K, V> dict)
    {
        Dictionary<K, V> clone = [];
        foreach (var e in dict) clone.Add(e.Key, e.Value);
        return clone;
    }

    #region IPacket extensions

    protected static void Read(ref bool value, IPacket packet) => value = packet.ReadBool();

    protected static void Read(ref int value, IPacket packet) => value = packet.ReadInt();

    protected static void Read(ref float value, IPacket packet) => value = packet.ReadFloat();

    protected static void Read(ref string value, IPacket packet) => value = packet.ReadString();

    protected static void Write<V>(V value, IPacket packet) where V : Enum => packet.Write(Convert.ToInt32(value));

    protected static void Read<V>(ref V value, IPacket packet) where V : Enum => value = (V)Enum.ToObject(typeof(V), packet.ReadInt());

    protected static void WriteSelf<V>(IPacket packet, V item) where V : IWireInterface => item.WriteData(packet);

    protected static V ReadSelf<V>(IPacket packet) where V : IWireInterface, new()
    {
        V item = new();
        item.ReadData(packet);
        return item;
    }

    #endregion
}
