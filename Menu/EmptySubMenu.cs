using Silksong.ModMenu.Elements;
using Silksong.TheHuntIsOn.Modules.Lib;
using System.Collections.Generic;

namespace Silksong.TheHuntIsOn.Menu;

internal class EmptySubMenu : ModuleSubMenu<EmptySettings>
{
    public override IEnumerable<MenuElement> Elements() => [];

    internal override void Apply(EmptySettings data) { }

    internal override EmptySettings Export() => new();
}
