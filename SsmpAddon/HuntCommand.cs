using SSMP.Api.Command.Server;
using System;
using System.Collections.Generic;

namespace Silksong.TheHuntIsOn.SsmpAddon;

internal class HuntCommand(HuntServerAddon serverAddon) : IServerCommand
{
    public bool AuthorizedOnly => true;

    public string Trigger => "/thehuntison";

    public string[] Aliases => ["/hunt"];

    private static readonly SubcommandRegister<HuntCommand> subcommands = new(
        "/hunt",
        [new ResetSubcommand(), new StatusSubcommand(), new UpdateArchitectCommand(), new UpdateEventsCommand()]);

    internal DateTime LastReset { get; private set; } = DateTime.UtcNow;

    internal event Action? OnGameReset;

    internal void StartNewSession()
    {
        LastReset = DateTime.UtcNow;
        OnGameReset?.Invoke();
    }

    internal void UpdateArchitectLevels() => serverAddon.UpdateArchitectLevels();

    internal void UpdateEvents() => serverAddon.UpdateEvents();

    internal void BroadcastMessage(string message) => serverAddon.BroadcastMessage(message);

    public void Execute(ICommandSender commandSender, string[] arguments) => subcommands.Execute(this, commandSender, arguments);
}

internal class ResetSubcommand : Subcommand<HuntCommand>
{
    public override string Name => "reset";

    public override IEnumerable<string> Aliases => ["start", "newgame"];

    public override string Usage => "'/hunt reset': Reset hunter power ups and start a new session.";

    public override bool Execute(HuntCommand parent, ICommandSender commandSender, string[] arguments)
    {
        if (!MaxArguments(commandSender, arguments, 0)) return false;

        parent.StartNewSession();
        parent.BroadcastMessage("Hunt session reset.");
        return true;
    }
}

internal class StatusSubcommand : Subcommand<HuntCommand>
{
    public override string Name => "status";

    public override IEnumerable<string> Aliases => ["state"];

    public override string Usage => "'/hunt status': Query the status of the current session.";

    public override bool Execute(HuntCommand parent, ICommandSender commandSender, string[] arguments)
    {
        if (!MaxArguments(commandSender, arguments, 0)) return false;

        commandSender.SendMessage($"Current game started {FormatDuration(DateTime.UtcNow - parent.LastReset)} ago.");
        return true;
    }
}

internal class UpdateArchitectCommand : Subcommand<HuntCommand>
{
    public override string Name => "update-architect";

    public override IEnumerable<string> Aliases => ["update-levels"];

    public override string Usage => "'/hunt update-architect': Reload Architect-Server levels and notify clients.";

    public override bool Execute(HuntCommand parent, ICommandSender commandSender, string[] arguments)
    {
        if (!MaxArguments(commandSender, arguments, 0)) return false;

        parent.UpdateArchitectLevels();
        commandSender.SendMessage("Reloaded Architect levels.");
        return true;
    }
}

internal class UpdateEventsCommand : Subcommand<HuntCommand>
{
    public override string Name => "update-events";

    public override string Usage => "'/hunt update-events': Reload events.json on the server and notify clients.";

    public override bool Execute(HuntCommand parent, ICommandSender commandSender, string[] arguments)
    {
        if (!MaxArguments(commandSender, arguments, 0)) return false;

        parent.UpdateEvents();
        commandSender.SendMessage("Reloaded events.json");
        return true;
    }
}