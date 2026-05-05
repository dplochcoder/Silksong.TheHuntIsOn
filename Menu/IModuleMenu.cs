using System;
using System.Collections.Generic;
using Silksong.ModMenu.Elements;
using Silksong.TheHuntIsOn.Modules.Lib;

namespace Silksong.TheHuntIsOn.Menu;

internal interface IModuleMenu
{
    public event Action? OnValueChanged;

    public IEnumerable<MenuElement> Elements();

    public void ApplyRaw(ModuleSettings? settings);

    public ModuleSettings ExportRaw();
}
