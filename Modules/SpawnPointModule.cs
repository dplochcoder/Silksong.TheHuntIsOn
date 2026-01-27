using MonoDetour;
using MonoDetour.DetourTypes;
using MonoDetour.HookGen;
using Silksong.ModMenu.Elements;
using Silksong.ModMenu.Models;
using Silksong.TheHuntIsOn.Menu;
using Silksong.TheHuntIsOn.Modules.Lib;
using Silksong.TheHuntIsOn.SsmpAddon.PacketUtil;
using Silksong.TheHuntIsOn.Util;
using SSMP.Networking.Packet;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Silksong.TheHuntIsOn.Modules;

internal enum SpawnPoint
{
    Unchanged,
    BoneBottom,
    Bellhart,
    MossGrotto,
    Songclave,
}

internal class SpawnPointSettings : ModuleSettings<SpawnPointSettings>
{
    public override ModuleSettingsType DynamicType => ModuleSettingsType.SpawnPoint;

    public SpawnPoint SpawnPoint = SpawnPoint.Unchanged;

    public override void ReadDynamicData(IPacket packet) => SpawnPoint = packet.ReadEnum<SpawnPoint>();

    public override void WriteDynamicData(IPacket packet) => SpawnPoint.WriteData(packet);
}

[MonoDetourTargets(typeof(GameManager), GenerateControlFlowVariants = true)]
internal class SpawnPointModule : GlobalSettingsModule<SpawnPointModule, SpawnPointSettings, SpawnPointSubMenu>
{
    protected override SpawnPointModule Self() => this;

    public override string Name => "Spawn Point";

    public override ModuleActivationType ModuleActivationType => ModuleActivationType.AnyConfiguration;

    public override void OnEnabled()
    {
        PrepatcherPlugin.PlayerDataVariableEvents<int>.OnGetVariable += OverrideRespawnInt;
        Events.OnNewScene += OnNewScene;
    }

    public override void OnDisabled()
    {
        PrepatcherPlugin.PlayerDataVariableEvents<int>.OnGetVariable -= OverrideRespawnInt;
        Events.OnNewScene -= OnNewScene;
    }

    private class RespawnData
    {
        public string Scene = "";
        public Vector2 Position;
        public bool FacingRight;
    }

    private static readonly Dictionary<SpawnPoint, RespawnData> respawnData = new ()
    {
        [SpawnPoint.Bellhart] = new()
        {
            Scene = "Belltown",
            Position = new(46, 23),
            FacingRight = true,
        },
        [SpawnPoint.BoneBottom] = new()
        {
            Scene = "Bonetown",
            Position = new(218, 8),
            FacingRight = true,
        },
        [SpawnPoint.MossGrotto] = new()
        {
            Scene = "Tut_01",
            Position = new(54.5f, 5f),
            FacingRight = false,
        },
        [SpawnPoint.Songclave] = new()
        {
            Scene = "Song_Enclave",
            Position = new(91f, 8f),
            FacingRight = false,
        }
    };

    private static bool GetRespawnData(out RespawnData data)
    {
        if (GetEnabledConfig(out var config)) return respawnData.TryGetValue(config.SpawnPoint, out data);
        else
        {
            data = new();
            return false;
        }
    }

    private const string RESPAWN_MARKER_NAME = "TheHuntIsOn-RespawnMarker";

    private static ReturnFlow OverrideGetRespawnInfo(GameManager self, ref string scene, ref string marker)
    {
        if (!GetRespawnData(out var data)) return ReturnFlow.None;

        scene = data.Scene;
        marker = RESPAWN_MARKER_NAME;
        return ReturnFlow.SkipOriginal;
    }

    private int OverrideRespawnInt(PlayerData playerData, string name, int current) => name switch
    {
        nameof(PlayerData.respawnType) => GlobalConfig.SpawnPoint == SpawnPoint.Unchanged ? current : 0,
        _ => current
    };

    private void OnNewScene(Scene scene)
    {
        if (!GetRespawnData(out var data) || scene.name != data.Scene) return;

        GameObject obj = new(RESPAWN_MARKER_NAME);
        obj.transform.position = data.Position;
        obj.tag = "RespawnPoint";

        var marker = obj.AddComponent<RespawnMarker>();
        marker.respawnFacingRight = data.FacingRight;
    }

    [MonoDetourHookInitialize]
    private static void Hook() => Md.GameManager.GetRespawnInfo.ControlFlowPrefix(OverrideGetRespawnInfo);
}

internal class SpawnPointSubMenu : ModuleSubMenu<SpawnPointSettings>
{
    private readonly ChoiceElement<SpawnPoint> SpawnPoint = new("Spawn Point", ChoiceModels.ForEnum<SpawnPoint>(), "Forced respawn point on death.");

    public override IEnumerable<MenuElement> Elements() => [SpawnPoint];

    internal override void Apply(SpawnPointSettings data) => SpawnPoint.Value = data.SpawnPoint;

    internal override SpawnPointSettings Export() => new() { SpawnPoint = SpawnPoint.Value };
}
