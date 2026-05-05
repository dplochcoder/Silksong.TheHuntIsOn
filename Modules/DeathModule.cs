using System.Collections;
using System.ComponentModel;
using MonoDetour;
using MonoDetour.HookGen;
using PrepatcherPlugin;
using Silksong.ModMenu.Generator;
using Silksong.TheHuntIsOn.Modules.Lib;
using Silksong.TheHuntIsOn.Modules.PauseTimerModule;
using Silksong.TheHuntIsOn.SsmpAddon.PacketUtil;
using SSMP.Networking.Packet;

namespace Silksong.TheHuntIsOn.Modules;

[GenerateMenu]
public class DeathSettings : ModuleSettings<DeathSettings>
{
    public override ModuleSettingsType DynamicType() => ModuleSettingsType.Death;

    [Description("Seconds to wait to respawn after death.")]
    [ModMenuOptions(0, 10, 20, 30, 45, 60, 90, 120, 180, 300)]
    public int RespawnTimer = 0;

    [Description("If false, don't spawn coccoons at all.")]
    public bool SpawnCoccoon = true;

    [Description("If false, don't lose rosaries on death.")]
    public bool LoseRosaries = true;

    [Description("If false, don't restrict silk on death.")]
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

    protected override bool Equivalent(DeathSettings other) =>
        RespawnTimer == other.RespawnTimer
        && SpawnCoccoon == other.SpawnCoccoon
        && LoseRosaries == other.LoseRosaries
        && LimitSilk == other.LimitSilk;
}

[MonoDetourTargets(typeof(HeroController), GenerateControlFlowVariants = true)]
internal class DeathModule : GlobalSettingsModule<DeathModule, DeathSettings, DeathSettingsMenu>
{
    internal static int GetRespawnTimer() =>
        GetEnabledConfig(out var config) ? config.RespawnTimer : 0;

    protected override DeathModule Self() => this;

    public override string Name => "Death";

    public override ModuleActivationType ModuleActivationType =>
        ModuleActivationType.AnyConfiguration;

    protected override void OnGlobalConfigChanged(DeathSettings before, DeathSettings after)
    {
        if (before.RespawnTimer > after.RespawnTimer)
            PauseTimerUI.ShortenRespawn(before.RespawnTimer - after.RespawnTimer);
    }

    protected override void CustomizeMenu(DeathSettingsMenu menu)
    {
        void UpdateInteractable(bool value)
        {
            menu.LoseRosaries.Interactable = value;
            menu.LimitSilk.Interactable = value;
        }

        menu.SpawnCoccoon.OnValueChanged += UpdateInteractable;
        UpdateInteractable(menu.SpawnCoccoon.Value);
    }

    private static void ExtendDeath(
        HeroController self,
        ref bool nonLethal,
        ref bool frostDeath,
        ref IEnumerator coroutine
    )
    {
        if (nonLethal || !GetEnabledConfig(out var s))
            return;

        IEnumerator orig = coroutine;
        IEnumerator Append()
        {
            while (orig.MoveNext())
                yield return orig.Current;

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
