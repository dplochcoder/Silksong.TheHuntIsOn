using System.ComponentModel;
using MonoDetour;
using MonoDetour.HookGen;
using Silksong.FsmUtil;
using Silksong.ModMenu.Generator;
using Silksong.TheHuntIsOn.Modules.Lib;
using Silksong.TheHuntIsOn.SsmpAddon.PacketUtil;
using Silksong.TheHuntIsOn.Util;
using SSMP.Networking.Packet;

namespace Silksong.TheHuntIsOn.Modules;

[GenerateMenu]
public class BindSettings : ModuleSettings<BindSettings>
{
    public override ModuleSettingsType DynamicType() => ModuleSettingsType.Bind;

    [Description("Number of masks to heal when binding.")]
    [ModMenuOptions(0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10)]
    public int HealMasks = 3;

    [Description("Number of masks to heal when multi-binding.")]
    [ModMenuOptions(0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10)]
    public int MultibinderHealMasks = 2;

    [Description("Number of silk spools required to bind.")]
    [ModMenuOptions(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18)]
    public int SilkCost = 9;

    [Description("Multiplier on the time it takes to bind.")]
    [ModMenuOptions(0.25f, 0.5f, 0.75f, 1f, 1.5f, 2f, 3f)]
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

    protected override bool Equivalent(BindSettings other) =>
        HealMasks == other.HealMasks
        && MultibinderHealMasks == other.MultibinderHealMasks
        && SilkCost == other.SilkCost
        && TimePenalty == other.TimePenalty;
}

[MonoDetourTargets(typeof(SilkSpool))]
internal class BindModule : GlobalSettingsModule<BindModule, BindSettings, BindSettingsMenu>
{
    protected override BindModule Self() => this;

    public override string Name => "Bind";

    public override ModuleActivationType ModuleActivationType =>
        ModuleActivationType.AnyConfiguration;

    protected override void OnGlobalConfigChanged(BindSettings before, BindSettings after)
    {
        if (before.SilkCost != after.SilkCost)
            UIEvents.UpdateSilk();
    }

    protected override void CustomizeMenu(BindSettingsMenu menu)
    {
        menu.HealMasks.Model.FormatIntDelta(3);
        menu.MultibinderHealMasks.Model.FormatIntDelta(2);
        menu.SilkCost.Model.FormatIntDelta(9);
        menu.TimePenalty.Model.FormatPercent();
    }

    private static void EditBindFsm(PlayMakerFSM fsm)
    {
        fsm.GetState("Can Bind?")!
            .InsertAction(
                0,
                IfEnabled(s => fsm.FsmVariables.GetFsmInt("Silk Cost").Value = s.SilkCost)
            );
        fsm.GetState("Set Normal")!
            .InsertAction(
                3,
                IfEnabled(s => fsm.FsmVariables.GetFsmInt("Heal Amount").Value = s.HealMasks)
            );
        fsm.GetState("Multi Bind")!
            .AddAction(
                IfEnabled(s =>
                    fsm.FsmVariables.GetFsmInt("Heal Amount").Value = s.MultibinderHealMasks
                )
            );
        fsm.GetState("Bind Shared")!
            .InsertAction(
                0,
                IfEnabled(s => fsm.FsmVariables.GetFsmFloat("Bind Time").Value *= s.TimePenalty)
            );
    }

    private static void OverrideBindCost(ref float result)
    {
        if (PlayerData.instance.IsAnyCursed)
            return;
        if (GetEnabledConfig(out var config))
            result = config.SilkCost;
    }

    static BindModule() => Events.AddFsmEdit("Hero_Hornet(Clone)", "Bind", EditBindFsm);

    [MonoDetourHookInitialize]
    private static void Hook() => Md.SilkSpool.get_BindCost.Postfix(OverrideBindCost);
}
