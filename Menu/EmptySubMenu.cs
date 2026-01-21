using Silksong.ModMenu.Elements;
using Silksong.TheHuntIsOn.Modules;
using Silksong.TheHuntIsOn.Util;
using System.Collections.Generic;

namespace Silksong.TheHuntIsOn.Menu;

internal class EmptySubMenu : IModuleSubMenu
{
    public IEnumerable<MenuElement> Elements() => [];

    public void ApplyRaw(object? data) { }

    public Cloneable ExportRaw() => new EmptySettings();
}
