using Silksong.TheHuntIsOn.Menu;
using Silksong.TheHuntIsOn.Util;

namespace Silksong.TheHuntIsOn.Modules;

internal abstract class Module<ModuleT, GlobalT, LocalT, SubMenuT> : ModuleBase where ModuleT : Module<ModuleT, GlobalT, LocalT, SubMenuT> where GlobalT : Cloneable<GlobalT>, new() where LocalT : Cloneable<LocalT>, new() where SubMenuT : ModuleSubMenu<GlobalT>, new()
{
    protected static ModuleT? Instance { get; private set; }

    protected Module() => Instance = Self();

    protected abstract ModuleT Self();

    protected GlobalT GlobalConfig => TheHuntIsOnPlugin.GetGlobalConfig(Name, out GlobalT config) ? config : new();

    protected static bool GetEnabledConfig(out GlobalT config)
    {
        if (Instance == null || !Instance.Enabled)
        {
            config = new();
            return false;
        }

        config = Instance.GlobalConfig;
        return true;
    }

    protected LocalT LocalConfig
    {
        get => TheHuntIsOnPlugin.GetLocalConfig(Name, out LocalT config) ? config : new();
        set => TheHuntIsOnPlugin.SetLocalConfig(Name, value);
    }

    public override IModuleSubMenu CreateSubMenu() => new SubMenuT();
}
