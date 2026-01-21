using Silksong.ModMenu.Elements;
using Silksong.ModMenu.Models;
using Silksong.TheHuntIsOn.Menu;
using Silksong.TheHuntIsOn.Util;
using System.Collections.Generic;

namespace Silksong.TheHuntIsOn.Modules;

internal class SpawnPointModule : Module<SpawnPointSettings, EmptySettings, SpawnPointMenu>
{
    public override string Name => "Spawn Point";
}

internal enum SpawnPoint
{
    MossGrotto,
    Bonebottom,
    Bellhart,
    Songclave,
}

internal class SpawnPointSettings : Cloneable<SpawnPointSettings>
{
    public SpawnPoint SpawnPoint = SpawnPoint.MossGrotto;
}

internal class SpawnPointMenu : ModuleSubMenu<SpawnPointSettings>
{
    private readonly ChoiceElement<SpawnPoint> SpawnPoint = new("Spawn Point", ChoiceModels.ForEnum<SpawnPoint>(), "Where to spawn initially.");

    public override IEnumerable<MenuElement> Elements() => [SpawnPoint];

    internal override void Apply(SpawnPointSettings data)
    {
        SpawnPoint.Value = data.SpawnPoint;
    }

    internal override SpawnPointSettings Export() => new()
    {
        SpawnPoint = this.SpawnPoint.Value,
    };
}
