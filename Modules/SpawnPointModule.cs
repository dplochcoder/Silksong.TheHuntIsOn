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

internal class SpawnPointSettings : NetworkedCloneable<SpawnPointSettings>
{
    public SpawnPoint SpawnPoint = SpawnPoint.Unchanged;

    public override void ReadData(IPacket packet) => SpawnPoint = packet.ReadEnum<SpawnPoint>();

    public override void WriteData(IPacket packet) => SpawnPoint.WriteData(packet);
}

internal class SpawnPointModule : GlobalSettingsModule<SpawnPointModule, SpawnPointSettings, SpawnPointSubMenu>
{
    internal class RespawnData
    {
        public string Scene = "";
        public Vector2 Position;
        public bool FacingRight;
    }

    protected override SpawnPointModule Self() => this;

    public override string Name => "Spawn Point";

    public override ModuleActivationType ModuleActivationType => ModuleActivationType.AnyConfiguration;

    public override void OnEnabled()
    {
        PrepatcherPlugin.PlayerDataVariableEvents<string>.OnGetVariable += OverrideRespawnString;
        PrepatcherPlugin.PlayerDataVariableEvents<int>.OnGetVariable += OverrideRespawnInt;
        Events.OnNewScene += OnNewScene;
    }

    public override void OnDisabled()
    {
        PrepatcherPlugin.PlayerDataVariableEvents<string>.OnGetVariable -= OverrideRespawnString;
        PrepatcherPlugin.PlayerDataVariableEvents<int>.OnGetVariable -= OverrideRespawnInt;
        Events.OnNewScene -= OnNewScene;
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

    private bool GetRespawnData(out RespawnData data) => respawnData.TryGetValue(GlobalConfig.SpawnPoint, out data);

    private const string RESPAWN_MARKER_NAME = "TheHuntIsOn-RespawnMarker";

    private string OverrideRespawnString(PlayerData playerData, string name, string orig) =>
        name switch
    {
        nameof(PlayerData.respawnScene) => GetRespawnData(out var data) ? data.Scene : orig,
        nameof(PlayerData.respawnMarkerName) => GetRespawnData(out _) ? RESPAWN_MARKER_NAME : orig,
        _ => orig,
    };

    private int OverrideRespawnInt(PlayerData playerData, string name, int orig) => name switch
    {
        nameof(PlayerData.respawnType) => GetRespawnData(out _) ? 0 : orig,
        _ => orig
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
}

internal class SpawnPointSubMenu : ModuleSubMenu<SpawnPointSettings>
{
    private readonly ChoiceElement<SpawnPoint> SpawnPoint = new("Spawn Point", ChoiceModels.ForEnum<SpawnPoint>(), "Forced respawn point on death.");

    public override IEnumerable<MenuElement> Elements() => [SpawnPoint];

    internal override void Apply(SpawnPointSettings data) => SpawnPoint.Value = data.SpawnPoint;

    internal override SpawnPointSettings Export() => new() { SpawnPoint = SpawnPoint.Value };
}
