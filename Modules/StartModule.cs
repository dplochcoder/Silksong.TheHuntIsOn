using System;
using System.Collections.Generic;
using Silksong.ModMenu.Elements;
using Silksong.TheHuntIsOn.Menu;
using Silksong.TheHuntIsOn.Modules.Lib;
using Silksong.TheHuntIsOn.SsmpAddon.PacketUtil;
using Silksong.TheHuntIsOn.Util;
using SSMP.Networking.Packet;

namespace Silksong.TheHuntIsOn.Modules;

internal class StartSettings : ModuleSettings<StartSettings>
{
    public override ModuleSettingsType DynamicType => ModuleSettingsType.Start;

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

    protected override bool Equivalent(StartSettings other) =>
        StartingMasks == other.StartingMasks && StartingSilkSpools == other.StartingSilkSpools;
}

internal class StartModule : GlobalSettingsModule<StartModule, StartSettings, StartSubMenu>
{
    protected override StartModule Self() => this;

    public override string Name => "Start";

    public override ModuleActivationType ModuleActivationType =>
        ModuleActivationType.AnyConfiguration;

    private static Func<int> FromSettings(Func<StartSettings, int> func) =>
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

    protected override void OnGlobalConfigChanged(StartSettings before, StartSettings after)
    {
        if (before.StartingMasks != after.StartingMasks)
            UIEvents.UpdateHealth();
        if (before.StartingSilkSpools != after.StartingSilkSpools)
            UIEvents.UpdateSilk();
    }
}

internal class StartSubMenu : ModuleSubMenu<StartSettings>
{
    private readonly ChoiceElement<int> StartingMasks = new(
        "Starting Masks",
        ChoiceModelUtil.IntRangeModel(1, 10).FormatIntDelta(5),
        "Number of masks players start with."
    );
    private readonly ChoiceElement<int> StartingSilkSpools = new(
        "Starting Silk Spools",
        ChoiceModelUtil.IntRangeModel(1, 18).FormatIntDelta(9),
        "Number of silk spools players start with."
    );

    public override IEnumerable<MenuElement> Elements() => [StartingMasks, StartingSilkSpools];

    internal override void Apply(StartSettings data)
    {
        StartingMasks.Value = data.StartingMasks;
        StartingSilkSpools.Value = data.StartingSilkSpools;
    }

    internal override StartSettings Export() =>
        new()
        {
            StartingMasks = StartingMasks.Value,
            StartingSilkSpools = StartingSilkSpools.Value,
        };
}
