using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Silksong.FsmUtil.Actions;
using System;

namespace Silksong.TheHuntIsOn.Util;

internal static class FsmEdits
{
    internal static bool IsCallMethodProper(this FsmStateAction self, string behaviour, string methodName) => self is CallMethodProper call && call.behaviour.Value == behaviour && call.methodName.Value == methodName;

    internal static bool IsCallMethodProper<T>(this FsmStateAction self, string methodName) => self.IsCallMethodProper(typeof(T).Name, methodName);

    internal static void ReplaceActions(this FsmState self, Func<FsmStateAction, bool> matcher, Action action)
    {
        for (int i = 0; i < self.Actions.Length; i++)
            if (matcher(self.Actions[i]))
                self.Actions[i] = new LambdaAction() { Method = action };
    }

    internal static void ReplaceActions(this PlayMakerFSM self, Func<FsmStateAction, bool> matcher, Action action)
    {
        foreach (var state in self.FsmStates) state.ReplaceActions(matcher, action);
    }
}
