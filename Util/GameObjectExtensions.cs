using Silksong.UnityHelper.Extensions;
using System;
using UnityEngine;

namespace Silksong.TheHuntIsOn.Util;

internal static class GameObjectExtensions
{
    internal static void DoOnUpdate(this GameObject self, Action action) => self.GetOrAddComponent<OnUpdateHelper>().OnUpdate += action;

    internal static Bounds GetBounds(this RectTransform self)
    {
        var arr = new Vector3[4];
        self.GetWorldCorners(arr);
        var a = arr[0];
        var b = arr[2];

        Vector2 center = new((a.x + b.x) / 2, (a.y + b.y) / 2);
        Vector2 size = new(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
        return new(center, size);
    }
}

internal class OnUpdateHelper : MonoBehaviour
{
    internal event Action? OnUpdate;

    private void Update() => OnUpdate?.Invoke();
}