using Silksong.ModMenu.Elements;
using Silksong.TheHuntIsOn.Util;
using System.Collections.Generic;

namespace Silksong.TheHuntIsOn.Menu;

internal interface IModuleSubMenu
{
    IEnumerable<MenuElement> Elements();

    void ApplyRaw(INetworkedCloneable? data);

    INetworkedCloneable ExportRaw();
}
