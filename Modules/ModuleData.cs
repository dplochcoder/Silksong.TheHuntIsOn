using Silksong.TheHuntIsOn.Util;

namespace Silksong.TheHuntIsOn.Modules;

internal class ModuleData(ModuleActivation moduleActivation, Cloneable? speedrunnerSettings, Cloneable? hunterSettings, Cloneable? everyoneSettings)
{
    internal ModuleData() : this(ModuleActivation.Inactive, null, null, null) { }
    internal ModuleData(ModuleData copy) : this(copy.ModuleActivation, copy.SpeedrunnerSettings, copy.HunterSettings, copy.EveryoneSettings) { }

    public ModuleActivation ModuleActivation = moduleActivation;
    public Cloneable? SpeedrunnerSettings = speedrunnerSettings;
    public Cloneable? HunterSettings = hunterSettings;
    public Cloneable? EveryoneSettings = everyoneSettings;

    public bool IsEnabled(RoleId role) => ModuleActivation switch
    {
        ModuleActivation.Inactive => false,
        ModuleActivation.SpeedrunnerOnly => role == RoleId.Speedrunner,
        ModuleActivation.HuntersOnly => role == RoleId.Hunter,
        ModuleActivation.EveryoneSame | ModuleActivation.EveryoneDifferent => true,
        _ => false
    };

    public Cloneable? GetSettings(RoleId role) => ModuleActivation switch
    {
        ModuleActivation.Inactive => null,
        ModuleActivation.SpeedrunnerOnly => (role == RoleId.Speedrunner) ? SpeedrunnerSettings : null,
        ModuleActivation.HuntersOnly => (role == RoleId.Hunter) ? HunterSettings : null,
        ModuleActivation.EveryoneSame => EveryoneSettings,
        ModuleActivation.EveryoneDifferent => (role == RoleId.Speedrunner) ? SpeedrunnerSettings : HunterSettings,
        _ => null,
    };
}
