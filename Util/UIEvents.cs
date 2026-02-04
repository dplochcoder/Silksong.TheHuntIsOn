namespace Silksong.TheHuntIsOn.Util;

internal static class UIEvents
{
    internal static void UpdateHealthAndSilk() => EventRegister.SendEvent("HUD APPEAR RESET");
}
