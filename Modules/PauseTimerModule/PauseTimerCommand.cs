using Silksong.TheHuntIsOn.SsmpAddon;
using Silksong.TheHuntIsOn.SsmpAddon.Packets;
using SSMP.Api.Command.Server;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Silksong.TheHuntIsOn.Modules.PauseTimerModule;

internal class PauseTimerCommand : IServerCommand
{
    private readonly HuntServerAddon serverAddon;

    public PauseTimerCommand(HuntServerAddon serverAddon)
    {
        this.serverAddon = serverAddon;

        serverAddon.OnPlayerConnected += player => serverAddon.SendToPlayer(player, state);
    }

    public string Trigger => "/pausetimer";

    public string[] Aliases => ["/pt"];

    public bool AuthorizedOnly => true;

    private readonly ServerPauseState state = new();

    internal void BroadcastMessage(string message) => serverAddon.BroadcastMessage(message);

    internal void Broadcast<T>(T packet) where T : IIdentifiedPacket<ClientPacketId>, new() => serverAddon.Broadcast(packet);

    private static readonly List<PauseTimerSubcommand> subcommands =
    [
        new PauseSubcommand(),
        new UnpauseSubcommand(),
        new CountdownSubcommand(),
        new ClearCountdownsSubcommand()
    ];

    private static string AllSubcommands() => string.Join("|", [.. subcommands.Select(s => s.Name()).OrderBy(s => s)]);

    private static bool TryGetSubcommand(string name, [MaybeNullWhen(false)] out PauseTimerSubcommand subcommand)
    {
        foreach (var item in subcommands)
        {
            if (item.Name() == name || item.Aliases().Any(a => a == name))
            {
                subcommand = item;
                return true;
            }
        }

        subcommand = null;
        return false;
    }

    internal static bool MinArguments(ICommandSender commandSender, string[] arguments, int min)
    {
        if (arguments.Length >= min) return true;

        commandSender.SendMessage("Missing arguments.");
        return false;
    }

    internal static bool MaxArguments(ICommandSender commandSender, string[] arguments, int max)
    {
        if (arguments.Length <= max) return true;

        commandSender.SendMessage("Too many arguments.");
        return false;
    }

    internal static bool ParseInt(ICommandSender commandSender, string arg, out int value)
    {
        if (int.TryParse(arg, out value) && value >= 0) return true;

        commandSender.SendMessage($"Invalid integer '{arg}'");
        return false;
    }

    internal bool ServerPaused => state.ServerPaused;

    internal bool IsServerPaused(out float? remaining) => state.IsServerPaused(out remaining);

    private void UpdateCountdowns(DateTime now, Func<Countdown, Countdown> map)
    {
        state.Countdowns = [.. state.Countdowns.Select(map)];
        state.UpdateCountdowns(now);
        Broadcast(state);
    }

    internal void UpdatePauseState(bool paused, long unpauseTimeTicks)
    {
        state.ServerPaused = paused;
        state.UnpauseTimeTicks = unpauseTimeTicks;
        Broadcast(state);
    }

    internal bool AddCountdown(DateTime now, Countdown countdown)
    {
        state.UpdateCountdowns(now);
        if (state.Countdowns.Count >= ServerPauseState.MAX_COUNTDOWNS) return false;

        state.Countdowns.Add(countdown);
        Broadcast(state);
        return true;
    }

    internal void ClearCountdowns()
    {
        state.Countdowns.Clear();
        Broadcast(state);
    }

    internal void PauseCountdowns(DateTime now) => UpdateCountdowns(now, c => c.Pause(now));

    internal void UnpauseCountdowns(DateTime now, DateTime unpauseWhen) => UpdateCountdowns(now, c => c.UnpauseAt(now, unpauseWhen));

    public void Execute(ICommandSender commandSender, string[] arguments)
    {
        if (arguments.Length <= 1)
        {
            commandSender.SendMessage($"Usage: '/pt <{AllSubcommands()}>'");
            commandSender.SendMessage("Use '/pt help <command>' for details");
            return;
        }

        PauseTimerSubcommand? subcommand;
        string name = arguments[1].ToLower();
        if (name == "help")
        {
            if (arguments.Length == 2) commandSender.SendMessage($"Usage: '/pt help <{AllSubcommands()}>'");
            else if (TryGetSubcommand(arguments[2].ToLower(), out subcommand))
            {
                commandSender.SendMessage($"Usage: {subcommand.Usage()}");

                List<string> aliases = [.. subcommand.Aliases()];
                if (aliases.Count > 0)
                {
                    aliases.Sort();
                    commandSender.SendMessage($"Aliases: {string.Join("|", aliases)}");
                }
            }
            else
            {
                commandSender.SendMessage($"Unrecognized command '{name}'.");
                commandSender.SendMessage($"Usage: '/pt help <{AllSubcommands()}>'");
            }
            return;
        }

        if (!TryGetSubcommand(name, out subcommand))
        {
            commandSender.SendMessage($"Unrecognized command '{name}'.");
            commandSender.SendMessage($"Usage: '/pt <{AllSubcommands()}>'");
            return;
        }

        if (!subcommand.Execute(this, commandSender, [.. arguments.Skip(2)])) commandSender.SendMessage($"Usage: {subcommand.Usage()}");
    }
}

