using System;
using System.Collections.Generic;
using Silksong.ModMenu.Elements;
using Silksong.TheHuntIsOn.Modules.Lib;

namespace Silksong.TheHuntIsOn.Menu;

internal abstract class ModuleSubMenu<T> : IModuleSubMenu
    where T : ModuleSettings<T>, new()
{
    public event Action? OnDataUpdated;

    protected void NotifyDataUpdated() => OnDataUpdated?.Invoke();

    public abstract IEnumerable<MenuElement> Elements();

    internal abstract void Apply(T data);

    public void ApplyRaw(ModuleSettings? data) => Apply(data is T typed ? typed : new());

    internal abstract T Export();

    public ModuleSettings ExportRaw() => Export();
}
