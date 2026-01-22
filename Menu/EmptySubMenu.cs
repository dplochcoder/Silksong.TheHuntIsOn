using Silksong.ModMenu.Elements;
using Silksong.TheHuntIsOn.Modules.Lib;
using Silksong.TheHuntIsOn.Util;
using System.Collections.Generic;

namespace Silksong.TheHuntIsOn.Menu;

internal class EmptySubMenu : IModuleSubMenu
{
    public IEnumerable<MenuElement> Elements() => [];

    public void ApplyRaw(NetworkedCloneable? data) { }

    public NetworkedCloneable ExportRaw() => new EmptySettings();
}
