using Silksong.ModMenu.Elements;
using Silksong.TheHuntIsOn.Menu;
using Silksong.TheHuntIsOn.Modules.Lib;
using Silksong.TheHuntIsOn.SsmpAddon.PacketUtil;
using Silksong.TheHuntIsOn.Util;
using SSMP.Networking.Packet;
using System;
using System.Collections.Generic;

namespace Silksong.TheHuntIsOn.Modules;

internal class StatsSettings : ModuleSettings<StatsSettings>
{
    public override ModuleSettingsType DynamicType => ModuleSettingsType.Stats;

    public int StartingMasks = 5;
    public int StartingSilkSpools = 9;

    public override void ReadDynamicData(IPacket packet)
    {
        StartingMasks.ReadData(packet);
        StartingSilkSpools.ReadData(packet);
    }

    public override void WriteDynamicData(IPacket packet)
    {
        StartingMasks.WriteData(packet);
        StartingSilkSpools.WriteData(packet);
    }

    protected override bool Equivalent(StatsSettings other) => StartingMasks == other.StartingMasks && StartingSilkSpools == other.StartingSilkSpools;
}

internal class StatsModule : GlobalSettingsModule<StatsModule, StatsSettings, StatsSubMenu>
{
    protected override StatsModule Self() => this;

    public override string Name => "Stats";

    public override ModuleActivationType ModuleActivationType => ModuleActivationType.AnyConfiguration;

    private static Func<int> FromSettings(Func<StatsSettings, int> func) => () => GetEnabledConfig(out var settings) ? func(settings) : 0;

    private static readonly Dictionary<string, Func<int>> intModifiers = new()
    {
        [nameof(PlayerData.maxHealth)] = FromSettings(s => s.StartingMasks - 5),
        [nameof(PlayerData.maxHealthBase)] = FromSettings(s => s.StartingMasks - 5),
        [nameof(PlayerData.silkMax)] = FromSettings(s => s.StartingSilkSpools - 9)
    };

    public override void OnEnabled()
    {
        foreach (var e in intModifiers) Events.AddPdIntModifier(e.Key, e.Value);
        UIEvents.UpdateHealth();
        UIEvents.UpdateSilk();
    }

    public override void OnDisabled()
    {
        foreach (var e in intModifiers) Events.RemovePdIntModifier(e.Key, e.Value);
        UIEvents.UpdateHealth();
        UIEvents.UpdateSilk();
    }

    protected override void OnGlobalConfigChanged(StatsSettings before, StatsSettings after)
    {
        if (before.StartingMasks != after.StartingMasks) UIEvents.UpdateHealth();
        if (before.StartingSilkSpools != after.StartingSilkSpools) UIEvents.UpdateSilk();
    }
}

internal class StatsSubMenu : ModuleSubMenu<StatsSettings>
{
    private readonly ChoiceElement<int> StartingMasks = new("Starting Masks", ChoiceModelUtil.IntRangeModel(1, 10).FormatIntDelta(5), "Number of masks players start with.");
    private readonly ChoiceElement<int> StartingSilkSpools = new("Starting Silk Spools", ChoiceModelUtil.IntRangeModel(1, 18).FormatIntDelta(9), "Number of silk spools players start with.");

    public override IEnumerable<MenuElement> Elements() => [StartingMasks, StartingSilkSpools];

    internal override void Apply(StatsSettings data)
    {
        StartingMasks.Value = data.StartingMasks;
        StartingSilkSpools.Value = data.StartingSilkSpools;
    }

    internal override StatsSettings Export() => new()
    {
        StartingMasks = StartingMasks.Value,
        StartingSilkSpools = StartingSilkSpools.Value,
    };
}
