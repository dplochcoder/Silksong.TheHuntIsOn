using Silksong.ModMenu.Elements;
using Silksong.ModMenu.Models;
using Silksong.TheHuntIsOn.Menu;
using Silksong.TheHuntIsOn.Util;
using System.Collections.Generic;

namespace Silksong.TheHuntIsOn.Modules;

internal class BindModule : Module<BindSettings, EmptySettings, BindMenu>
{
    public override string Name => "Bind";
}

internal class BindSettings : Cloneable<BindSettings>
{
    public int HealMasks = 3;
    public int SilkCost = 9;
    public float TimePenalty = 1f;
}

internal class BindMenu : ModuleSubMenu<BindSettings>
{
    private readonly ChoiceElement<int> HealMasks = new("Heal Masks", CollectionUtil.IntRangeModel(0, 10), "Number of masks to heal when binding.");
    private readonly ChoiceElement<int> SilkCost = new("Silk Cost", CollectionUtil.IntRangeModel(1, 18), "Number of silk spools required to bind.");
    private readonly ChoiceElement<float> TimePenalty = new("Time Penalty", ChoiceModels.ForValues([0.25f, 0.5f, 1f, 1.5f, 2f, 3f]), "Multiplier for the time it takes to bind.");

    public override IEnumerable<MenuElement> Elements() => [HealMasks, SilkCost, TimePenalty];

    internal override void Apply(BindSettings data)
    {
        HealMasks.Value = data.HealMasks;
        SilkCost.Value = data.SilkCost;
        TimePenalty.Value = data.TimePenalty;
    }

    internal override BindSettings Export() => new()
    {
        HealMasks = HealMasks.Value,
        SilkCost = SilkCost.Value,
        TimePenalty = TimePenalty.Value,
    };
}
