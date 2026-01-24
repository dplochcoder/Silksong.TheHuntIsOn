using Silksong.ModMenu.Elements;
using Silksong.TheHuntIsOn.Util;
using System.Collections.Generic;

namespace Silksong.TheHuntIsOn.Menu;

internal class EmptySubMenu : ModuleSubMenu<Empty>
{
    public override IEnumerable<MenuElement> Elements() => [];

    internal override void Apply(Empty data) { }

    internal override Empty Export() => new();
}
