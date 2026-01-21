using Silksong.TheHuntIsOn.Menu;
using Silksong.TheHuntIsOn.Util;

namespace Silksong.TheHuntIsOn.Modules;

internal abstract class Module<GlobalT, LocalT, SubMenuT> : ModuleBase where GlobalT : Cloneable<GlobalT>, new() where LocalT : Cloneable<LocalT>, new() where SubMenuT : ModuleSubMenu<GlobalT>, new()
{
    protected Module()
    {
        // FIXME: Events
    }

    protected GlobalT GlobalConfig => TheHuntIsOnPlugin.GetGlobalConfig<GlobalT>(Name, out GlobalT config) ? config : new();

    protected LocalT LocalConfig
    {
        get => TheHuntIsOnPlugin.GetLocalConfig<LocalT>(Name, out LocalT config) ? config : new();
        set => TheHuntIsOnPlugin.SetLocalConfig(Name, value);
    }

    public override IModuleSubMenu CreateSubMenu() => new SubMenuT();
}
