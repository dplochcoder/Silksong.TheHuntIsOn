using Silksong.TheHuntIsOn.SsmpAddon.Packets;
using Silksong.TheHuntIsOn.Util;
using SSMP.Networking.Packet;

namespace Silksong.TheHuntIsOn.Modules;

internal class ModuleData(ModuleActivation moduleActivation, NetworkedCloneable? speedrunnerSettings, NetworkedCloneable? hunterSettings, NetworkedCloneable? everyoneSettings) : IWireInterface
{
    public ModuleData() : this(ModuleActivation.Inactive, null, null, null) { }
    internal ModuleData(ModuleData copy) : this(copy.ModuleActivation, copy.SpeedrunnerSettings, copy.HunterSettings, copy.EveryoneSettings) { }

    public ModuleActivation ModuleActivation = moduleActivation;
    public NetworkedCloneable? SpeedrunnerSettings = speedrunnerSettings;
    public NetworkedCloneable? HunterSettings = hunterSettings;
    public NetworkedCloneable? EveryoneSettings = everyoneSettings;

    public bool IsEnabled(RoleId role) => ModuleActivation switch
    {
        ModuleActivation.Inactive => false,
        ModuleActivation.SpeedrunnerOnly => role == RoleId.Speedrunner,
        ModuleActivation.HuntersOnly => role == RoleId.Hunter,
        ModuleActivation.EveryoneSame | ModuleActivation.EveryoneDifferent => true,
        _ => false
    };

    public NetworkedCloneable? GetSettings(RoleId role) => ModuleActivation switch
    {
        ModuleActivation.Inactive => null,
        ModuleActivation.SpeedrunnerOnly => (role == RoleId.Speedrunner) ? SpeedrunnerSettings : null,
        ModuleActivation.HuntersOnly => (role == RoleId.Hunter) ? HunterSettings : null,
        ModuleActivation.EveryoneSame => EveryoneSettings,
        ModuleActivation.EveryoneDifferent => (role == RoleId.Speedrunner) ? SpeedrunnerSettings : HunterSettings,
        _ => null,
    };

    public void WriteData(IPacket packet)
    {
        packet.WriteEnum(ModuleActivation);
        packet.WriteDynamic(SpeedrunnerSettings);
        packet.WriteDynamic(HunterSettings);
        packet.WriteDynamic(EveryoneSettings);
    }

    public void ReadData(IPacket packet)
    {
        ModuleActivation = packet.ReadEnum<ModuleActivation>();
        packet.ReadDynamic(out SpeedrunnerSettings);
        packet.ReadDynamic(out HunterSettings);
        packet.ReadDynamic(out EveryoneSettings);
    }
}
