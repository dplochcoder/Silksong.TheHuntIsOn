using Silksong.TheHuntIsOn.SsmpAddon.PacketUtil;
using SSMP.Networking.Packet;
using System;

namespace Silksong.TheHuntIsOn.Modules.EventsModule;

internal class SpeedrunnerCountEvent(SpeedrunnerCountEventType type, int value) : IWireInterface
{
    public static bool TryParse(string s, out SpeedrunnerCountEvent result)
    {
        result = new();
        for (int i = 0; i < s.Length; i++)
        {
            if (!char.IsDigit(s[i])) continue;
            return Enum.TryParse(s[..i], out result.Type) && int.TryParse(s[i..], out result.Value);
        }

        return false;
    }

    public SpeedrunnerCountEvent() : this(default, 0) { }

    public SpeedrunnerCountEventType Type = type;
    public int Value = value;

    public void ReadData(IPacket packet)
    {
        Type = packet.ReadEnum<SpeedrunnerCountEventType>();
        Value.ReadData(packet);
    }

    public void WriteData(IPacket packet)
    {
        Type.WriteData(packet);
        Value.WriteData(packet);
    }

    public override int GetHashCode() => Type.GetHashCode() ^ Value.GetHashCode();

    public override bool Equals(object obj) => (obj is SpeedrunnerCountEvent other) && Type == other.Type && Value == other.Value;
}