internal abstract class PauseTimerSubcommand
{
    public abstract string Name();

    public virtual IEnumerable<string> Aliases() => [];

    public abstract string Usage();

    public abstract bool Execute(PauseTimerCommand parent, ICommandSender send, string[] arguments);
}

internal class PauseSubcommand : PauseTimerSubcommand
{
    public override string Name() => "pause";

    public override string Usage() => "'/pt pause [X]': Pause the game for all players. If X is specified, unpause after X seconds.";

    public override bool Execute(PauseTimerCommand parent, ICommandSender commandSender, string[] arguments)
    {
        if (!PauseTimerCommand.MaxArguments(commandSender, arguments, 1)) return false;

        bool pause = true;
        long unpauseTimeTicks = long.MaxValue;
        var now = DateTime.UtcNow;
        int seconds = 0;
        if (arguments.Length == 1)
        {
            if (!PauseTimerCommand.ParseInt(commandSender, arguments[0], out seconds)) return false;

            var unpauseAt = now.AddSeconds(seconds);
            unpauseTimeTicks = unpauseAt.Ticks;
            parent.PauseCountdowns(now);
            parent.UnpauseCountdowns(now, unpauseAt);
        }
        else parent.PauseCountdowns(now);

        parent.UpdatePauseState(pause, unpauseTimeTicks);
        commandSender.SendMessage(seconds == 0 ? "Paused server." : $"Paused server for {seconds} seconds.");
        parent.BroadcastMessage(seconds == 0 ? "Server paused." : $"Server paused for {seconds} seconds.");
        return true;
    }
}

internal class UnpauseSubcommand : PauseTimerSubcommand
{
    public override string Name() => "unpause";

    public override string Usage() => "'/pt unpause [X]': Unpause the game for all players. If X is specified, unpause after X seconds.";

    public override bool Execute(PauseTimerCommand parent, ICommandSender commandSender, string[] arguments)
    {
        if (!PauseTimerCommand.MaxArguments(commandSender, arguments, 1)) return false;

        if (!parent.ServerPaused)
        {
            commandSender.SendMessage("Server is already unpaused.");
            return true;
        }

        bool pause;
        long unpauseTimeTicks;
        var now = DateTime.UtcNow;
        int seconds = 0;
        if (arguments.Length == 1)
        {
            if (!PauseTimerCommand.ParseInt(commandSender, arguments[0], out seconds)) return false;

            var unpauseAt = now.AddSeconds(seconds);
            pause = true;
            unpauseTimeTicks = unpauseAt.Ticks;
            parent.UnpauseCountdowns(now, unpauseAt);
        }
        else
        {
            pause = false;
            unpauseTimeTicks = 0;
            parent.UnpauseCountdowns(now, now);
        }

        parent.UpdatePauseState(pause, unpauseTimeTicks);
        commandSender.SendMessage(seconds == 0 ? "Unpaused server." : $"Scheduled unpause in {seconds} seconds.");
        parent.BroadcastMessage(seconds == 0 ? "Server unpaused." : $"Server unpausing in {seconds} seconds.");
        return true;
    }
}

internal class CountdownSubcommand : PauseTimerSubcommand
{
    public override string Name() => "countdown";

    public override string Usage() => "'/pt countdown X [msg...]': Start a countdown for all players on the server lasting X seconds, with an optional message attached.";

    public override bool Execute(PauseTimerCommand parent, ICommandSender commandSender, string[] arguments)
    {
        if (!PauseTimerCommand.MinArguments(commandSender, arguments, 1)) return false;

        if (!PauseTimerCommand.ParseInt(commandSender, arguments[0], out var seconds)) return false;

        var now = DateTime.UtcNow;
        Countdown countdown = new() { FinishTimeTicks = now.AddSeconds(seconds).Ticks };

        // Respect any active pauses or timed unpauses.
        if (parent.IsServerPaused(out var remaining))
        {
            if (remaining.HasValue) countdown = countdown.UnpauseAt(now, now.AddSeconds(remaining.Value));
            else countdown = countdown.Pause(now);
        }

        if (arguments.Length > 1)
        {
            countdown.Message = string.Join(" ", arguments.Skip(1));
            if (countdown.Message.Length > ServerPauseState.MAX_MESSAGE_LENGTH)
            {
                commandSender.SendMessage("Countdown message is too long.");
                return false;
            }
        }

        if (!parent.AddCountdown(now, countdown))
        {
            commandSender.SendMessage("Too many countdowns. Try '/pt clearcountdowns'.");
            return true;
        }

        commandSender.SendMessage($"Broadcasted new {seconds} second countdown.");
        return true;
    }
}

internal class ClearCountdownsSubcommand : PauseTimerSubcommand
{
    public override string Name() => "clearcountdowns";

    public override string Usage() => "'/pt clearcountdowns': clear all outstanding countdowns";

    public override bool Execute(PauseTimerCommand parent, ICommandSender commandSender, string[] arguments)
    {
        if (!PauseTimerCommand.MaxArguments(commandSender, arguments, 0)) return false;

        parent.ClearCountdowns();
        commandSender.SendMessage("Cleared all active countdowns.");
        return true;
    }
}
