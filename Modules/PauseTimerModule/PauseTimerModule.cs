using Silksong.ModMenu.Elements;
using Silksong.TheHuntIsOn.Menu;
using Silksong.TheHuntIsOn.Modules.Lib;
using Silksong.TheHuntIsOn.SsmpAddon;
using Silksong.TheHuntIsOn.Util;
using System.Collections.Generic;
using UnityEngine;

namespace Silksong.TheHuntIsOn.Modules.PauseTimerModule;

internal class PauseTimerModule : Module<PauseTimerModule, EmptySettings, EmptySubMenu, PauseTimerUIConfig>
{
    private ServerPauseState serverPauseState = new();

    internal static ServerPauseState GetServerPauseState() => IsEnabled ? (Instance?.serverPauseState ?? new()) : new();

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

    private void UpdateTimeScale()
    {
        var gs = GameManager.instance.GameState;
        Time.timeScale = (HuntClientAddon.IsConnected && (gs == GlobalEnums.GameState.PLAYING || gs == GlobalEnums.GameState.PAUSED) && GetServerPauseState().IsServerPaused(out _)) ? 0 : prevTimeScale;
    }

    protected override PauseTimerModule Self() => this;

    public override string Name => "Pause Timer";

    public override ModuleActivationType ModuleActivationType => ModuleActivationType.OnOffOnly;

    public override IEnumerable<MenuElement> CreateCosmeticsMenuElements() => PauseTimerUIConfig.CreateMenu(CosmeticConfig, UpdateCosmeticConfig);
}
