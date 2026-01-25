using Silksong.ModMenu.Elements;
using Silksong.TheHuntIsOn.Modules.Lib;
using System.Collections.Generic;

namespace Silksong.TheHuntIsOn.Menu;

internal interface IModuleSubMenu
{
    IEnumerable<MenuElement> Elements();

    void ApplyRaw(ModuleSettings? data);

    ModuleSettings ExportRaw();
}
