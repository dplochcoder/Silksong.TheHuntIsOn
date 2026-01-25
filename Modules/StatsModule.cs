using Silksong.ModMenu.Elements;
using Silksong.TheHuntIsOn.Menu;
using Silksong.TheHuntIsOn.Modules.Lib;
using Silksong.TheHuntIsOn.SsmpAddon.PacketUtil;
using Silksong.TheHuntIsOn.Util;
using SSMP.Networking.Packet;
using System.Collections.Generic;

namespace Silksong.TheHuntIsOn.Modules;

internal class StatsSettings : NetworkedCloneable<StatsSettings>
{
    public int StartingMasks = 5;
    public int StartingSilkSpools = 9;

    public override void ReadData(IPacket packet)
    {
        StartingMasks.ReadData(packet);
        StartingSilkSpools.ReadData(packet);
    }

    public override void WriteData(IPacket packet)
    {
        StartingMasks.WriteData(packet);
        StartingSilkSpools.WriteData(packet);
    }
}

internal class StatsModule : GlobalSettingsModule<StatsModule, StatsSettings, StatsSubMenu>
{
    protected override StatsModule Self() => this;

    public override string Name => "Stats";

    public override ModuleActivationType ModuleActivationType => ModuleActivationType.AnyConfiguration;

    public override void OnEnabled()
    {
        PrepatcherPlugin.PlayerDataVariableEvents<int>.OnGetVariable += ModifyCoreStats;
        PrepatcherPlugin.PlayerDataVariableEvents<int>.OnSetVariable += ModifyCoreStats;
    }

    public override void OnDisabled()
    {
        PrepatcherPlugin.PlayerDataVariableEvents<int>.OnGetVariable -= ModifyCoreStats;
        PrepatcherPlugin.PlayerDataVariableEvents<int>.OnSetVariable -= ModifyCoreStats;
    }

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
}

internal class StatsSubMenu : ModuleSubMenu<StatsSettings>
{
    private readonly ChoiceElement<int> StartingMasks = new("Starting Masks", CollectionUtil.IntRangeModel(1, 10), "Number of masks players start with.");
    private readonly ChoiceElement<int> StartingSilkSpools = new("Starting Silk Spools", CollectionUtil.IntRangeModel(1, 18), "Number of silk spools players start with.");

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
