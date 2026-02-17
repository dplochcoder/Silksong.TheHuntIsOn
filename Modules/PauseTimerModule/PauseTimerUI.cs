using GlobalEnums;
using MonoDetour;
using MonoDetour.HookGen;
using Silksong.TheHuntIsOn.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMProOld;
using UnityEngine;
using UObject = UnityEngine.Object;

namespace Silksong.TheHuntIsOn.Modules.PauseTimerModule;

[MonoDetourTargets(typeof(GameManager))]
internal class PauseTimerUI
{
    private record StatusMessage(string Actual, string Spacing)
    {
        public static readonly StatusMessage Empty = new("");

        public StatusMessage(string singleton) : this(singleton, singleton) { }

        public readonly string Actual = Actual;
        public readonly string Spacing = Spacing;
    }

    private static readonly PauseTimerUI instance = new();

    private PauseTimerUI() => Events.OnGameManagerUpdate += Update;

    // Call to force class-loading.
    internal static void Load() { }

    private GameObject? parent;
    private readonly List<TextMeshPro> textCache = [];

    private float respawnTimer;

    private bool EnsureParent()
    {
        if (parent != null) return true;
        textCache.Clear();

        var cameraParent = GameObject.Find("_GameCameras/HudCamera/In-game");
        if (cameraParent == null) return false;

        parent = new("CountdownsDisplayer");
        UObject.DontDestroyOnLoad(parent);
        parent.transform.SetParent(cameraParent.transform);
        parent.transform.localPosition = Vector3.zero;
        return true;
    }

    private static StatusMessage MessageWithTime(string msg, float time) => new($"{msg}: {FormatTime(time)}", $"{msg}: {FormatTimeSpacing(time)}");

    private static string FormatHours(float timeSeconds)
    {
        int hours = Mathf.FloorToInt(timeSeconds / 3600);
        int minutes = Mathf.FloorToInt((timeSeconds % 3600) / 60);
        if (minutes >= 60) minutes = 59;

        string status = $"{hours} {(hours > 1 ? "hours" : "hour")}";
        if (minutes > 0) status = $"{status} and {minutes} {(minutes > 1 ? "minutes" : "minute")}";
        return status;
    }

    private static string FormatTime(float timeSeconds)
    {
        if (timeSeconds <= 0) return "0.00";
        else if (timeSeconds < 10) return $"{timeSeconds:0.00}";
        else if (timeSeconds < 60) return $"{timeSeconds:00.0}";
        else if (timeSeconds < 3600)
        {
            int minutes = Mathf.FloorToInt(timeSeconds / 60);
            int seconds = Mathf.FloorToInt(timeSeconds % 60);
            if (seconds >= 60) seconds = 59;
            return $"{minutes}:{seconds:00}";
        }
        else return FormatHours(timeSeconds);
    }

    private static string FormatTimeSpacing(float timeSeconds)
    {
        if (timeSeconds < 10) return "0.00";
        else if (timeSeconds < 60) return "00.0";
        else if (timeSeconds < 600) return "0:00";
        else if (timeSeconds < 3600) return "00:00";
        else return FormatHours(timeSeconds);
    }

    private List<StatusMessage> ComputeStatuses()
    {
        List<StatusMessage> statuses = [];

        var state = PauseTimerModule.GetServerPauseState();
        if (state.IsServerPaused(out var unpauseSeconds))
        {
            if (!unpauseSeconds.HasValue) statuses.Add(new("Server Paused"));
            else statuses.Add(MessageWithTime("Unpausing in", unpauseSeconds.Value));
        }
        if (respawnTimer > 0) statuses.Add(MessageWithTime("Respawn in", respawnTimer));

        foreach (var countdown in state.Countdowns)
        {
            if (countdown.GetDisplayTime(out float seconds)) statuses.Add(MessageWithTime(countdown.Message, seconds));
        }

        return statuses;
    }

    private static readonly Lazy<TMP_FontAsset> Font = new(() => Resources.FindObjectsOfTypeAll<TMP_FontAsset>().Where(f => f.name == "trajan_bold_tmpro").First());

    private void CreateText()
    {
        GameObject obj = new("Display") { layer = (int)PhysLayers.UI };
        obj.transform.SetParent(parent!.transform);

        var text = obj.AddComponent<TextMeshPro>();
        text.font = Font.Value;
        text.color = Color.white;
        text.enableWordWrapping = false;
        text.autoSizeTextContainer = true;

        var renderer = obj.GetComponent<MeshRenderer>();
        renderer.sortingLayerName = "HUD";
        renderer.sortingOrder = 11;

        obj.SetActive(false);
        textCache.Add(text);
    }

    private static readonly Vector2 INACTIVE_POS = new(-1000, -1000);

    private void UpdateStatuses(List<StatusMessage> statuses)
    {
        var config = PauseTimerModule.GetUIConfig();
        var spacingParameters = config.PauseTimerPosition.SpacingParameters();

        for (int i = 0; i < textCache.Count; i++)
        {
            var text = textCache[i];
            var status = i < statuses.Count ? statuses[i] : StatusMessage.Empty;
            if (status.Actual == "")
            {
                text.gameObject.SetActive(false);
                text.text = "";
                text.transform.position = INACTIVE_POS;
                continue;
            }

            text.gameObject.SetActive(true);

            var scale = config.PauseTimerSize.FontScale();
            text.transform.localScale = new(scale, scale, 1);
            text.fontSize = 40;

            text.text = status.Spacing;
            text.ForceMeshUpdate();
            text.transform.position = spacingParameters.GetPosition(i, statuses.Count, config.PauseTimerSize.Spacing(), scale, text.bounds);
            text.text = status.Actual;
        }
    }

    internal static void ShortenRespawn(float reduce)
    {
        if (instance == null) return;
        instance.respawnTimer = Mathf.Max(0, instance.respawnTimer - reduce);
    }

    private void Update()
    {
        if (respawnTimer > 0 && !PauseTimerModule.GetServerPauseState().IsServerPaused(out _))
        {
            respawnTimer -= Time.unscaledDeltaTime;
            if (respawnTimer < 0) respawnTimer = 0;
        }

        if (!EnsureParent()) return;

        List<StatusMessage> statuses = ComputeStatuses();
        while (textCache.Count < statuses.Count) CreateText();
        UpdateStatuses(statuses);
    }

    private IEnumerator WaitForRespawn(IEnumerator orig)
    {
        if (respawnTimer <= 0) return orig;

        IEnumerator Modified()
        {
            yield return new WaitUntil(() => respawnTimer <= 0);
            while (orig.MoveNext()) yield return orig.Current;
        }
        return Modified();
    }

    private static void PostfixBeginSceneTransitionRoutine(GameManager self, ref GameManager.SceneLoadInfo sceneLoadInfo, ref IEnumerator returnValue) => returnValue = instance?.WaitForRespawn(returnValue) ?? returnValue;

    private static void PostfixPlayerDead(GameManager self, ref float waitTime, ref IEnumerator coroutine)
    {
        if (instance == null) return;
        instance.respawnTimer = Mathf.Max(instance.respawnTimer, DeathModule.GetRespawnTimer());
    }

    [MonoDetourHookInitialize]
    private static void Hook()
    {
        Md.GameManager.BeginSceneTransitionRoutine.Postfix(PostfixBeginSceneTransitionRoutine);
        Md.GameManager.PlayerDead.Postfix(PostfixPlayerDead);
    }
}