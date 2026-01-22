using Silksong.ModMenu.Elements;
using Silksong.ModMenu.Models;
using Silksong.ModMenu.Screens;
using Silksong.TheHuntIsOn.Modules.Lib;
using Silksong.TheHuntIsOn.SsmpAddon;
using Silksong.TheHuntIsOn.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Silksong.TheHuntIsOn.Menu;

internal class GlobalSaveDataMenu
{
    private readonly ChoiceElement<bool> Enabled = new("Enable", ChoiceModels.ForBool("No", "Yes"), "Enabled this mod.");
    private readonly ChoiceElement<RoleId> Role = new("Role", ChoiceModels.ForEnum<RoleId>(), "Team to play on.");
    private readonly Dictionary<string, ModuleMultiMenu> ModuleMultiMenus = [];

    // Updated on incoming packet or on "Apply".
    private ModuleDataset currentModuleDataset;

    public event Action<GlobalSaveData>? OnGlobalSaveDataChanged;

    internal GlobalSaveDataMenu(GlobalSaveData globalSaveData, IEnumerable<ModuleBase> modules)
    {
        foreach (var module in modules)
        {
            ModuleMultiMenu menu = new(module);
            menu.Apply(globalSaveData.ModuleDataset.ModuleData.TryGetValue(module.Name, out var data) ? data : new());
            ModuleMultiMenus[module.Name] = menu;
        }

        currentModuleDataset = new(globalSaveData.ModuleDataset);
        Enabled.OnValueChanged += _ => InvokeSaveDataChanged();
        Role.OnValueChanged += _ => InvokeSaveDataChanged();
    }

    private readonly EventSuppressor saveDataChanged = new();

    private void InvokeSaveDataChanged()
    {
        if (saveDataChanged.Suppressed) return;
        OnGlobalSaveDataChanged?.Invoke(Export());
    }

    public void AppendTo(SimpleMenuScreen screen)
    {
        screen.Add(Enabled);
        screen.Add(Role);

        PaginatedMenuScreenBuilder modulesScreenBuilder = new("The Hunt is On - Modules");
        modulesScreenBuilder.AddRange(ModuleMultiMenus.OrderBy(e => e.Key).Select(e => e.Value.RootElement));
        var modulesScreen = modulesScreenBuilder.Build();

        TextButton modulesButton = new("Modules");
        modulesButton.OnSubmit += () => MenuScreenNavigation.Show(modulesScreen);
        screen.Add(modulesButton);

        TextButton applyButton = new("Apply Module Settings");
        applyButton.OnSubmit += () =>
        {
            ModuleDataset update = new();
            foreach (var e in ModuleMultiMenus) currentModuleDataset.ModuleData[e.Key] = e.Value.Export();

            if (HuntClientAddon.IsConnected) HuntClientAddon.Instance?.SendModuleDataset(update);
            else
            {
                currentModuleDataset = update;
                InvokeSaveDataChanged();
            }
        };
        screen.Add(applyButton);

        TextButton revertButton = new("Revert Module Settings");
        revertButton.OnSubmit += () => Apply(Export());
        screen.Add(revertButton);
    }

    public void Apply(GlobalSaveData globalSaveData)
    {
        using (saveDataChanged.Suppress())
        {
            Enabled.Value = globalSaveData.Enabled;
            Role.Value = globalSaveData.Role;

            foreach (var (name, data) in globalSaveData.ModuleDataset)
            {
                if (ModuleMultiMenus.TryGetValue(name, out var menu)) menu.Apply(data);
            }
        }
    }

    private GlobalSaveData Export() => new()
    {
        Enabled = Enabled.Value,
        Role = Role.Value,
        ModuleDataset = currentModuleDataset,
    };
}
