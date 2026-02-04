using HutongGames.PlayMaker;
using Silksong.FsmUtil.Actions;
using Silksong.TheHuntIsOn.Menu;
using System;

namespace Silksong.TheHuntIsOn.Modules.Lib;

/// <summary>
/// Base class for all modules.
/// </summary>
/// <typeparam name="ModuleT">Self-referential type.</typeparam>
/// <typeparam name="GlobalT">Global settings type, sent over the network when updated.</typeparam>
/// <typeparam name="SubMenuT">Sub-menu class for global settings.</typeparam>
/// <typeparam name="CosmeticT">Cosmetic settings, stored globally but not sent over the network.</typeparam>
/// <typeparam name="LocalT">Local settings type, localized to the current save file.</typeparam>
internal abstract class Module<ModuleT, GlobalT, SubMenuT, CosmeticT> : ModuleBase where ModuleT : Module<ModuleT, GlobalT, SubMenuT, CosmeticT> where GlobalT : ModuleSettings<GlobalT>, new() where SubMenuT : ModuleSubMenu<GlobalT>, new() where CosmeticT : class, new()
{
    protected static ModuleT? Instance { get; private set; }

    protected Module() => Instance = Self();

    protected abstract ModuleT Self();

    protected GlobalT GlobalConfig => TheHuntIsOnPlugin.GetGlobalConfig<GlobalT>(Name);

    protected ModuleActivation ModuleActivation => TheHuntIsOnPlugin.GetModuleActivation(Name);

    protected static bool IsEnabled => Instance?.Enabled ?? false;

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

    public override void OnGlobalConfigChanged(ModuleSettings? before, ModuleSettings? after)
    {
        var b = (before as GlobalT) ?? new();
        var a = (after as GlobalT) ?? new();
        if (b.Equivalent(a)) return;

        OnGlobalConfigChanged(b, a);
    }

    protected virtual void OnGlobalConfigChanged(GlobalT before, GlobalT after) { }

    protected static FsmStateAction IfEnabled(Action<GlobalT> action) => new LambdaAction()
    {
        Method = () =>
        {
            if (GetEnabledConfig(out var config)) action(config);
        }
    };

    protected CosmeticT CosmeticConfig
    {
        get => TheHuntIsOnPlugin.GetCosmeticConfig<CosmeticT>(Name);
        set => TheHuntIsOnPlugin.SetCosmeticConfig(Name, value);
    }

    protected void UpdateCosmeticConfig(Action<CosmeticT> action) => TheHuntIsOnPlugin.UpdateCosmeticConfig(Name, action);

    public override IModuleSubMenu CreateGlobalDataSubMenu() => new SubMenuT();
}
