using HutongGames.PlayMaker.Actions;
using MonoDetour;
using MonoDetour.DetourTypes;
using MonoDetour.HookGen;
using PrepatcherPlugin;
using Silksong.ModMenu.Elements;
using Silksong.ModMenu.Models;
using Silksong.TheHuntIsOn.Menu;
using Silksong.TheHuntIsOn.Modules.Lib;
using Silksong.TheHuntIsOn.SsmpAddon.PacketUtil;
using SSMP.Networking.Packet;
using System;
using System.Collections.Generic;

namespace Silksong.TheHuntIsOn.Modules;

internal class CurrencySettings : ModuleSettings<CurrencySettings>
{
    public float RosaryMultiplier = 1f;
    public float ShellShardMultiplier = 1f;

    public override ModuleSettingsType DynamicType => ModuleSettingsType.Currency;

    public override void ReadDynamicData(IPacket packet)
    {
        RosaryMultiplier.ReadData(packet);
        ShellShardMultiplier.ReadData(packet);
    }

    public override void WriteDynamicData(IPacket packet)
    {
        RosaryMultiplier.WriteData(packet);
        ShellShardMultiplier.WriteData(packet);
    }

    protected override bool Equivalent(CurrencySettings other) =>
        RosaryMultiplier == other.RosaryMultiplier
        && ShellShardMultiplier == other.ShellShardMultiplier;
}

[MonoDetourTargets(typeof(PlayerData), GenerateControlFlowVariants = true)]
internal class CurrencyModule : GlobalSettingsModule<CurrencyModule, CurrencySettings, CurrencySubMenu>
{
    protected override CurrencyModule Self() => this;

    public override string Name => "Currency";

    public override ModuleActivationType ModuleActivationType =>
        ModuleActivationType.AnyConfiguration;

    private static int ApplyMultiplier(int amount, float multiplier)
    {
        if (multiplier == 1f)
            return amount;
        return Math.Max(1, (int)Math.Round(amount * multiplier));
    }

    private static ReturnFlow OverrideAddGeo(PlayerData self, ref int amount)
    {
        if (!GetEnabledConfig(out var config) || config.RosaryMultiplier == 1.0f)
            return ReturnFlow.None;

        amount = ApplyMultiplier(amount, config.RosaryMultiplier);
        PlayerDataAccess.geo += amount;

        int currencyCap = GlobalSettings.Gameplay.GetCurrencyCap(CurrencyType.Money);
        if (PlayerDataAccess.geo > currencyCap)
            PlayerDataAccess.geo = currencyCap;

        return ReturnFlow.SkipOriginal;
    }

    private static ReturnFlow OverrideAddShards(PlayerData self, ref int amount)
    {
        if (!GetEnabledConfig(out var config) || config.ShellShardMultiplier == 1.0f)
            return ReturnFlow.None;

        amount = ApplyMultiplier(amount, config.ShellShardMultiplier);
        PlayerDataAccess.ShellShards += amount;

        int currencyCap = GlobalSettings.Gameplay.GetCurrencyCap(CurrencyType.Shard);
        if (PlayerDataAccess.ShellShards > currencyCap)
            PlayerDataAccess.ShellShards = currencyCap;

        return ReturnFlow.SkipOriginal;
    }

    [MonoDetourHookInitialize]
    private static void Hook()
    {
        Md.PlayerData.AddGeo.ControlFlowPrefix(OverrideAddGeo);
        Md.PlayerData.AddShards.ControlFlowPrefix(OverrideAddShards);
    }
}

internal class CurrencySubMenu : ModuleSubMenu<CurrencySettings>
{
    private static ListChoiceModel<float> MultiplierModel() =>
        new([1.0f, 2.0f, 3.0f, 4.0f, 5.0f])
        {
            DisplayFn = (idx, value) => $"{value:0.0}x",
        };

    private readonly ChoiceElement<float> RosaryMultiplier = new(
        "Rosary Multiplier",
        MultiplierModel(),
        "Multiplier on rosary accumulation from all sources."
    );
    private readonly ChoiceElement<float> ShellShardMultiplier = new(
        "Shell Shard Multiplier",
        MultiplierModel(),
        "Multiplier on shell shard accumulation from all sources."
    );

    public override IEnumerable<MenuElement> Elements() =>
        [RosaryMultiplier, ShellShardMultiplier];

    internal override void Apply(CurrencySettings data)
    {
        RosaryMultiplier.Value = data.RosaryMultiplier;
        ShellShardMultiplier.Value = data.ShellShardMultiplier;
    }

    internal override CurrencySettings Export() =>
        new()
        {
            RosaryMultiplier = RosaryMultiplier.Value,
            ShellShardMultiplier = ShellShardMultiplier.Value,
        };
}
