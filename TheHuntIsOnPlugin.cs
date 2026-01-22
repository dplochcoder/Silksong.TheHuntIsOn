using BepInEx;
using MonoDetour;
using Silksong.DataManager;
using Silksong.ModMenu.Elements;
using Silksong.ModMenu.Plugin;
using Silksong.ModMenu.Screens;
using Silksong.TheHuntIsOn.Menu;
using Silksong.TheHuntIsOn.Modules.Lib;
using Silksong.TheHuntIsOn.SsmpAddon;
using Silksong.TheHuntIsOn.Util;
using SSMP.Api.Client;
using SSMP.Api.Server;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Silksong.TheHuntIsOn;

[BepInAutoPlugin(id: "io.github.silksong.thehuntison")]
public partial class TheHuntIsOnPlugin : BaseUnityPlugin, IModMenuCustomMenu, IGlobalDataMod<GlobalSaveData>, ISaveDataMod<LocalSaveData>
{
    private static TheHuntIsOnPlugin? instance;

    private readonly Dictionary<string, ModuleBase> modules = [];

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

    internal static RoleId GetRole() => instance != null ? instance.GlobalSaveData.Role : RoleId.Hunter;

    internal static ModuleActivation GetModuleActivation(string name)
    {
        if (instance != null)
        {
            var global = instance.GlobalSaveData;
            if (global.Enabled && global.ModuleDataset.ModuleData.TryGetValue(name, out var data)) return data.ModuleActivation;
        }

        return ModuleActivation.Inactive;
    }

    internal static T GetGlobalConfig<T>(string name) where T : NetworkedCloneable<T>, new()
    {
        if (instance != null)
        {
            var global = instance.GlobalSaveData;
            if (global.ModuleDataset.ModuleData.TryGetValue(name, out var data) && data.GetSettings(global.Role) is T typed) return typed;
        }

        return new();
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

    internal static T GetLocalConfig<T>(string name) where T : NetworkedCloneable<T>, new()
    {
        if (instance != null && instance.LocalSaveData != null && instance.LocalSaveData.LocalData.TryGetValue(name, out var data) && data is T typed) return typed;
        else return new();
    }

    internal static void SetLocalConfig<T>(string name, T config) where T : NetworkedCloneable<T>
    {
        if (instance == null) return;

        var local = instance.LocalSaveData?.CloneTyped() ?? new();
        local.LocalData[name].Value = config.Clone();
        instance.LocalSaveData = local;
    }

    internal static T GetCosmeticConfig<T>(string name) where T : new()
    {
        if (instance != null && instance.GlobalSaveData.Cosmetics.Config.TryGetValue(name, out var data) && data is T typed) return typed;
        else return new();
    }

    internal static void SetCosmeticConfig<T>(string name, T config) where T : class
    {
        if (instance == null) return;
        instance.GlobalSaveData.Cosmetics.Config[name] = config;
    }

    internal static void UpdateCosmeticConfig<T>(string name, Action<T> update) where T : class, new()
    {
        var config = GetCosmeticConfig<T>(name);
        update(config);
        SetCosmeticConfig<T>(name, config);
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

    private MenuElement BuildCustomizeButton()
    {
        PaginatedMenuScreenBuilder customizerScreenBuilder = new("The Hunt is On - Customize");
        foreach (var module in modules.OrderBy(e => e.Key).Select(e => e.Value))
        {
            List<MenuElement> elements = [.. module.CreateCosmeticsMenuElements()];
            if (elements.Count == 0) continue;

            PaginatedMenuScreenBuilder subMenuBuilder = new($"Customize - {module.Name}");
            subMenuBuilder.AddRange(elements);
            var subMenuScreen = subMenuBuilder.Build();

            TextButton subMenuButton = new(module.Name);
            subMenuButton.OnSubmit += () => MenuScreenNavigation.Show(subMenuScreen);
            customizerScreenBuilder.Add(subMenuButton);
        }
        var customizeScreen = customizerScreenBuilder.Build();

        TextButton customizeButton = new("Customize");
        customizeButton.OnSubmit += () => MenuScreenNavigation.Show(customizeScreen);
        return customizeButton;
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

        screen.Add(BuildCustomizeButton());

        return screen;
    }
}
