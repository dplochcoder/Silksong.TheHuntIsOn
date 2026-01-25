using SSMP.Api.Command.Server;
using System;
using System.Collections.Generic;

namespace Silksong.TheHuntIsOn.SsmpAddon;

internal class HuntCommand : IServerCommand
{
    public bool AuthorizedOnly => true;

    public string Trigger => "/thehuntison";

    public string[] Aliases => ["/hunt"];

    private static readonly SubcommandRegister<HuntCommand> subcommands = new("/hunt", [new ResetSubcommand(), new StatusSubcommand()]);

    internal DateTime LastReset { get; private set; } = DateTime.UtcNow;

    internal event Action? OnGameReset;

    internal void StartNewSession()
    {
        LastReset = DateTime.UtcNow;
        OnGameReset?.Invoke();
    }

    public void Execute(ICommandSender commandSender, string[] arguments) => subcommands.Execute(this, commandSender, arguments);
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

internal class ResetSubcommand : Subcommand<HuntCommand>
{
    public override string Name => "reset";

    public override IEnumerable<string> Aliases => ["start", "newgame"];

    public override string Usage => "'/hunt reset': Reset hunter power ups and start a new session.";

    public override bool Execute(HuntCommand parent, ICommandSender commandSender, string[] arguments)
    {
        if (!MaxArguments(commandSender, arguments, 0)) return false;

        parent.StartNewSession();
        return true;
    }
}
