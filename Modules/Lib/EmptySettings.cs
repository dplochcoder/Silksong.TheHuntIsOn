using Silksong.TheHuntIsOn.Util;
using SSMP.Networking.Packet;

namespace Silksong.TheHuntIsOn.Modules.Lib;

/// <summary>
/// Settings class for no settings.
/// </summary>
internal class EmptySettings : NetworkedCloneable<EmptySettings>
{
    public override void ReadData(IPacket packet) { }

    public override void WriteData(IPacket packet) { }
}
