using Silksong.ModMenu.Elements;
using Silksong.TheHuntIsOn.Menu;
using Silksong.TheHuntIsOn.Modules.Lib;
using System.Collections.Generic;

namespace Silksong.TheHuntIsOn.Modules.PauseTimerModule;

internal class PauseTimerModule : Module<PauseTimerModule, EmptySettings, EmptySubMenu, PauseTimerUIConfig, ServerPauseState>
{
    protected override PauseTimerModule Self() => this;

    public override string Name => "Pause Timer";

    public override IEnumerable<MenuElement> CreateCosmeticsMenuElements() => PauseTimerUIConfig.CreateMenu(CosmeticConfig, UpdateCosmeticConfig);
}
