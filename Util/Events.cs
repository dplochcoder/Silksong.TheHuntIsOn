using MonoDetour;
using MonoDetour.HookGen;
using Silksong.PurenailUtil.Collections;
using System;

namespace Silksong.TheHuntIsOn.Util;

[MonoDetourTargets(typeof(PlayMakerFSM))]
internal static class Events
{
    private static readonly HashMultimap<string, Action<PlayMakerFSM>> fsmEditsByName = [];
    private static readonly HashMultitable<string, string, Action<PlayMakerFSM>> fsmEdits = [];

    internal static void AddFsmEdit(string fsmName, Action<PlayMakerFSM> fsmEdit) => fsmEditsByName.Add(fsmName, fsmEdit);
    internal static void AddFsmEdit(string objName, string fsmName, Action<PlayMakerFSM> fsmEdit) => fsmEdits.Add(objName, fsmName, fsmEdit);
    internal static void RemoveFsmEdit(string fsmName, Action<PlayMakerFSM> fsmEdit) => fsmEditsByName.Remove(fsmName, fsmEdit);
    internal static void RemoveFsmEdit(string objName, string fsmName, Action<PlayMakerFSM> fsmEdit) => fsmEdits.Remove(objName, fsmName, fsmEdit);

    private static void OnEnablePlayMakerFSM(PlayMakerFSM fsm)
    {
        foreach (var action in fsmEditsByName.Get(fsm.FsmName)) action(fsm);
        foreach (var action in fsmEdits.Get(fsm.gameObject.name, fsm.FsmName)) action(fsm);
    }

    [MonoDetourHookInitialize]
    private static void Hook() => Md.PlayMakerFSM.OnEnable.Postfix(OnEnablePlayMakerFSM);
}
