using BepInEx;
using MonoDetour;
using Silksong.DataManager;
using Silksong.ModMenu.Plugin;
using Silksong.ModMenu.Screens;
using Silksong.TheHuntIsOn.Menu;
using Silksong.TheHuntIsOn.Modules;
using Silksong.TheHuntIsOn.SsmpAddon;
using Silksong.TheHuntIsOn.Util;
using SSMP.Api.Client;
using SSMP.Api.Server;
using System.Collections.Generic;

namespace Silksong.TheHuntIsOn;

[BepInAutoPlugin(id: "io.github.silksong.thehuntison")]
public partial class TheHuntIsOnPlugin : BaseUnityPlugin, IModMenuCustomMenu, IGlobalDataMod<GlobalSaveData>, ISaveDataMod<LocalSaveData>
{
    private static TheHuntIsOnPlugin? instance;

    private Dictionary<string, ModuleBase> modules = [];

    // Can be updated from disk, from menu, or over SSMP.
    // EventSuppressors allow updates to propagate between the three sources without infinite cascade.
    private readonly EventSuppressor updateMenu = new();

    private void UpdateModulesGlobal()
    {
        foreach (var module in modules.Values)
        {
            module.Enabled = GlobalSaveData.Enabled && GlobalSaveData.ModuleDataset.ModuleData.TryGetValue(module.Name, out var data) && data.IsEnabled(GlobalSaveData.Role);
            module.OnGlobalConfigUpdated();
        }
    }

    private void UpdateModulesLocal()
    {
        foreach (var module in modules.Values) module.OnLocalConfigUpdated();
    }

    private void OnModuleDatasetUpdate(ModuleDataset moduleDataset) => GlobalSaveData = GlobalSaveData with { ModuleDataset = moduleDataset };

    private GlobalSaveData GlobalSaveData
    {
        get => field;
        set
        {
            field = value;
            UpdateMenu();
            UpdateModulesGlobal();
        }
    } = new();

    GlobalSaveData? IGlobalDataMod<GlobalSaveData>.GlobalData
    {
        get => GlobalSaveData;
        set => GlobalSaveData = value ?? new();
    }

    internal static bool GetGlobalConfig<T>(string name, out T config) where T : NetworkedCloneable<T>, new()
    {
        if (instance != null)
        {
            var global = instance.GlobalSaveData;
            if (global.ModuleDataset.ModuleData.TryGetValue(name, out var data) && data.GetSettings(global.Role) is T typed)
            {
                config = typed;
                return true;
            }
        }

        config = new();
        return false;
    }

    private LocalSaveData? LocalSaveData
    {
        get => field;
        set
        {
            field = value;
            UpdateModulesLocal();
        }
    }

    LocalSaveData? ISaveDataMod<LocalSaveData>.SaveData { get => LocalSaveData; set => LocalSaveData = value; }

    internal static bool GetLocalConfig<T>(string name, out T config) where T : NetworkedCloneable<T>, new()
    {
        if (instance != null)
        {
            var local = instance.LocalSaveData;
            if (local != null && local.LocalData.TryGetValue(name, out var data) && data is T typed)
            {
                config = typed;
                return true;
            }
        }

        config = new();
        return false;
    }

    internal static void SetLocalConfig<T>(string name, T config) where T : NetworkedCloneable<T>
    {
        if (instance == null) return;

        var local = instance.LocalSaveData?.CloneTyped() ?? new();
        local.LocalData[name] = config.Clone();
        instance.LocalSaveData = local;
    }

    private void Awake()
    {
        instance = this;

        MonoDetourManager.InvokeHookInitializers(typeof(TheHuntIsOnPlugin).Assembly);

        foreach (var module in ModuleBase.GetAllModulesInAssembly()) modules.Add(module.Name, module);

        ClientAddon.RegisterAddon(new HuntClientAddon());
        HuntClientAddon.OnModuleDatasetUpdate += OnModuleDatasetUpdate;

        ServerAddon.RegisterAddon(new HuntServerAddon());

        Logger.LogInfo($"Plugin {Name} ({Id}) has loaded!");
    }

    public string ModMenuName() => "The Hunt is On";

    private GlobalSaveDataMenu? globalSaveDataMenu;

    private void UpdateMenu()
    {
        if (updateMenu.Suppressed) return;
        globalSaveDataMenu?.Apply(GlobalSaveData);
    }

    public AbstractMenuScreen BuildCustomMenu()
    {
        SimpleMenuScreen screen = new("The Hunt is On");
        screen.OnDispose += () => globalSaveDataMenu = null;

        globalSaveDataMenu = new(GlobalSaveData, modules.Values);
        globalSaveDataMenu.OnGlobalSaveDataChanged += newSaveData =>
        {
            using (updateMenu.Suppress())
            {
                GlobalSaveData = newSaveData;
            }
        };

        globalSaveDataMenu.AppendTo(screen);
        return screen;
    }
}
