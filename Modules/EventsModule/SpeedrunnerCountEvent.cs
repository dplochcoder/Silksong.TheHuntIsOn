using Silksong.TheHuntIsOn.SsmpAddon.Packets;
using Silksong.TheHuntIsOn.Util;
using SSMP.Networking.Packet;

namespace Silksong.TheHuntIsOn.Modules.EventsModule;

// Speedrunner events tied to collecting a certain number of things.
internal enum SpeedrunnerCountType
{
    Masks,           // 2 - 10
    SilkSpools,      // 2 - 18
    SilkHearts,      // 1 - 3
    CraftingKits,    // 1 - 4
    ToolPouches,     // 1 - 4
    NeedleUpgrades,  // 1 - 4
    Melodies,        // 1 - 3
    Hearts,          // 1 - 4
}

internal class SpeedrunnerCountEvent : NetworkedCloneable<SpeedrunnerCountEvent>
{
    public SpeedrunnerCountType Type;
    public int Value;

    public override void ReadData(IPacket packet)
    {
        Type = packet.ReadEnum<SpeedrunnerCountType>();
        Value.ReadData(packet);
    }

    public override void WriteData(IPacket packet)
    {
        Type.WriteData(packet);
        Value.WriteData(packet);
    }
}