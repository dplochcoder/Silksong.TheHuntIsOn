using System;
using System.Collections.Generic;
using System.Linq;
using SSMP.Api.Command.Server;

namespace Silksong.TheHuntIsOn.SsmpAddon;

internal class HuntCommand(HuntServerAddon serverAddon) : IServerCommand
{
    public bool AuthorizedOnly => true;

    public string Trigger => "/thehuntison";

    public string[] Aliases => ["/hunt"];

    private static readonly SubcommandRegister<HuntCommand> subcommands = new(
        "/hunt",
        [
            new SetupSubcommand(),
            new CancelSubcommand(),
            new CountdownConfigSubcommand(),
            new StartRoundSubcommand(),
            new ResetSubcommand(),
            new StatusSubcommand(),
            new UpdateArchitectCommand(),
            new UpdateEventsCommand(),
        ]
    );

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

    internal bool IsPreparing => serverAddon.IsPreparing;

    internal bool AllPlayersReady => serverAddon.AllPlayersReady;

    internal int ReadyPlayerCount => serverAddon.ReadyPlayerCount;

    internal int TotalPlayerCount => serverAddon.TotalPlayerCount;

    internal void StartPreparing() => serverAddon.StartPreparing();

    internal void CancelPreparing() => serverAddon.CancelPreparing();

    internal void FinishRound() => serverAddon.FinishRound();

    internal int SpeedrunnerCountdown => serverAddon.SpeedrunnerCountdown;

    internal IReadOnlyList<int> HunterCountdowns => serverAddon.HunterCountdowns;

    internal void SetSpeedrunnerCountdown(int seconds) =>
        serverAddon.SetSpeedrunnerCountdown(seconds);

    internal void SetHunterCountdowns(List<int> seconds) =>
        serverAddon.SetHunterCountdowns(seconds);

    internal void ExecuteRoundStart() => serverAddon.ExecuteRoundStart();

    public void Execute(ICommandSender commandSender, string[] arguments) =>
        subcommands.Execute(this, commandSender, arguments);
}

internal class SetupSubcommand : Subcommand<HuntCommand>
{
    public override string Name => "setup";

    public override IEnumerable<string> Aliases => ["prepare", "prep"];

    public override string Usage =>
        "&6'/hunt setup [SR_CD H_CD ...]'&r: Set up a new round. Optionally set speedrunner and hunter countdowns. Players must &6'/ready'&r before starting.";

    public override bool Execute(
        HuntCommand parent,
        ICommandSender commandSender,
        string[] arguments
    )
    {
        if (parent.IsPreparing)
        {
            commandSender.SendMessage("A round is already being set up.");
            return false;
        }

        if (arguments.Length >= 1)
        {
            if (!ParseInt(commandSender, arguments[0], out var srSeconds))
                return false;
            parent.SetSpeedrunnerCountdown(srSeconds);

            List<int> hunterSeconds = [];
            for (int i = 1; i < arguments.Length; i++)
            {
                if (!ParseInt(commandSender, arguments[i], out var s))
                    return false;
                hunterSeconds.Add(s);
            }
            if (hunterSeconds.Count > 0)
                parent.SetHunterCountdowns(hunterSeconds);

            var values = string.Join(", ", hunterSeconds.Select(s => $"{s}s"));
            commandSender.SendMessage(
                $"Countdowns set: speedrunner {srSeconds}s"
                    + (hunterSeconds.Count > 0 ? $", hunters {values}" : "")
                    + "."
            );
        }

        parent.StartPreparing();
        parent.BroadcastMessage(
            "A new round is being set up! &lWarning: Your save data will be overwritten.&r Type &6'/ready'&r to opt in."
        );
        return true;
    }
}

internal class CancelSubcommand : Subcommand<HuntCommand>
{
    public override string Name => "cancel";

    public override IEnumerable<string> Aliases => [];

    public override string Usage => "&6'/hunt cancel'&r: Cancel round preparation.";

    public override bool Execute(
        HuntCommand parent,
        ICommandSender commandSender,
        string[] arguments
    )
    {
        if (!MaxArguments(commandSender, arguments, 0))
            return false;

        if (!parent.IsPreparing)
        {
            commandSender.SendMessage("No round is being set up.");
            return false;
        }

        parent.CancelPreparing();
        parent.BroadcastMessage("Round set up has been cancelled.");
        return true;
    }
}

internal class CountdownConfigSubcommand : Subcommand<HuntCommand>
{
    public override string Name => "countdown";

    public override IEnumerable<string> Aliases => ["cd"];

    public override string Usage =>
        "&6'/hunt countdown speedrunner X'&r: Set speedrunner countdown to X seconds. "
        + "&6'/hunt countdown hunter X [X ...]'&r: Set hunter countdown(s). One countdown per value.";

