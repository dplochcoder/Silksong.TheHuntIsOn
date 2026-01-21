using Silksong.ModMenu.Elements;
using Silksong.ModMenu.Models;
using Silksong.TheHuntIsOn.Menu;
using Silksong.TheHuntIsOn.Util;
using System.Collections.Generic;

namespace Silksong.TheHuntIsOn.Modules;

internal class DeathPenaltyModule : Module<DeathPenaltySettings, EmptySettings, DeathPenaltyMenu>
{
    public override string Name => "DeathPenalty";
}

internal class DeathPenaltySettings : Cloneable<DeathPenaltySettings>
{
    public int RespawnTimer = 0;
    public bool SpawnCoccoon = true;
    public bool LoseRosaries = true;
    public bool LimitSilk = true;
}

internal class DeathPenaltyMenu : ModuleSubMenu<DeathPenaltySettings>
{
    private readonly ChoiceElement<int> RespawnTimer = new("Respawn Timer", ChoiceModels.ForValues([0, 10, 20, 30, 45, 60, 90, 120, 180, 300]), "Seconds to wait to respawn after death.");
    private readonly ChoiceElement<bool> SpawnCoccoon = new("Spawn Coccoon", ChoiceModels.ForBool(), "If false, don't spawn coccoons at all.");
    private readonly ChoiceElement<bool> LoseRosaries = new("Lose Rosaries", ChoiceModels.ForBool(), "If false, don't lose rosaries on death.");
    private readonly ChoiceElement<bool> LimitSilk = new("Limit Silk", ChoiceModels.ForBool(), "If false, don't restrict silk on death.");

    public DeathPenaltyMenu() => SpawnCoccoon.OnValueChanged += value =>
    {
        LoseRosaries.Interactable = value;
        LimitSilk.Interactable = value;
    };

    public override IEnumerable<MenuElement> Elements() => [SpawnCoccoon, LoseRosaries, LimitSilk];

    internal override void Apply(DeathPenaltySettings data)
    {
        RespawnTimer.Value = data.RespawnTimer;
        SpawnCoccoon.Value = data.SpawnCoccoon;
        LoseRosaries.Value = data.LoseRosaries;
        LimitSilk.Value = data.LimitSilk;
    }

    internal override DeathPenaltySettings Export() => new()
    {
        RespawnTimer = RespawnTimer.Value,
        SpawnCoccoon = SpawnCoccoon.Value,
        LoseRosaries = LoseRosaries.Value,
        LimitSilk = LimitSilk.Value,
    };
}
