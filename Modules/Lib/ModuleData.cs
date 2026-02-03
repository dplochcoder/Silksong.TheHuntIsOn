using Silksong.TheHuntIsOn.SsmpAddon.PacketUtil;
using SSMP.Networking.Packet;

namespace Silksong.TheHuntIsOn.Modules.Lib;

internal class ModuleData : NetworkedCloneable<ModuleData>
{
    public ModuleData() { }
    public ModuleData(ModuleActivation moduleActivation, ModuleSettings? speedrunnerSettings, ModuleSettings? hunterSettings, ModuleSettings? everyoneSettings)
    {
        ModuleActivation = moduleActivation;
        SpeedrunnerSettings = speedrunnerSettings;
        HunterSettings = hunterSettings;
        EveryoneSettings = everyoneSettings;
    }

    public ModuleActivation ModuleActivation;
    public ModuleSettings? SpeedrunnerSettings;
    public ModuleSettings? HunterSettings;
    public ModuleSettings? EveryoneSettings;

    private static ModuleSettings? ReadSettings(IPacket packet) => packet.ReadOptionalDynamic<ModuleSettingsType, ModuleSettings, ModuleSettingsFactory>();

    public override void ReadData(IPacket packet)
    {
        ModuleActivation = packet.ReadEnum<ModuleActivation>();
        SpeedrunnerSettings = ReadSettings(packet);
        HunterSettings = ReadSettings(packet);
        EveryoneSettings = ReadSettings(packet);
    }

    public override void WriteData(IPacket packet)
    {
        ModuleActivation.WriteData(packet);
        SpeedrunnerSettings.WriteOptionalDynamic(packet);
        HunterSettings.WriteOptionalDynamic(packet);
        EveryoneSettings.WriteOptionalDynamic(packet);
    }

    public override ModuleData Clone() => new(ModuleActivation, SpeedrunnerSettings?.Clone(), HunterSettings?.Clone(),  EveryoneSettings?.Clone());

    public bool IsEnabled(RoleId role) => ModuleActivation switch
    {
        ModuleActivation.Inactive => false,
        ModuleActivation.SpeedrunnerOnly => role == RoleId.Speedrunner,
        ModuleActivation.HuntersOnly => role == RoleId.Hunter,
        ModuleActivation.EveryoneSame => true,
        ModuleActivation.EveryoneDifferent => true,
        _ => false
    };

    public ModuleSettings? GetSettings(RoleId role) => ModuleActivation switch
    {
        ModuleActivation.Inactive => null,
        ModuleActivation.SpeedrunnerOnly => (role == RoleId.Speedrunner) ? SpeedrunnerSettings : null,
        ModuleActivation.HuntersOnly => (role == RoleId.Hunter) ? HunterSettings : null,
        ModuleActivation.EveryoneSame => EveryoneSettings,
        ModuleActivation.EveryoneDifferent => (role == RoleId.Speedrunner) ? SpeedrunnerSettings : HunterSettings,
        _ => null,
    };
}
