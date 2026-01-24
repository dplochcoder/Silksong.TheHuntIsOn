using Silksong.ModMenu.Elements;
using Silksong.TheHuntIsOn.Menu;
using Silksong.TheHuntIsOn.Modules.Lib;
using Silksong.TheHuntIsOn.SsmpAddon;
using Silksong.TheHuntIsOn.Util;
using System.Collections.Generic;

namespace Silksong.TheHuntIsOn.Modules.PauseTimerModule;

internal class PauseTimerModule : Module<PauseTimerModule, Empty, EmptySubMenu, PauseTimerUIConfig>
{
    private ServerPauseState serverPauseState = new();

    public PauseTimerModule() => HuntClientAddon.OnServerPauseState += state => serverPauseState = state;

    protected override PauseTimerModule Self() => this;

    public override string Name => "Pause Timer";

    public override ModuleActivationType ModuleActivationType => ModuleActivationType.OnOffOnly;

    public override IEnumerable<MenuElement> CreateCosmeticsMenuElements() => PauseTimerUIConfig.CreateMenu(CosmeticConfig, UpdateCosmeticConfig);

    // FIXME: UI and application.
}
