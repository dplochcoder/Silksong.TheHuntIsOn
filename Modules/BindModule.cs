using HutongGames.PlayMaker;
using Silksong.FsmUtil;
using Silksong.ModMenu.Elements;
using Silksong.ModMenu.Models;
using Silksong.TheHuntIsOn.Menu;
using Silksong.TheHuntIsOn.Modules.Lib;
using Silksong.TheHuntIsOn.Util;
using System;
using System.Collections.Generic;

namespace Silksong.TheHuntIsOn.Modules;

internal class BindSettings : NetworkedCloneable<BindSettings>
{
    public int HealMasks = 3;
    public int MultibinderHealMasks = 2;
    public int SilkCost = 9;
    public float TimePenalty = 1f;
}

internal class BindModule : Module<BindModule, BindSettings, EmptySettings, BindMenu>
{
    protected override BindModule Self() => this;

    public override string Name => "Bind";

    private static void SetSilkCost(FsmInt fsmInt)
    {
        if (Instance != null) fsmInt.Value = Instance.GlobalConfig.SilkCost;
    }

    private static void EditBindFsm(PlayMakerFSM fsm)
    {
        fsm.GetState("Can Bind?")!.InsertMethod(0, IfEnabled(s => fsm.FsmVariables.GetFsmInt("Silk Cost").Value = s.SilkCost));
        fsm.GetState("Set Normal")!.InsertMethod(3, IfEnabled(s => fsm.FsmVariables.GetFsmInt("Heal Amount").Value = s.HealMasks));
        fsm.GetState("Multi Bind")!.AddMethod(IfEnabled(s => fsm.FsmVariables.GetFsmInt("Heal Amount").Value = s.MultibinderHealMasks));
        fsm.GetState("Bind Shared")!.InsertMethod(0, IfEnabled(s => fsm.FsmVariables.GetFsmFloat("Bind Time").Value *= s.TimePenalty));
    }

    private static Action<FsmStateAction> IfEnabled(Action<BindSettings> action) => _ =>
    {
        if (GetEnabledConfig(out var s)) action(s);
    };

    // FIXME: Fix UI
    static BindModule() => Events.AddFsmEdit("Hero_Hornet", "Bind", EditBindFsm);
}

internal class BindMenu : ModuleSubMenu<BindSettings>
{
    private readonly ChoiceElement<int> HealMasks = new("Heal Masks", CollectionUtil.IntRangeModel(0, 10), "Number of masks to heal when binding.");
    private readonly ChoiceElement<int> MultibinderHealMasks = new("Multibinder Heal Masks", CollectionUtil.IntRangeModel(0, 5), "Number of masks to heal when multi-binding.");
    private readonly ChoiceElement<int> SilkCost = new("Silk Cost", CollectionUtil.IntRangeModel(1, 18), "Number of silk spools required to bind.");
    private readonly ChoiceElement<float> TimePenalty = new("Time Penalty", ChoiceModels.ForValues([0.25f, 0.5f, 0.75f, 1f, 1.5f, 2f, 3f]), "Multiplier on the time it takes to bind.");

    public override IEnumerable<MenuElement> Elements() => [HealMasks, MultibinderHealMasks, SilkCost, TimePenalty];

    internal override void Apply(BindSettings data)
    {
        HealMasks.Value = data.HealMasks;
        MultibinderHealMasks.Value = data.MultibinderHealMasks;
        SilkCost.Value = data.SilkCost;
        TimePenalty.Value = data.TimePenalty;
    }

    internal override BindSettings Export() => new()
    {
        HealMasks = HealMasks.Value,
        MultibinderHealMasks = MultibinderHealMasks.Value,
        SilkCost = SilkCost.Value,
        TimePenalty = TimePenalty.Value,
    };
}
