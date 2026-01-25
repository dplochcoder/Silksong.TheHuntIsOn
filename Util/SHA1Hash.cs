using Silksong.TheHuntIsOn.SsmpAddon.PacketUtil;
using SSMP.Networking.Packet;
using System;
using System.IO;
using System.Text;

namespace Silksong.TheHuntIsOn.Util;

internal class SHA1Hash : NetworkedCloneable<SHA1Hash>, IEquatable<SHA1Hash>
{
    private const int NUM_BYTES = 20;

    public byte[] Hash = new byte[NUM_BYTES];

    public SHA1Hash() { }
    public SHA1Hash(byte[] bytes)
    {
        if (bytes.Length != NUM_BYTES) throw new ArgumentException($"{bytes.Length} != {NUM_BYTES}");
        Hash = bytes;
    }

    public override void ReadData(IPacket packet)
    {
        for (int i = 0; i < NUM_BYTES; i++) Hash[i] = packet.ReadByte();
    }

    public override void WriteData(IPacket packet)
    {
        for (int i = 0; i < NUM_BYTES; i++) packet.Write(Hash[i]);
    }

    public override SHA1Hash Clone()
    {
        SHA1Hash clone = new();
        Array.Copy(Hash, clone.Hash, NUM_BYTES);
        return clone;
    }

    internal static SHA1Hash Compute(string data)
    {
        var sha1 = System.Security.Cryptography.SHA1.Create();
        return new(sha1.ComputeHash(Encoding.UTF8.GetBytes(data)));
    }

    internal static SHA1Hash Compute(Stream data)
    {
        var sha1 = System.Security.Cryptography.SHA1.Create();
        return new(sha1.ComputeHash(data));
    }

    public bool Equals(SHA1Hash other)
    {
        for (int i = 0; i < NUM_BYTES; i++)
        {
            if (Hash[i] != other.Hash[i]) return false;
        }
        return true;
    }

    public override int GetHashCode()
    {
        int hash = 0;
        for (int i = 0; i < NUM_BYTES; i += 4) hash ^= BitConverter.ToInt32(Hash.AsSpan()[i..]);
        return hash;
    }

    public override bool Equals(object obj) => (obj is SHA1Hash other) && Equals(other);
}
