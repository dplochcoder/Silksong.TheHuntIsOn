using SSMP.Api.Command.Server;
using System;
using System.Collections.Generic;

namespace Silksong.TheHuntIsOn.SsmpAddon;

internal class HuntCommand : IServerCommand
{
    public bool AuthorizedOnly => true;

    public string Trigger => "/thehuntison";

    public string[] Aliases => ["/hunt"];

    private static readonly SubcommandRegister<HuntCommand> subcommands = new("/hunt", [new StatusSubcommand(), new StartSessionSubcommand(), new StopSessionSubcommand()]);

    private DateTime lastChange = DateTime.Now;
    private bool activeSession;

    internal bool IsActiveSession(out DateTime lastChange)
    {
        lastChange = this.lastChange;
        return activeSession;
    }

    internal static event Action? OnStartSession;

    internal void StartNewSession()
    {
        if (activeSession) return;

        lastChange = DateTime.Now;
        activeSession = true;
        OnStartSession?.Invoke();
    }

    internal static event Action? OnEndSession;

    internal void EndSession()
    {
        if (!activeSession) return;

        lastChange = DateTime.Now;
        activeSession = false;
        OnEndSession?.Invoke();
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

        bool active = parent.IsActiveSession(out var lastChange);
        var elapsed = DateTime.Now - lastChange;

        var term = active ? "started" : "ended";
        commandSender.SendMessage($"Session {term} {FormatDuration(elapsed)} ago.");
        return true;
    }
}

internal class StartSessionSubcommand : Subcommand<HuntCommand>
{
    public override string Name => "start";

    public override string Usage => "'/hunt start': Start a new session.";

    public override bool Execute(HuntCommand parent, ICommandSender commandSender, string[] arguments)
    {
        if (!MaxArguments(commandSender, arguments, 0)) return false;

        if (parent.IsActiveSession(out _))
        {
            commandSender.SendMessage("Game already in session.");
            return true;
        }

        parent.StartNewSession();
        return true;
    }
}

internal class StopSessionSubcommand : Subcommand<HuntCommand>
{
    public override string Name => "stop";

    public override IEnumerable<string> Aliases => ["end"];

    public override string Usage => "'/hunt stop': End the current session.";

    public override bool Execute(HuntCommand parent, ICommandSender commandSender, string[] arguments)
    {
        if (!MaxArguments(commandSender, arguments, 0)) return false;

        if (!parent.IsActiveSession(out _))
        {
            commandSender.SendMessage("There is no active session.");
            return true;
        }

        parent.EndSession();
        return true;
    }
}