    public override bool Execute(
        HuntCommand parent,
        ICommandSender commandSender,
        string[] arguments
    )
    {
        if (!MinArguments(commandSender, arguments, 2))
            return false;

        string role = arguments[0].ToLowerInvariant();
        switch (role)
        {
            case "speedrunner":
            case "sr":
                if (!MaxArguments(commandSender, arguments, 2))
                    return false;
                if (!ParseInt(commandSender, arguments[1], out var srSeconds))
                    return false;

                parent.SetSpeedrunnerCountdown(srSeconds);
                commandSender.SendMessage($"Speedrunner countdown set to {srSeconds}s.");
                return true;

            case "hunter":
            case "h":
                List<int> hunterSeconds = [];
                for (int i = 1; i < arguments.Length; i++)
                {
                    if (!ParseInt(commandSender, arguments[i], out var s))
                        return false;
                    hunterSeconds.Add(s);
                }

                parent.SetHunterCountdowns(hunterSeconds);
                var values = string.Join(", ", hunterSeconds.Select(s => $"{s}s"));
                commandSender.SendMessage($"Hunter countdown(s) set to {values}.");
                return true;

            default:
                commandSender.SendMessage(
                    "Expected 'speedrunner' or 'hunter' as first argument."
                );
                return false;
        }
    }
}

internal class StartRoundSubcommand : Subcommand<HuntCommand>
{
    public override string Name => "startround";

    public override IEnumerable<string> Aliases => ["start", "newgame", "start-round"];

    public override string Usage =>
        "&6'/hunt startround'&r: Start a new round, resetting all hunter power ups.";

    public override bool Execute(
        HuntCommand parent,
        ICommandSender commandSender,
        string[] arguments
    )
    {
        if (!MaxArguments(commandSender, arguments, 0))
            return false;

        if (!parent.IsPreparing)
        {
            commandSender.SendMessage(
                "No round is being set up. Use &6'/hunt setup'&r first."
            );
            return false;
        }

        if (!parent.AllPlayersReady)
        {
            commandSender.SendMessage(
                $"Not all players are ready ({parent.ReadyPlayerCount}/{parent.TotalPlayerCount})."
            );
            return false;
        }

        parent.FinishRound();
        parent.StartNewSession();
        parent.ExecuteRoundStart();
        parent.BroadcastMessage("&lA new round has started!&r");
        return true;
    }
}

internal class ResetSubcommand : Subcommand<HuntCommand>
{
    public override string Name => "reset";

    public override IEnumerable<string> Aliases => [];

    public override string Usage =>
        "&6'/hunt reset'&r: Reset hunter power ups and start a new session.";

    public override bool Execute(
        HuntCommand parent,
        ICommandSender commandSender,
        string[] arguments
    )
    {
        if (!MaxArguments(commandSender, arguments, 0))
            return false;

        parent.StartNewSession();
        parent.BroadcastMessage("Hunt session reset.");
        return true;
    }
}

internal class StatusSubcommand : Subcommand<HuntCommand>
{
    public override string Name => "status";

    public override IEnumerable<string> Aliases => ["state"];

    public override string Usage => "&6'/hunt status'&r: Query the status of the current session.";

    public override bool Execute(
        HuntCommand parent,
        ICommandSender commandSender,
        string[] arguments
    )
    {
        if (!MaxArguments(commandSender, arguments, 0))
            return false;

        commandSender.SendMessage(
            $"Current game started {FormatDuration(DateTime.UtcNow - parent.LastReset)} ago."
        );

        if (parent.IsPreparing)
            commandSender.SendMessage(
                $"Round preparing: {parent.ReadyPlayerCount}/{parent.TotalPlayerCount} ready."
            );

        if (parent.SpeedrunnerCountdown > 0)
            commandSender.SendMessage(
                $"Speedrunner countdown: {parent.SpeedrunnerCountdown}s."
            );

        if (parent.HunterCountdowns.Count > 0)
        {
            var values = string.Join(", ", parent.HunterCountdowns.Select(s => $"{s}s"));
            commandSender.SendMessage($"Hunter countdown(s): {values}.");
        }

        return true;
    }
}

internal class UpdateArchitectCommand : Subcommand<HuntCommand>
{
    public override string Name => "update-architect";

    public override IEnumerable<string> Aliases => ["update-levels"];

    public override string Usage =>
        "&6'/hunt update-architect'&r: Reload Architect-Server levels and notify clients.";

    public override bool Execute(
        HuntCommand parent,
        ICommandSender commandSender,
        string[] arguments
    )
    {
        if (!MaxArguments(commandSender, arguments, 0))
            return false;

        parent.UpdateArchitectLevels();
        commandSender.SendMessage("Reloaded Architect levels.");
        return true;
    }
}

internal class UpdateEventsCommand : Subcommand<HuntCommand>
{
    public override string Name => "update-events";

    public override string Usage =>
        "&6'/hunt update-events'&r: Reload events.json on the server and notify clients.";

    public override bool Execute(
        HuntCommand parent,
        ICommandSender commandSender,
        string[] arguments
    )
    {
        if (!MaxArguments(commandSender, arguments, 0))
            return false;

        parent.UpdateEvents();
        commandSender.SendMessage("Reloaded events.json");
        return true;
    }
}
