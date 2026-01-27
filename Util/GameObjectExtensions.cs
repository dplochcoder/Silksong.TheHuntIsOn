using Silksong.UnityHelper.Extensions;
using System;
using UnityEngine;

namespace Silksong.TheHuntIsOn.Util;

internal static class GameObjectExtensions
{
    internal static void DoOnUpdate(this GameObject self, Action action) => self.GetOrAddComponent<OnUpdateHelper>().OnUpdate += action;
}

internal class OnUpdateHelper : MonoBehaviour
{
    internal event Action? OnUpdate;

    private void Update() => OnUpdate?.Invoke();
}