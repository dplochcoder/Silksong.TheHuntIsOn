using MonoDetour;
using MonoDetour.Cil;
using MonoDetour.HookGen;
using MonoMod.Cil;
using Silksong.ModMenu.Elements;
using Silksong.ModMenu.Models;
using Silksong.TheHuntIsOn.Menu;
using Silksong.TheHuntIsOn.Modules.Lib;
using Silksong.TheHuntIsOn.SsmpAddon.PacketUtil;
using Silksong.TheHuntIsOn.Util;
using SSMP.Networking.Packet;
using System;
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

[MonoDetourTargets(typeof(GameManager))]
internal class SpawnPointModule : GlobalSettingsModule<SpawnPointModule, SpawnPointSettings, SpawnPointSubMenu>
{
    internal SpawnPointModule() => Events.OnNewScene += OnNewScene;

    protected override SpawnPointModule Self() => this;

    public override string Name => "Spawn Point";

    public override ModuleActivationType ModuleActivationType => ModuleActivationType.AnyConfiguration;

    public override void OnEnabled()
    {
        PrepatcherPlugin.PlayerDataVariableEvents<string>.OnGetVariable += OverrideRespawnString;
        PrepatcherPlugin.PlayerDataVariableEvents<int>.OnGetVariable += OverrideRespawnInt;
    }

    public override void OnDisabled()
    {
        PrepatcherPlugin.PlayerDataVariableEvents<string>.OnGetVariable -= OverrideRespawnString;
        PrepatcherPlugin.PlayerDataVariableEvents<int>.OnGetVariable -= OverrideRespawnInt;
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

    private static readonly Lazy<Dictionary<string, RespawnData>> respawnDataByScene = new(() =>
    {
        Dictionary<string, RespawnData> dict = [];
        foreach (var d in respawnData.Values) dict[d.Scene] = d;
        return dict;
    });

    private const string RESPAWN_MARKER_NAME = "TheHuntIsOn-RespawnMarker";

    private static void OverridePlayerDeadMoveNext(ILManipulationInfo info)
    {
        ILCursor cursor = new(info.Context);
        cursor.Goto(0);

        // Override the SpawnPreloader scene.
        if (cursor.TryGotoNext(i => i.MatchCall<ScenePreloader>(nameof(ScenePreloader.SpawnPreloader))) && cursor.TryGotoPrev(MoveType.After, i => i.MatchLdloc(3)))
        {
            static string Delegate(string scene) => GetRespawnData(out var data) ? data.Scene : scene;
            cursor.EmitDelegate(Delegate);
        }
    }

    private static void OverrideReadyForRespawn(ILManipulationInfo info)
    {
        ILCursor cursor = new(info.Context);
        cursor.Goto(0);

        if (cursor.TryGotoNext(i => i.MatchCall<GameManager>(nameof(GameManager.BeginSceneTransition))))
        {
            cursor.Remove();

            // Override the scene and entry gate for the next transition.
            static void Delegate(GameManager self, GameManager.SceneLoadInfo info)
            {
                if (GetRespawnData(out var data))
                {
                    info.SceneName = data.Scene;
                    info.EntryGateName = RESPAWN_MARKER_NAME;
                }
                self.BeginSceneTransition(info);
            }
            cursor.EmitDelegate(Delegate);
        }
    }

    private string OverrideRespawnString(PlayerData self, string name, string current) => name switch
    {
        nameof(PlayerData.respawnScene) => GetRespawnData(out var data) ? data.Scene : current,
        nameof(PlayerData.respawnMarkerName) => GlobalConfig.SpawnPoint != SpawnPoint.Unchanged ? RESPAWN_MARKER_NAME : current,
        nameof(PlayerData.tempRespawnScene) => GlobalConfig.SpawnPoint != SpawnPoint.Unchanged ? "" : current,
        nameof(PlayerData.tempRespawnMarker) => GlobalConfig.SpawnPoint != SpawnPoint.Unchanged ? "" : current,
        _ => current,
    };

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
        marker.customFadeDuration = new();
        marker.overrideMapZone = new();
        obj.SetActive(true);
    }

    [MonoDetourHookInitialize]
    private static void Hook()
    {
        Md.GameManager._PlayerDead_d__245.MoveNext.ILHook(OverridePlayerDeadMoveNext);
        Md.GameManager.ReadyForRespawn.ILHook(OverrideReadyForRespawn);
    }
}

internal class SpawnPointSubMenu : ModuleSubMenu<SpawnPointSettings>
{
    private readonly ChoiceElement<SpawnPoint> SpawnPoint = new("Spawn Point", ChoiceModels.ForEnum<SpawnPoint>(), "Forced respawn point on death.");

    public override IEnumerable<MenuElement> Elements() => [SpawnPoint];

    internal override void Apply(SpawnPointSettings data) => SpawnPoint.Value = data.SpawnPoint;

    internal override SpawnPointSettings Export() => new() { SpawnPoint = SpawnPoint.Value };
}
