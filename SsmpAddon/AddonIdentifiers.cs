using System.Reflection;

namespace Silksong.TheHuntIsOn.SsmpAddon;

internal static class AddonIdentifiers
{
    public const string NAME = "TheHuntIsOn";
    public static readonly string VERSION = Assembly.GetExecutingAssembly().GetName().Version.ToString();
}
