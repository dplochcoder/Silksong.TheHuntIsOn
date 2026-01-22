using Silksong.ModMenu.Elements;
using Silksong.ModMenu.Models;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Silksong.TheHuntIsOn.Modules.PauseTimerModule;

internal enum PauseTimerPosition
{
    BottomLeft,
    BottomCenter,
    BottomRight,
    CenterLeft,
    CenterRight,
    TopCenter,
    TopRight,
    BelowHud,
}

internal record PauseTimerSpacingParameters(PauseTimerPosition Position, Vector2 AnchorPos, Vector2 ForwardSpace, Vector2 ReverseSpace)
{
    public Vector2 GetPosition(int i, int count, float spacing, float scale, Bounds localBounds)
    {
        Vector2 pos = AnchorPos + spacing * (ForwardSpace * i + ReverseSpace * (count - 1 - i));
        Vector2 pivot = new(
            Position.IsLeft() ? -localBounds.min.x : (Position.IsRight() ? -localBounds.max.x : -localBounds.center.x),
            Position.IsBottom() ? -localBounds.min.y : (Position.IsTop() ? -localBounds.max.y : -localBounds.center.y));
        return pos + pivot * scale;
    }
}

public static class PauseTimerPositionExtensions
{
    internal static bool IsLeft(this PauseTimerPosition self) => self == PauseTimerPosition.BelowHud || self == PauseTimerPosition.CenterLeft || self == PauseTimerPosition.BottomLeft;

    internal static bool IsRight(this PauseTimerPosition self) => self == PauseTimerPosition.BottomRight || self == PauseTimerPosition.CenterRight || self == PauseTimerPosition.TopRight;

    internal static bool IsTop(this PauseTimerPosition self) => self == PauseTimerPosition.BelowHud || self == PauseTimerPosition.TopCenter || self == PauseTimerPosition.TopRight;

    internal static bool IsBottom(this PauseTimerPosition self) => self == PauseTimerPosition.BottomLeft || self == PauseTimerPosition.BottomCenter || self == PauseTimerPosition.BottomRight;

    internal static bool IsVCenter(this PauseTimerPosition self) => self == PauseTimerPosition.CenterLeft || self == PauseTimerPosition.CenterRight;

    internal static PauseTimerSpacingParameters SpacingParameters(this PauseTimerPosition self)
    {
        Vector2 anchorPos;
        anchorPos.x = self.IsLeft() ? -14.5f : (self.IsRight() ? 14.5f : 0);
        anchorPos.y = self.IsTop() ? 7.5f : (self.IsBottom() ? -8.25f : 0);
        if (self == PauseTimerPosition.BelowHud) anchorPos.y = 4.5f;
        if (self == PauseTimerPosition.BottomLeft) anchorPos.y = -4.5f;

        Vector2 forwardSpace = new(0, self.IsTop() ? -1 : (self.IsVCenter() ? -0.5f : 0));
        Vector2 reverseSpace = new(0, self.IsBottom() ? 1 : (self.IsVCenter() ? 0.5f : 0));
        return new(self, anchorPos, forwardSpace, reverseSpace);
    }
}

internal enum PauseTimerSize
{
    Normal,
    Small,
    Large,
}

internal static class PauseTimerSizeExtensions
{
    internal static float FontScale(this PauseTimerSize size) => size switch { PauseTimerSize.Normal => 0.3f, PauseTimerSize.Small => 0.2f, PauseTimerSize.Large => 0.4f, _ => 0.3f };

    internal static float Spacing(this PauseTimerSize size) => size switch { PauseTimerSize.Normal => 0.95f, PauseTimerSize.Small => 0.6f, PauseTimerSize.Large => 1.3f, _ => 0.95f };
}

internal class PauseTimerUIConfig
{
    public PauseTimerPosition PauseTimerPosition = PauseTimerPosition.BottomCenter;
    public PauseTimerSize PauseTimerSize = PauseTimerSize.Normal;

    public static IEnumerable<MenuElement> CreateMenu(PauseTimerUIConfig config, Action<Action<PauseTimerUIConfig>> editor)
    {
        ChoiceElement<PauseTimerPosition> pauseTimerPosition = new("Position", ChoiceModels.ForEnum<PauseTimerPosition>(), "Where to display the pause timer.") { Value = config.PauseTimerPosition };
        pauseTimerPosition.OnValueChanged += value => editor(c => c.PauseTimerPosition = value);
        yield return pauseTimerPosition;

        ChoiceElement<PauseTimerSize> pauseTimerSize = new("Size", ChoiceModels.ForEnum<PauseTimerSize>(), "Font size for the pause timer.") { Value = config.PauseTimerSize };
        pauseTimerSize.OnValueChanged += value => editor(c => c.PauseTimerSize = value);
        yield return pauseTimerSize;
    }
}
