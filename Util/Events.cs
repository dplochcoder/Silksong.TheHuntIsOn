using MonoDetour;
using MonoDetour.HookGen;
using Silksong.PurenailUtil.Collections;
using System;
using UnityEngine.SceneManagement;

namespace Silksong.TheHuntIsOn.Util;

[MonoDetourTargets(typeof(GameManager))]
[MonoDetourTargets(typeof(HeroController))]
[MonoDetourTargets(typeof(PlayMakerFSM))]
internal static class Events
{
    private static readonly HashMultimap<string, Func<int>> pdIntModifiers = [];

    internal static void AddPdIntModifier(string name, Func<int> modifier) => pdIntModifiers.Add(name, modifier);
    internal static void RemovePdIntModifier(string name, Func<int> modifier) => pdIntModifiers.Remove(name, modifier);

    private static int OverrideGetPDInt(PlayerData playerData, string name, int current)
    {
        foreach (var modifier in pdIntModifiers.Get(name)) current += modifier();
        return current;
    }

    private static int OverrideSetPDInt(PlayerData playerData, string name, int current)
    {
        foreach (var modifier in pdIntModifiers.Get(name)) current -= modifier();
        return current;
    }

    internal static event Action<Scene>? OnNewScene;
    private static readonly HashMultimap<string, Action<Scene>> sceneEditsByName = [];

    internal static void AddSceneEdit(string sceneName, Action<Scene> edit) => sceneEditsByName.Add(sceneName, edit);
    internal static void RemoveSceneEdit(string sceneName, Action<Scene> edit) => sceneEditsByName.Remove(sceneName, edit);

    private static void OnLevelActivated(GameManager self, ref Scene before, ref Scene after)
    {
        OnNewScene?.Invoke(after);
        foreach (var edit in sceneEditsByName.Get(after.name)) edit(after);
    }

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

    internal static event Action? OnHeroUpdate;

    private static void PostfixOnHeroUpdate(HeroController self) => OnHeroUpdate?.Invoke();

    internal static event Action? OnGameManagerUpdate;

    private static void PostfixOnGameManagerUpdate(GameManager self) => OnGameManagerUpdate?.Invoke();

    [MonoDetourHookInitialize]
    private static void Hook()
    {
        PrepatcherPlugin.PlayerDataVariableEvents<int>.OnGetVariable += OverrideGetPDInt;
        PrepatcherPlugin.PlayerDataVariableEvents<int>.OnSetVariable += OverrideSetPDInt;
        Md.GameManager.LevelActivated.Prefix(OnLevelActivated);
        Md.GameManager.Update.Postfix(PostfixOnGameManagerUpdate);
        Md.HeroController.Update.Postfix(PostfixOnHeroUpdate);
        Md.PlayMakerFSM.OnEnable.Postfix(OnEnablePlayMakerFSM);
    }
}
