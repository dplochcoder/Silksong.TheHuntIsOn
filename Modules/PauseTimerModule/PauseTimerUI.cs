using GlobalEnums;
using MonoDetour;
using MonoDetour.HookGen;
using Silksong.TheHuntIsOn.SsmpAddon;
using Silksong.TheHuntIsOn.Util;
using Silksong.UnityHelper.Extensions;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UObject = UnityEngine.Object;

namespace Silksong.TheHuntIsOn.Modules.PauseTimerModule;

[MonoDetourTargets(typeof(GameManager))]
internal class PauseTimerUI
{
    private static PauseTimerUI? instance;

    private GameObject? parent;
    private readonly List<Text> textCache = [];

    private float respawnTimer;

    internal PauseTimerUI()
    {
        instance = this;
        Events.OnGameManagerUpdate += Update;
    }

    private bool EnsureParent()
    {
        if (parent != null) return true;
        textCache.Clear();

        var camera = GameObject.Find("_UIManager/UICanvas");
        if (camera == null) return false;

        parent = new("CountdownsDisplayer");
        parent.AddComponent<RectTransform>();
        parent.transform.SetParent(camera.transform);
        parent.transform.localPosition = Vector3.zero;
        return true;
    }

    private static string FormatTime(float timeSeconds)
    {
        if (timeSeconds <= 0) return "0.00";
        if (timeSeconds >= 3600)
        {
            int hours = Mathf.FloorToInt(timeSeconds / 3600);
            int minutes = Mathf.FloorToInt((timeSeconds % 3600) / 60);
            if (minutes >= 60) minutes = 59;

            string status = $"{hours} {(hours > 1 ? "hours" : "hour")}";
            if (minutes > 0) status = $"{status} and {minutes} {(minutes > 1 ? "minutes" : "minute")}";
            return status;
        }
        if (timeSeconds >= 60)
        {
            int minutes = Mathf.FloorToInt(timeSeconds / 60);
            int seconds = Mathf.FloorToInt(timeSeconds % 60);
            if (seconds >= 60) seconds = 59;
            return $"{minutes}:{seconds:00}";
        }
        if (timeSeconds >= 10) return $"{timeSeconds:00.0}";
        return $"{timeSeconds:0.00}";
    }

    private List<string> ComputeStatuses()
    {
        List<string> statuses = [];

        var state = PauseTimerModule.GetServerPauseState();
        if (state.IsServerPaused(out var unpauseSeconds))
        {
            if (!unpauseSeconds.HasValue) statuses.Add("Server Paused");
            else statuses.Add($"Unpausing in: {FormatTime(unpauseSeconds.Value)}");
        }
        if (respawnTimer > 0) statuses.Add($"Respawn in: {FormatTime(respawnTimer)}");

        foreach (var countdown in state.Countdowns)
        {
            if (countdown.GetDisplayTime(out float seconds)) statuses.Add($"{countdown.Message}: {FormatTime(seconds)}");
        }

        return statuses;
    }

    private static Font? _font;
    private static Font Font
    {
        get
        {
            if (_font == null) _font = Resources.FindObjectsOfTypeAll<Font>().Where(f => f.name == "TrajanPro-Bold").First();
            return _font;
        }
    }

    private void CreateText()
    {
        GameObject obj = new("Display");
        obj.transform.SetParent(parent!.transform);

        var text = obj.AddComponent<Text>();
        text.font = Font;
        text.color = Color.white;

        obj.layer = (int)PhysLayers.UI;
        obj.SetActive(false);
        textCache.Add(text);
    }

    private void UpdateStatuses(List<string> statuses)
    {
        var state = PauseTimerModule.GetServerPauseState();
        var config = PauseTimerModule.GetUIConfig();
        var spacingParameters = config.PauseTimerPosition.SpacingParameters();

        for (int i = 0; i < textCache.Count; i++)
        {
            var text = textCache[i];
            var status = i < statuses.Count ? statuses[i] : "";
            if (status == "")
            {
                text.gameObject.SetActive(false);
                text.text = "";
                text.transform.position = new(-100, -100);
                continue;
            }

            text.gameObject.SetActive(true);
            text.text = status;
            text.fontSize = 24;

            var scale = config.PauseTimerSize.FontScale();
            text.transform.localScale = new(scale, scale, 1);
            text.transform.localPosition = spacingParameters.GetPosition(i, statuses.Count, config.PauseTimerSize.Spacing(), scale, text.rectTransform.GetBounds());
        }
    }

    private void Update()
    {
        if (respawnTimer > 0 && HuntClientAddon.IsConnected && !PauseTimerModule.GetServerPauseState().IsServerPaused(out _))
        {
            respawnTimer -= Time.unscaledDeltaTime;
            if (respawnTimer < 0) respawnTimer = 0;
        }

        if (!EnsureParent()) return;

        List<string> statuses = [];
        if (HuntClientAddon.IsConnected) statuses = ComputeStatuses();
        while (textCache.Count < statuses.Count) CreateText();
        UpdateStatuses(statuses);
    }

    private IEnumerator WaitForRespawn(IEnumerator orig)
    {
        if (respawnTimer <= 0 || !HuntClientAddon.IsConnected) return orig;

        IEnumerator Modified()
        {
            yield return new WaitUntil(() => respawnTimer <= 0 || !HuntClientAddon.IsConnected);
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