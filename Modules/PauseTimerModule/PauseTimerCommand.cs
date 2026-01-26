using Silksong.TheHuntIsOn.SsmpAddon;
using Silksong.TheHuntIsOn.SsmpAddon.PacketUtil;
using SSMP.Api.Command.Server;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Silksong.TheHuntIsOn.Modules.PauseTimerModule;

internal class PauseTimerCommand : IServerCommand
{
    private readonly HuntServerAddon serverAddon;

    public PauseTimerCommand(HuntServerAddon serverAddon)
    {
        this.serverAddon = serverAddon;

        serverAddon.OnUpdatePlayer += player => serverAddon.SendToPlayer(player, state);
        serverAddon.OnGameReset += () =>
        {
            state.Clear();
            serverAddon.Broadcast(state);
        };
    }

    public string Trigger => "/pausetimer";

    public string[] Aliases => ["/pt"];

    public bool AuthorizedOnly => true;

    private readonly ServerPauseState state = new();

    internal void BroadcastMessage(string message) => serverAddon.BroadcastMessage(message);

    internal void Broadcast<T>(T packet) where T : IIdentifiedPacket<ClientPacketId>, new() => serverAddon.Broadcast(packet);

    private static readonly SubcommandRegister<PauseTimerCommand> subcommands = new("/pt", [
        new PauseSubcommand(),
        new UnpauseSubcommand(),
        new CountdownSubcommand(),
        new ClearCountdownsSubcommand()
    ]);

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

    public void Execute(ICommandSender commandSender, string[] arguments) => subcommands.Execute(this, commandSender, arguments);
}

internal class PauseSubcommand : Subcommand<PauseTimerCommand>
{
    public override string Name => "pause";

    public override string Usage => "'/pt pause [X]': Pause the game for all players. If X is specified, unpause after X seconds.";

    public override bool Execute(PauseTimerCommand parent, ICommandSender commandSender, string[] arguments)
    {
        if (!MaxArguments(commandSender, arguments, 1)) return false;

        bool pause = true;
        long unpauseTimeTicks = long.MaxValue;
        var now = DateTime.UtcNow;
        int seconds = 0;
        if (arguments.Length == 1)
        {
            if (!ParseInt(commandSender, arguments[0], out seconds)) return false;

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

internal class UnpauseSubcommand : Subcommand<PauseTimerCommand>
{
    public override string Name => "unpause";

    public override string Usage => "'/pt unpause [X]': Unpause the game for all players. If X is specified, unpause after X seconds.";

    public override bool Execute(PauseTimerCommand parent, ICommandSender commandSender, string[] arguments)
    {
        if (!MaxArguments(commandSender, arguments, 1)) return false;

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
            if (!ParseInt(commandSender, arguments[0], out seconds)) return false;

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

internal class CountdownSubcommand : Subcommand<PauseTimerCommand>
{
    public override string Name => "countdown";

    public override string Usage => "'/pt countdown X [msg...]': Start a countdown for all players on the server lasting X seconds, with an optional message attached.";

    public override bool Execute(PauseTimerCommand parent, ICommandSender commandSender, string[] arguments)
    {
        if (!MinArguments(commandSender, arguments, 1)) return false;
        if (!ParseInt(commandSender, arguments[0], out var seconds)) return false;

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
            if (countdown.Message.Length > Countdown.MAX_MESSAGE_LENGTH)
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

internal class ClearCountdownsSubcommand : Subcommand<PauseTimerCommand>
{
    public override string Name => "clearcountdowns";

    public override IEnumerable<string> Aliases => ["clear"];

    public override string Usage => "'/pt clearcountdowns': clear all outstanding countdowns";

    public override bool Execute(PauseTimerCommand parent, ICommandSender commandSender, string[] arguments)
    {
        if (!MaxArguments(commandSender, arguments, 0)) return false;

        parent.ClearCountdowns();
        commandSender.SendMessage("Cleared all active countdowns.");
        return true;
    }
}
