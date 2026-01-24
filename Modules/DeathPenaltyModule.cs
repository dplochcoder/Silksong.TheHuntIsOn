using MonoDetour;
using MonoDetour.HookGen;
using Silksong.ModMenu.Elements;
using Silksong.ModMenu.Models;
using Silksong.TheHuntIsOn.Menu;
using Silksong.TheHuntIsOn.Modules.Lib;
using Silksong.TheHuntIsOn.SsmpAddon.PacketUtil;
using Silksong.TheHuntIsOn.Util;
using SSMP.Networking.Packet;
using System.Collections;
using System.Collections.Generic;

namespace Silksong.TheHuntIsOn.Modules;

internal class DeathPenaltySettings : NetworkedCloneable<DeathPenaltySettings>
{
    public int RespawnTimer = 0;
    public bool SpawnCoccoon = true;
    public bool LoseRosaries = true;
    public bool LimitSilk = true;

    public override void ReadData(IPacket packet)
    {
        RespawnTimer.ReadData(packet);
        SpawnCoccoon.ReadData(packet);
        LoseRosaries.ReadData(packet);
        LimitSilk.ReadData(packet);
    }

    public override void WriteData(IPacket packet)
    {
        RespawnTimer.WriteData(packet);
        SpawnCoccoon.WriteData(packet);
        LoseRosaries.WriteData(packet);
        LimitSilk.WriteData(packet);
    }
}

[MonoDetourTargets(typeof(HeroController))]
internal class DeathPenaltyModule : GlobalSettingsModule<DeathPenaltyModule, DeathPenaltySettings, DeathPenaltySubMenu>
{
    protected override DeathPenaltyModule Self() => this;

    public override string Name => "DeathPenalty";

    public override ModuleActivationType ModuleActivationType => ModuleActivationType.AnyConfiguration;

    // FIXME: Respawn timer.
    private static void ExtendDeath(HeroController self, ref bool nonLethal, ref bool frostDeath, ref IEnumerator coroutine)
    {
        if (nonLethal || !GetEnabledConfig(out var s)) return;

        IEnumerator orig = coroutine;
        IEnumerator Append()
        {
            while (orig.MoveNext()) yield return orig.Current;

            var pd = PlayerData.instance;
            if (!s.SpawnCoccoon || !s.LoseRosaries)
            {
                // Save the rosaries.
                var pool = pd.GetInt(nameof(pd.HeroCorpseMoneyPool));
                var saved = pd.GetInt(nameof(pd.geo));
                pd.SetInt(nameof(pd.geo), pool + saved);
                pd.SetInt(nameof(pd.HeroCorpseMoneyPool), 0);
            }
            if (!s.SpawnCoccoon || !s.LimitSilk)
            {
                pd.SetBool(nameof(pd.IsSilkSpoolBroken), false);
                GameCameras.instance.silkSpool.RefreshSilk();
            }
            if (!s.SpawnCoccoon)
            {
                pd.SetString(nameof(pd.HeroCorpseScene), "");
                pd.SetString(nameof(pd.HeroCorpseMarkerGuid), "");
                GameManager.instance.gameMap.corpseSceneMapZone = GlobalEnums.MapZone.NONE;
            }
        }
        coroutine = Append();
    }

    [MonoDetourHookInitialize]
    private static void Hook() => Md.HeroController.Die.Postfix(ExtendDeath);
}

internal class DeathPenaltySubMenu : ModuleSubMenu<DeathPenaltySettings>
{
    private readonly ChoiceElement<int> RespawnTimer = new("Respawn Timer", ChoiceModels.ForValues([0, 10, 20, 30, 45, 60, 90, 120, 180, 300]), "Seconds to wait to respawn after death.");
    private readonly ChoiceElement<bool> SpawnCoccoon = new("Spawn Coccoon", ChoiceModels.ForBool(), "If false, don't spawn coccoons at all.");
    private readonly ChoiceElement<bool> LoseRosaries = new("Lose Rosaries", ChoiceModels.ForBool(), "If false, don't lose rosaries on death.");
    private readonly ChoiceElement<bool> LimitSilk = new("Limit Silk", ChoiceModels.ForBool(), "If false, don't restrict silk on death.");

    public DeathPenaltySubMenu() => SpawnCoccoon.OnValueChanged += value =>
    {
        LoseRosaries.Interactable = value;
        LimitSilk.Interactable = value;
    };

    public override IEnumerable<MenuElement> Elements() => [SpawnCoccoon, LoseRosaries, LimitSilk];

    internal override void Apply(DeathPenaltySettings data)
    {
        RespawnTimer.Value = data.RespawnTimer;
        SpawnCoccoon.Value = data.SpawnCoccoon;
        LoseRosaries.Value = data.LoseRosaries;
        LimitSilk.Value = data.LimitSilk;
    }

    internal override DeathPenaltySettings Export() => new()
    {
        RespawnTimer = RespawnTimer.Value,
        SpawnCoccoon = SpawnCoccoon.Value,
        LoseRosaries = LoseRosaries.Value,
        LimitSilk = LimitSilk.Value,
    };
}
