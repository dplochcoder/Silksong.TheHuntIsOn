using HutongGames.PlayMaker;
using Silksong.FsmUtil;
using Silksong.ModMenu.Elements;
using Silksong.ModMenu.Models;
using Silksong.TheHuntIsOn.Menu;
using Silksong.TheHuntIsOn.Modules.Lib;
using Silksong.TheHuntIsOn.SsmpAddon.PacketUtil;
using Silksong.TheHuntIsOn.Util;
using SSMP.Networking.Packet;
using System.Collections.Generic;

namespace Silksong.TheHuntIsOn.Modules;

internal class HealthSettings : NetworkedCloneable<HealthSettings>
{
    public int StartingMasks = 5;
    public int StartingSilkSpools = 9;
    public int BindHealMasks = 3;
    public int MultibinderHealMasks = 2;
    public int BindSilkCost = 9;
    public float BindTimePenalty = 1f;

    public override void ReadData(IPacket packet)
    {
        StartingMasks.ReadData(packet);
        StartingSilkSpools.ReadData(packet);
        BindHealMasks.ReadData(packet);
        MultibinderHealMasks.ReadData(packet);
        BindSilkCost.ReadData(packet);
        BindTimePenalty.ReadData(packet);
    }

    public override void WriteData(IPacket packet)
    {
        StartingMasks.WriteData(packet);
        StartingSilkSpools.WriteData(packet);
        BindHealMasks.WriteData(packet);
        MultibinderHealMasks.WriteData(packet);
        BindSilkCost.WriteData(packet);
        BindTimePenalty.WriteData(packet);
    }
}

internal class HealthModule : GlobalSettingsModule<HealthModule, HealthSettings, BindSubMenu>
{
    protected override HealthModule Self() => this;

    public override string Name => "Bind";

    public override ModuleActivationType ModuleActivationType => ModuleActivationType.AnyConfiguration;

    public override void OnEnabled() => PrepatcherPlugin.PlayerDataVariableEvents<int>.OnGetVariable += ModifyCoreStats;

    public override void OnDisabled() => PrepatcherPlugin.PlayerDataVariableEvents<int>.OnGetVariable += ModifyCoreStats;

    private int ModifyCoreStats(PlayerData playerData, string name, int orig)
    {
        if (Instance == null) return orig;

        var config = Instance.GlobalConfig;
        return name switch
        {
            nameof(PlayerData.maxHealth) => orig + (config.StartingMasks - 5),
            nameof(PlayerData.silkMax) => orig + (config.StartingSilkSpools - 9),
            _ => orig
        };
    }

    private static void SetSilkCost(FsmInt fsmInt)
    {
        if (Instance != null) fsmInt.Value = Instance.GlobalConfig.BindSilkCost;
    }

    private static void EditBindFsm(PlayMakerFSM fsm)
    {
        fsm.GetState("Can Bind?")!.InsertAction(0, IfEnabled(s => fsm.FsmVariables.GetFsmInt("Silk Cost").Value = s.BindSilkCost));
        fsm.GetState("Set Normal")!.InsertAction(3, IfEnabled(s => fsm.FsmVariables.GetFsmInt("Heal Amount").Value = s.BindHealMasks));
        fsm.GetState("Multi Bind")!.AddAction(IfEnabled(s => fsm.FsmVariables.GetFsmInt("Heal Amount").Value = s.MultibinderHealMasks));
        fsm.GetState("Bind Shared")!.InsertAction(0, IfEnabled(s => fsm.FsmVariables.GetFsmFloat("Bind Time").Value *= s.BindTimePenalty));
    }

    // FIXME: Fix UI
    static HealthModule() => Events.AddFsmEdit("Hero_Hornet", "Bind", EditBindFsm);
}

internal class BindSubMenu : ModuleSubMenu<HealthSettings>
{
    private readonly ChoiceElement<int> StartingMasks = new("Starting Masks", CollectionUtil.IntRangeModel(1, 10), "Number of masks players start with.");
    private readonly ChoiceElement<int> StartingSilkSpools = new("Starting Silk Spools", CollectionUtil.IntRangeModel(1, 18), "Number of silk spools players start with.");
    private readonly ChoiceElement<int> BindHealMasks = new("Bind Heal Masks", CollectionUtil.IntRangeModel(0, 10), "Number of masks to heal when binding.");
    private readonly ChoiceElement<int> MultibinderHealMasks = new("Multibinder Heal Masks", CollectionUtil.IntRangeModel(0, 5), "Number of masks to heal when multi-binding.");
    private readonly ChoiceElement<int> BindSilkCost = new("Bind Silk Cost", CollectionUtil.IntRangeModel(1, 18), "Number of silk spools required to bind.");
    private readonly ChoiceElement<float> BindTimePenalty = new("Bind Time Penalty", ChoiceModels.ForValues([0.25f, 0.5f, 0.75f, 1f, 1.5f, 2f, 3f]), "Multiplier on the time it takes to bind.");

    public override IEnumerable<MenuElement> Elements() => [StartingMasks, StartingSilkSpools, BindHealMasks, MultibinderHealMasks, BindSilkCost, BindTimePenalty];

    internal override void Apply(HealthSettings data)
    {
        StartingMasks.Value = data.StartingMasks;
        StartingSilkSpools.Value = data.StartingSilkSpools;
        BindHealMasks.Value = data.BindHealMasks;
        MultibinderHealMasks.Value = data.MultibinderHealMasks;
        BindSilkCost.Value = data.BindSilkCost;
        BindTimePenalty.Value = data.BindTimePenalty;
    }

    internal override HealthSettings Export() => new()
    {
        StartingMasks = StartingMasks.Value,
        StartingSilkSpools = StartingSilkSpools.Value,
        BindHealMasks = BindHealMasks.Value,
        MultibinderHealMasks = MultibinderHealMasks.Value,
        BindSilkCost = BindSilkCost.Value,
        BindTimePenalty = BindTimePenalty.Value,
    };
}
