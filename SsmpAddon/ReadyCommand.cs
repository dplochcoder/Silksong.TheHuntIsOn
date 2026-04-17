using Silksong.TheHuntIsOn.Util;
using SSMP.Api.Command.Client;

namespace Silksong.TheHuntIsOn.SsmpAddon;

internal class ReadyCommand : IClientCommand
{
    public string Trigger => "/ready";

    public string[] Aliases => ["/r"];

    public void Execute(string[] arguments)
    {
        if (!HuntClientAddon.IsConnected)
        {
            HuntClientAddon.Instance?.SendMessage("Not connected to a server.");
            return;
        }

        var role = TheHuntIsOnPlugin.GetRole();
        SaveTemplateLoader.LoadTemplateForRole(role);
        HuntClientAddon.Instance?.SendMessage($"Loading {role} save template...");

        HuntClientAddon.Instance?.Send(new ReadyUp());
        HuntClientAddon.Instance?.SendMessage("Sent ready signal to the server.");
    }
}
