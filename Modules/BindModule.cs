using HutongGames.PlayMaker;
using MonoDetour;
using MonoDetour.HookGen;
using PrepatcherPlugin;
using Silksong.FsmUtil;
using Silksong.ModMenu.Elements;
using Silksong.ModMenu.Models;
using Silksong.TheHuntIsOn.Menu;
using Silksong.TheHuntIsOn.Modules.Lib;
using Silksong.TheHuntIsOn.SsmpAddon.PacketUtil;
using Silksong.TheHuntIsOn.Util;
using SSMP.Networking.Packet;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Silksong.TheHuntIsOn.Modules;

internal class BindSettings : ModuleSettings<BindSettings>
{
    public override ModuleSettingsType DynamicType => ModuleSettingsType.Bind;

    public int HealMasks = 3;
    public int MultibinderHealMasks = 2;
    public int SilkCost = 9;
    public float TimePenalty = 1f;

    public override void ReadDynamicData(IPacket packet)
    {
        HealMasks.ReadData(packet);
        MultibinderHealMasks.ReadData(packet);
        SilkCost.ReadData(packet);
        TimePenalty.ReadData(packet);
    }

    public override void WriteDynamicData(IPacket packet)
    {
        HealMasks.WriteData(packet);
        MultibinderHealMasks.WriteData(packet);
        SilkCost.WriteData(packet);
        TimePenalty.WriteData(packet);
    }

    protected override bool Equivalent(BindSettings other) => HealMasks == other.HealMasks
        && MultibinderHealMasks == other.MultibinderHealMasks
        && SilkCost == other.SilkCost
        && TimePenalty == other.TimePenalty;
}

[MonoDetourTargets(typeof(SilkSpool))]
internal class BindModule : GlobalSettingsModule<BindModule, BindSettings, BindSubMenu>
{
    protected override BindModule Self() => this;

    public override string Name => "Bind";

    public override ModuleActivationType ModuleActivationType => ModuleActivationType.AnyConfiguration;

    protected override void OnGlobalConfigChanged(BindSettings before, BindSettings after)
    {
        if (before.SilkCost != after.SilkCost) UIEvents.UpdateSilk();
    }

    private static void SetSilkCost(FsmInt fsmInt)
    {
        if (GetEnabledConfig(out var config)) fsmInt.Value = config.SilkCost;
    }

    private static void EditBindFsm(PlayMakerFSM fsm)
    {
        fsm.GetState("Can Bind?")!.InsertAction(0, IfEnabled(s => fsm.FsmVariables.GetFsmInt("Silk Cost").Value = s.SilkCost));
        fsm.GetState("Set Normal")!.InsertAction(3, IfEnabled(s => fsm.FsmVariables.GetFsmInt("Heal Amount").Value = s.HealMasks));
        fsm.GetState("Multi Bind")!.AddAction(IfEnabled(s => fsm.FsmVariables.GetFsmInt("Heal Amount").Value = s.MultibinderHealMasks));
        fsm.GetState("Bind Shared")!.InsertAction(0, IfEnabled(s => fsm.FsmVariables.GetFsmFloat("Bind Time").Value *= s.TimePenalty));
    }

    private static void OverrideBindCost(ref float result)
    {
        if (PlayerData.instance.IsAnyCursed) return;
        if (GetEnabledConfig(out var config)) result = config.SilkCost;
    }

    static BindModule() => Events.AddFsmEdit("Hero_Hornet(Clone)", "Bind", EditBindFsm);

    [MonoDetourHookInitialize]
    private static void Hook() => Md.SilkSpool.get_BindCost.Postfix(OverrideBindCost);
}

internal class BindSubMenu : ModuleSubMenu<BindSettings>
{
    private static ListChoiceModel<float> PercentageModel(List<float> values) => new(values) { DisplayFn = (idx, value) => $"{value * 100:0.#}%" };

    private readonly ChoiceElement<int> HealMasks = new("Heal Masks", ChoiceModelUtil.IntRangeModel(0, 10).FormatIntDelta(3), "Number of masks to heal when binding.");
    private readonly ChoiceElement<int> MultibinderHealMasks = new("Multibinder Heal Masks", ChoiceModelUtil.IntRangeModel(0, 10).FormatIntDelta(2), "Number of masks to heal when multi-binding.");
    private readonly ChoiceElement<int> SilkCost = new("Silk Cost", ChoiceModelUtil.IntRangeModel(1, 18).FormatIntDelta(9), "Number of silk spools required to bind.");
    private readonly ChoiceElement<float> TimePenalty = new("Time Penalty", PercentageModel([0.25f, 0.5f, 0.75f, 1f, 1.5f, 2f, 3f]), "Multiplier on the time it takes to bind.");

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
