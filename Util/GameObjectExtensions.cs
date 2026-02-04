using Silksong.UnityHelper.Extensions;
using System;
using UnityEngine;

namespace Silksong.TheHuntIsOn.Util;

internal static class GameObjectExtensions
{
    internal static void DoOnDestroy(this GameObject self, Action action) => self.GetOrAddComponent<OnDestroyHelper>().Action += action;

    internal static void DoOnUpdate(this GameObject self, Action action) => self.GetOrAddComponent<OnUpdateHelper>().Action += action;
}

internal class OnDestroyHelper : MonoBehaviour
{
    internal event Action? Action;

    private void OnDestroy() => Action?.Invoke();
}

internal class OnUpdateHelper : MonoBehaviour
{
    internal event Action? Action;

    private void Update() => Action?.Invoke();
}