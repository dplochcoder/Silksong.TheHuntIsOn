using System;
using System.Collections.Generic;
using Silksong.ModMenu.Elements;
using Silksong.TheHuntIsOn.Modules.Lib;

namespace Silksong.TheHuntIsOn.Menu;

internal interface IModuleSubMenu
{
    event Action? OnDataUpdated;

    IEnumerable<MenuElement> Elements();

    void ApplyRaw(ModuleSettings? data);

    ModuleSettings ExportRaw();
}
