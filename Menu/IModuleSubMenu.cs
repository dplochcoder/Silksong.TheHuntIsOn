using Silksong.ModMenu.Elements;
using Silksong.TheHuntIsOn.SsmpAddon.PacketUtil;
using System.Collections.Generic;

namespace Silksong.TheHuntIsOn.Menu;

internal interface IModuleSubMenu
{
    IEnumerable<MenuElement> Elements();

    void ApplyRaw(INetworkedCloneable? data);

    INetworkedCloneable ExportRaw();
}
