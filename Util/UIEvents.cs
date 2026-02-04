using PrepatcherPlugin;
using Silksong.FsmUtil;
using System.Collections.Generic;
using System.Linq;

namespace Silksong.TheHuntIsOn.Util;

internal static class UIEvents
{
    internal static void UpdateHealth()
    {
        if (PlayerDataAccess.health > PlayerData.instance.CurrentMaxHealth) PlayerDataAccess.health = PlayerData.instance.CurrentMaxHealth;

        foreach (var fsm in healthDisplays.Where(fsm => fsm.gameObject.activeInHierarchy))
        {
            if (!fsm.enabled) fsm.enabled = true;

            using var lease = suppressWait.TempAdd(fsm);
            fsm.SendEvent("HUD APPEAR RESET");
        }
    }

    private static readonly HashSet<PlayMakerFSM> healthDisplays = [];
    private static readonly TempSet<PlayMakerFSM> suppressWait = new();

    private static void TrackHealthDisplay(PlayMakerFSM fsm)
    {
        healthDisplays.Add(fsm);

        fsm.gameObject.DoOnDestroy(() => healthDisplays.Remove(fsm));
        fsm.GetState("Set Appear Pause")!.AddMethod(action =>
        {
            if (!suppressWait.Contains(action.Fsm.FsmComponent)) return;
            
            var vars = action.Fsm.variables;
            var num = vars.GetFsmInt("Health Number").Value;
            action.Fsm.variables.GetFsmFloat("Appear Pause").Value = num * 0.05f;
        });
    }

    private static readonly HashSet<PlayMakerFSM> silkSpoolFsms = [];
    private static readonly EventSuppressor disableSpoolAnimSkip = new();

    private static void SkipSpoolAnimation(PlayMakerFSM fsm)
    {
        silkSpoolFsms.Add(fsm);
        fsm.gameObject.DoOnDestroy(() => silkSpoolFsms.Remove(fsm));

        var initState = fsm.GetState("Init")!;
        initState.AddTransition("SKIP", "Appear Anim");
        initState.InsertMethod(0, _ =>
        {
            if (disableSpoolAnimSkip.Suppressed) fsm.SendEvent("SKIP");
        });
    }

    internal static void UpdateSilk()
    {
        if (PlayerDataAccess.silk > PlayerDataAccess.silkMax) PlayerDataAccess.silk = PlayerDataAccess.silkMax;

        using var lease = disableSpoolAnimSkip.Suppress();
        foreach (var fsm in silkSpoolFsms)
        {
            if (!fsm.enabled) fsm.enabled = true;
            fsm.SendEvent("HUD APPEAR RESET");
        }
    }

    private static bool loaded = false;

    internal static void Load()
    {
        if (loaded) return;
        loaded = true;

        Events.AddFsmEdit("health_display", TrackHealthDisplay);
        Events.AddFsmEdit("Spool", "Animate", SkipSpoolAnimation);
    }
}
