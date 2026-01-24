using Silksong.TheHuntIsOn.SsmpAddon.PacketUtil;
using SSMP.Networking.Packet;

namespace Silksong.TheHuntIsOn.Modules.Lib;

internal class ModuleData : NetworkedCloneable<ModuleData>
{
    public ModuleData() { }
    public ModuleData(ModuleActivation moduleActivation, INetworkedCloneable? speedrunnerSettings, INetworkedCloneable? hunterSettings, INetworkedCloneable? everyoneSettings)
    {
        ModuleActivation = moduleActivation;
        SpeedrunnerSettings.Value = speedrunnerSettings;
        HunterSettings.Value = hunterSettings;
        EveryoneSettings.Value = everyoneSettings;
    }

    public ModuleActivation ModuleActivation = ModuleActivation.Inactive;
    public DynamicValue SpeedrunnerSettings = new();
    public DynamicValue HunterSettings = new();
    public DynamicValue EveryoneSettings = new();

    public override void ReadData(IPacket packet)
    {
        ModuleActivation = packet.ReadEnum<ModuleActivation>();
        SpeedrunnerSettings.ReadData(packet);
        HunterSettings.ReadData(packet);
        EveryoneSettings.ReadData(packet);
    }

    public override void WriteData(IPacket packet)
    {
        ModuleActivation.WriteData(packet);
        SpeedrunnerSettings.WriteData(packet);
        HunterSettings.WriteData(packet);
        EveryoneSettings.WriteData(packet);
    }

    public bool IsEnabled(RoleId role) => ModuleActivation switch
    {
        ModuleActivation.Inactive => false,
        ModuleActivation.SpeedrunnerOnly => role == RoleId.Speedrunner,
        ModuleActivation.HuntersOnly => role == RoleId.Hunter,
        ModuleActivation.EveryoneSame | ModuleActivation.EveryoneDifferent => true,
        _ => false
    };

    public INetworkedCloneable? GetSettings(RoleId role) => ModuleActivation switch
    {
        ModuleActivation.Inactive => null,
        ModuleActivation.SpeedrunnerOnly => (role == RoleId.Speedrunner) ? SpeedrunnerSettings.Value : null,
        ModuleActivation.HuntersOnly => (role == RoleId.Hunter) ? HunterSettings.Value : null,
        ModuleActivation.EveryoneSame => EveryoneSettings.Value,
        ModuleActivation.EveryoneDifferent => (role == RoleId.Speedrunner) ? SpeedrunnerSettings.Value : HunterSettings.Value,
        _ => null,
    };
}
