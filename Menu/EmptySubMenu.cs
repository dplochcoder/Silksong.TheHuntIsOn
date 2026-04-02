using System.Collections.Generic;
using Silksong.ModMenu.Elements;
using Silksong.TheHuntIsOn.Modules.Lib;

namespace Silksong.TheHuntIsOn.Menu;

internal class EmptySubMenu : ModuleSubMenu<EmptySettings>
{
    public override IEnumerable<MenuElement> Elements() => [];

    internal override void Apply(EmptySettings data) { }

    internal override EmptySettings Export() => new();
}
