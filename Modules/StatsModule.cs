using System;
using System.Collections.Generic;
using System.ComponentModel;
using Silksong.ModMenu.Generator;
using Silksong.TheHuntIsOn.Modules.Lib;
using Silksong.TheHuntIsOn.SsmpAddon.PacketUtil;
using Silksong.TheHuntIsOn.Util;
using SSMP.Networking.Packet;

namespace Silksong.TheHuntIsOn.Modules;

[GenerateMenu]
public class StatsSettings : ModuleSettings<StatsSettings>
{
    public override ModuleSettingsType DynamicType() => ModuleSettingsType.Stats;

    [Description("Number of masks players start with.")]
    [ModMenuOptions(1, 2, 3, 4, 5, 6, 7, 8, 9, 10)]
    public int StartingMasks = 5;

    [Description("Number of silk spools players start with.")]
    [ModMenuOptions(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18)]
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

    protected override bool Equivalent(StatsSettings other) =>
        StartingMasks == other.StartingMasks && StartingSilkSpools == other.StartingSilkSpools;
}

internal class StatsModule : GlobalSettingsModule<StatsModule, StatsSettings, StatsSettingsMenu>
{
    protected override StatsModule Self() => this;

    public override string Name => "Stats";

    public override ModuleActivationType ModuleActivationType =>
        ModuleActivationType.AnyConfiguration;

    private static Func<int> FromSettings(Func<StatsSettings, int> func) =>
        () => GetEnabledConfig(out var settings) ? func(settings) : 0;

    private static readonly Dictionary<string, Func<int>> intModifiers = new()
    {
        [nameof(PlayerData.maxHealth)] = FromSettings(s => s.StartingMasks - 5),
        [nameof(PlayerData.maxHealthBase)] = FromSettings(s => s.StartingMasks - 5),
        [nameof(PlayerData.silkMax)] = FromSettings(s => s.StartingSilkSpools - 9),
    };

    public override void OnEnabled()
    {
        foreach (var e in intModifiers)
            Events.AddPdIntModifier(e.Key, e.Value);
        UIEvents.UpdateHealth();
        UIEvents.UpdateSilk();
    }

    public override void OnDisabled()
    {
        foreach (var e in intModifiers)
            Events.RemovePdIntModifier(e.Key, e.Value);
        UIEvents.UpdateHealth();
        UIEvents.UpdateSilk();
    }

    protected override void OnGlobalConfigChanged(StatsSettings before, StatsSettings after)
    {
        if (before.StartingMasks != after.StartingMasks)
            UIEvents.UpdateHealth();
        if (before.StartingSilkSpools != after.StartingSilkSpools)
            UIEvents.UpdateSilk();
    }

    protected override void CustomizeMenu(StatsSettingsMenu menu)
    {
        menu.StartingMasks.Model.FormatIntDelta(5);
        menu.StartingSilkSpools.Model.FormatIntDelta(9);
    }
}
