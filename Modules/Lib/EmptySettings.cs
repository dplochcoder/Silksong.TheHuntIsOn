using SSMP.Networking.Packet;

namespace Silksong.TheHuntIsOn.Modules.Lib;

internal sealed class EmptySettings : ModuleSettings<EmptySettings>
{
    public override ModuleSettingsType DynamicType => ModuleSettingsType.Empty;

    public override ModuleSettings Clone() => this;

    protected override bool Equivalent(EmptySettings other) => true;

    public override void ReadDynamicData(IPacket packet) { }

    public override void WriteDynamicData(IPacket packet) { }
}
