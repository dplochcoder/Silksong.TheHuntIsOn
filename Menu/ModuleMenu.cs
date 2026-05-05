using System;
using System.Collections.Generic;
using Silksong.ModMenu.Elements;
using Silksong.ModMenu.Generator;
using Silksong.TheHuntIsOn.Modules.Lib;

namespace Silksong.TheHuntIsOn.Menu;

internal class ModuleMenu<T> : IModuleMenu
    where T : ModuleSettings<T>, new()
{
    public event Action? OnValueChanged;

    public readonly ICustomMenu<T> menu;

    public ModuleMenu(ICustomMenu<T> menu)
    {
        this.menu = menu;
        menu.OnValueChanged += _ => OnValueChanged?.Invoke();
    }

    public void ApplyRaw(ModuleSettings? settings)
    {
        if (settings is T typed)
            menu.ApplyFrom(typed);
    }

    public ModuleSettings ExportRaw()
    {
        T settings = new();
        menu.ExportTo(settings);
        return settings;
    }

    public IEnumerable<MenuElement> Elements() => menu.Elements();
}
