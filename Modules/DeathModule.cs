using MonoDetour;
using MonoDetour.HookGen;
using PrepatcherPlugin;
using Silksong.ModMenu.Elements;
using Silksong.ModMenu.Models;
using Silksong.TheHuntIsOn.Menu;
using Silksong.TheHuntIsOn.Modules.Lib;
using Silksong.TheHuntIsOn.Modules.PauseTimerModule;
using Silksong.TheHuntIsOn.SsmpAddon.PacketUtil;
using SSMP.Networking.Packet;
using System.Collections;
using System.Collections.Generic;

namespace Silksong.TheHuntIsOn.Modules;

internal class DeathSettings : ModuleSettings<DeathSettings>
{
    public override ModuleSettingsType DynamicType => ModuleSettingsType.Death;

    public int RespawnTimer = 0;
    public bool SpawnCoccoon = true;
    public bool LoseRosaries = true;
    public bool LimitSilk = true;

    public override void ReadDynamicData(IPacket packet)
    {
        RespawnTimer.ReadData(packet);
        SpawnCoccoon.ReadData(packet);
        LoseRosaries.ReadData(packet);
        LimitSilk.ReadData(packet);
    }

    public override void WriteDynamicData(IPacket packet)
    {
        RespawnTimer.WriteData(packet);
        SpawnCoccoon.WriteData(packet);
        LoseRosaries.WriteData(packet);
        LimitSilk.WriteData(packet);
    }

    protected override bool Equivalent(DeathSettings other) => RespawnTimer == other.RespawnTimer
        && SpawnCoccoon == other.SpawnCoccoon
        && LoseRosaries == other.LoseRosaries
        && LimitSilk == other.LimitSilk;
}

[MonoDetourTargets(typeof(HeroController), GenerateControlFlowVariants = true)]
internal class DeathModule : GlobalSettingsModule<DeathModule, DeathSettings, DeathSubMenu>
{
    internal static int GetRespawnTimer() => GetEnabledConfig(out var config) ? config.RespawnTimer : 0;

    protected override DeathModule Self() => this;

    public override string Name => "Death";

    public override ModuleActivationType ModuleActivationType => ModuleActivationType.AnyConfiguration;

    protected override void OnGlobalConfigChanged(DeathSettings before, DeathSettings after)
    {
        if (before.RespawnTimer > after.RespawnTimer) PauseTimerUI.ShortenRespawn(before.RespawnTimer - after.RespawnTimer);
    }

    private static void ExtendDeath(HeroController self, ref bool nonLethal, ref bool frostDeath, ref IEnumerator coroutine)
    {
        if (nonLethal || !GetEnabledConfig(out var s)) return;

        IEnumerator orig = coroutine;
        IEnumerator Append()
        {
            while (orig.MoveNext()) yield return orig.Current;

            if (!s.SpawnCoccoon || !s.LoseRosaries)
            {
                // Save the rosaries.
                PlayerDataAccess.geo += PlayerDataAccess.HeroCorpseMoneyPool;
                PlayerDataAccess.HeroCorpseMoneyPool = 0;
            }
            if (!s.SpawnCoccoon || !s.LimitSilk)
            {
                PlayerDataAccess.IsSilkSpoolBroken = false;
                GameCameras.instance.silkSpool.RefreshSilk();
            }
            if (!s.SpawnCoccoon)
            {
                PlayerDataAccess.HeroCorpseScene = "";
                PlayerDataAccess.HeroCorpseMarkerGuid = [];
                GameManager.instance.gameMap.corpseSceneMapZone = GlobalEnums.MapZone.NONE;
            }
        }
        coroutine = Append();
    }

    [MonoDetourHookInitialize]
    private static void Hook() => Md.HeroController.Die.Postfix(ExtendDeath);
}

internal class DeathSubMenu : ModuleSubMenu<DeathSettings>
{
    private readonly ChoiceElement<int> RespawnTimer = new("Respawn Timer", ChoiceModels.ForValues([0, 10, 20, 30, 45, 60, 90, 120, 180, 300]), "Seconds to wait to respawn after death.");
    private readonly ChoiceElement<bool> SpawnCoccoon = new("Spawn Coccoon", ChoiceModels.ForBool(), "If false, don't spawn coccoons at all.");
    private readonly ChoiceElement<bool> LoseRosaries = new("Lose Rosaries", ChoiceModels.ForBool(), "If false, don't lose rosaries on death.");
    private readonly ChoiceElement<bool> LimitSilk = new("Limit Silk", ChoiceModels.ForBool(), "If false, don't restrict silk on death.");

    public DeathSubMenu() => SpawnCoccoon.OnValueChanged += value =>
    {
        LoseRosaries.Interactable = value;
        LimitSilk.Interactable = value;
    };

    public override IEnumerable<MenuElement> Elements() => [RespawnTimer, SpawnCoccoon, LoseRosaries, LimitSilk];

    internal override void Apply(DeathSettings data)
    {
        RespawnTimer.Value = data.RespawnTimer;
        SpawnCoccoon.Value = data.SpawnCoccoon;
        LoseRosaries.Value = data.LoseRosaries;
        LimitSilk.Value = data.LimitSilk;
    }

    internal override DeathSettings Export() => new()
    {
        RespawnTimer = RespawnTimer.Value,
        SpawnCoccoon = SpawnCoccoon.Value,
        LoseRosaries = LoseRosaries.Value,
        LimitSilk = LimitSilk.Value,
    };
}
