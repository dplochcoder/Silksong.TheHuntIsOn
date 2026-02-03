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

[BepInDependency("org.silksong-modding.datamanager")]
[BepInDependency("org.silksong-modding.modmenu")]
[BepInDependency("org.silksong-modding.prepatcher")]
[BepInDependency("ssmp")]
[BepInAutoPlugin(id: "io.github.silksong.thehuntison")]
public partial class TheHuntIsOnPlugin : BaseUnityPlugin, IModMenuCustomMenu, IGlobalDataMod<GlobalSaveData>
{
    private static TheHuntIsOnPlugin? instance;

    internal static void LogError(string message)
    {
        if (instance == null) return;
        instance.Logger.LogError(message);
    }

    private readonly Dictionary<string, ModuleBase> modules = [];

    // Can be updated from disk, from menu, or over SSMP.
    // EventSuppressors allow updates to propagate between the three sources without infinite cascade.
    private readonly EventSuppressor updateMenu = new();

    private void UpdateModulesGlobal()
    {
        foreach (var module in modules.Values)
            module.Enabled = GlobalData != null && GlobalData.Enabled && GlobalData.ModuleDataset.TryGetValue(module.Name, out var data) && data.IsEnabled(GlobalData.Role);
    }

    private void OnModuleDataset(ModuleDataset moduleDataset) => GlobalData = (GlobalData ?? new()) with { ModuleDataset = moduleDataset };

    public GlobalSaveData? GlobalData
    {
        get => field;
        set
        {
            field = value;
            UpdateMenu();
            UpdateModulesGlobal();
        }
    }

    internal static RoleId GetRole() => instance != null ? instance.GlobalData?.Role ?? RoleId.Hunter : RoleId.Hunter;

    internal static ModuleActivation GetModuleActivation(string name)
    {
        if (instance != null)
        {
            var global = instance.GlobalData;
            if (global != null && global.Enabled && global.ModuleDataset.TryGetValue(name, out var data)) return data.ModuleActivation;
        }

        return ModuleActivation.Inactive;
    }

    internal static T GetGlobalConfig<T>(string name) where T : ModuleSettings<T>, new()
    {
        if (instance != null)
        {
            var global = instance.GlobalData;
            if (global != null && global.ModuleDataset.TryGetValue(name, out var data) && data.GetSettings(global.Role) is T typed) return typed;
        }

        return new();
    }

    internal static T GetCosmeticConfig<T>(string name) where T : new()
    {
        if (instance != null && instance.GlobalData != null && instance.GlobalData.Cosmetics.Config.TryGetValue(name, out var data) && data is T typed) return typed;
        else return new();
    }

    internal static void SetCosmeticConfig<T>(string name, T config) where T : class
    {
        if (instance == null) return;

        instance.GlobalData ??= new();
        instance.GlobalData.Cosmetics.Config[name] = config;
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

        foreach (var module in ModuleBase.GetAllModules()) modules.Add(module.Name, module);

        ClientAddon.RegisterAddon(new HuntClientAddon());
        HuntClientAddon.On<ModuleDataset>.Received += OnModuleDataset;

        ServerAddon.RegisterAddon(new HuntServerAddon());

        Logger.LogInfo($"Plugin {Name} ({Id}) has loaded!");
    }

    public string ModMenuName() => "The Hunt is On";

    private GlobalSaveDataMenu? globalSaveDataMenu;

    private void UpdateMenu()
    {
        if (updateMenu.Suppressed) return;
        globalSaveDataMenu?.Apply(GlobalData ?? new());
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

        globalSaveDataMenu = new(GlobalData ?? new(), modules.Values);
        globalSaveDataMenu.OnGlobalSaveDataChanged += newSaveData =>
        {
            using (updateMenu.Suppress())
            {
                GlobalData = newSaveData;
            }
        };
        globalSaveDataMenu.AppendTo(screen);

        screen.Add(BuildCustomizeButton());

        return screen;
    }
}
