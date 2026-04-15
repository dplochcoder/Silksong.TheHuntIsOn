using System.Collections.Generic;
using Silksong.ModMenu.Elements;
using Silksong.TheHuntIsOn.Menu;
using Silksong.TheHuntIsOn.Modules.Lib;
using Silksong.TheHuntIsOn.SsmpAddon;
using Silksong.TheHuntIsOn.Util;
using UnityEngine;

namespace Silksong.TheHuntIsOn.Modules.PauseTimerModule;

internal class PauseTimerModule
    : Module<PauseTimerModule, EmptySettings, EmptySubMenu, PauseTimerUIConfig>
{
    private ServerPauseState serverPauseState = new();

    internal static ServerPauseState GetServerPauseState() =>
        IsEnabled ? (Instance?.serverPauseState ?? new()) : new();

    internal static PauseTimerUIConfig GetUIConfig() => Instance?.CosmeticConfig ?? new();

    public PauseTimerModule()
    {
        HuntClientAddon.On<ServerPauseState>.Received += state => serverPauseState = state;

        TimeManager.OnTimeScaleUpdated += value =>
        {
            prevTimeScale = value;
            UpdateTimeScale();
        };
        Events.OnGameManagerUpdate += UpdateTimeScale;

        PauseTimerUI.Load();
    }

    private float prevTimeScale = 1f;
    private bool inputBlocked;

    private void UpdateTimeScale()
    {
        var gs = GameManager.instance.GameState;
        bool shouldPause =
            HuntClientAddon.IsConnected
            && (gs == GlobalEnums.GameState.PLAYING || gs == GlobalEnums.GameState.PAUSED)
            && GetServerPauseState().IsServerPaused(out _);

        Time.timeScale = shouldPause ? 0 : prevTimeScale;

        var hc = HeroController.instance;
        if (hc != null)
        {
            if (shouldPause)
            {
                hc.acceptingInput = false;
                inputBlocked = true;
            }
            else if (inputBlocked)
            {
                hc.acceptingInput = true;
                inputBlocked = false;
            }
        }
    }

    protected override PauseTimerModule Self() => this;

    public override string Name => "Pause Timer";

    public override ModuleActivationType ModuleActivationType => ModuleActivationType.OnOffOnly;

    public override IEnumerable<MenuElement> CreateCosmeticsMenuElements() =>
        PauseTimerUIConfig.CreateMenu(CosmeticConfig, UpdateCosmeticConfig);
}
