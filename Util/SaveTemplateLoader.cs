using System;
using System.IO;
using System.Reflection;
using Silksong.TheHuntIsOn.Modules.Lib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Silksong.TheHuntIsOn.Util;

internal static class SaveTemplateLoader
{
    private static string GetResourceName(RoleId role) =>
        role switch
        {
            RoleId.Speedrunner => "Silksong.TheHuntIsOn.Resources.Data.Start.speedrunner.dat",
            RoleId.Hunter => "Silksong.TheHuntIsOn.Resources.Data.Start.hunter.dat",
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, null),
        };

    private const string StartScene = "Tut_01";
    private const string SpawnMarkerName = "TheHuntIsOn-StartMarker";
    private static readonly Vector2 StartPosition = new(54.5f, 5f);

    private static bool pendingStartMarker;

    internal static void LoadTemplateForRole(RoleId role)
    {
        var profileId = GameManager.instance.profileID;
        if (profileId <= 0)
            return;

        var resourceName = GetResourceName(role);
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
        if (stream == null)
            return;

        using MemoryStream ms = new();
        stream.CopyTo(ms);
        var fileBytes = ms.ToArray();

        var jsonData = GameManager.instance.GetJsonForSaveBytes(fileBytes);
        GameManager.instance.SetLoadedGameData(jsonData, profileId);

        // Override respawn data to point to the start scene.
        var pd = PlayerData.instance;
        pd.respawnScene = StartScene;
        pd.respawnMarkerName = SpawnMarkerName;
        pd.respawnType = 0;
        pd.tempRespawnScene = "";
        pd.tempRespawnMarker = "";

        // Reset health and silk to base values so StartModule can apply its modifiers cleanly.
        pd.maxHealth = 5;
        pd.maxHealthBase = 5;
        pd.silkMax = 9;

        // Flag to create a spawn marker on next scene load.
        if (!pendingStartMarker)
        {
            pendingStartMarker = true;
            Events.OnEnterScene += OnEnterSceneCreateMarker;
        }

        GameManager.instance.ContinueGame();
    }

    private static void OnEnterSceneCreateMarker(Scene scene)
    {
        pendingStartMarker = false;
        Events.OnEnterScene -= OnEnterSceneCreateMarker;

        if (scene.name != StartScene)
            return;

        GameObject obj = new(SpawnMarkerName);
        obj.transform.position = StartPosition;
        obj.tag = "RespawnPoint";

        var marker = obj.AddComponent<RespawnMarker>();
        marker.respawnFacingRight = false;
        marker.customFadeDuration = new();
        marker.overrideMapZone = new();
        obj.SetActive(true);
    }
}
