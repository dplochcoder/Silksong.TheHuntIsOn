using Silksong.TheHuntIsOn.SsmpAddon.Packets;
using Silksong.TheHuntIsOn.Util;
using SSMP.Networking.Packet;

namespace Silksong.TheHuntIsOn.Modules.Lib;

internal class ModuleData(ModuleActivation moduleActivation, NetworkedCloneable? speedrunnerSettings, NetworkedCloneable? hunterSettings, NetworkedCloneable? everyoneSettings) : IWireInterface
{
    public ModuleData() : this(ModuleActivation.Inactive, null, null, null) { }
    internal ModuleData(ModuleData copy) : this(copy.ModuleActivation, copy.SpeedrunnerSettings, copy.HunterSettings, copy.EveryoneSettings) { }

    public ModuleActivation ModuleActivation = moduleActivation;
    public Dynamic<NetworkedCloneable> SpeedrunnerSettings = new(speedrunnerSettings);
    public Dynamic<NetworkedCloneable> HunterSettings = new(hunterSettings);
    public Dynamic<NetworkedCloneable> EveryoneSettings = new(everyoneSettings);

    public void WriteData(IPacket packet)
    {
        ModuleActivation.WriteData(packet);
        SpeedrunnerSettings.WriteData(packet);
        HunterSettings.WriteData(packet);
        EveryoneSettings.WriteData(packet);
    }

    public void ReadData(IPacket packet)
    {
        ModuleActivation = packet.ReadEnum<ModuleActivation>();
        SpeedrunnerSettings.ReadData(packet);
        HunterSettings.ReadData(packet);
        EveryoneSettings.ReadData(packet);
    }

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
}
