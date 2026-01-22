using SSMP.Api.Command.Server;
using System;
using System.Collections.Generic;

namespace Silksong.TheHuntIsOn.SsmpAddon;

internal abstract class Subcommand<T>
{
    public abstract string Name { get; }

    public virtual IEnumerable<string> Aliases => [];

    public abstract string Usage { get; }

    protected static bool MinArguments(ICommandSender commandSender, string[] arguments, int min)
    {
        if (arguments.Length >= min) return true;

        commandSender.SendMessage("Missing arguments.");
        return false;
    }

    protected static bool MaxArguments(ICommandSender commandSender, string[] arguments, int max)
    {
        if (arguments.Length <= max) return true;

        commandSender.SendMessage("Too many arguments.");
        return false;
    }

    protected static bool ParseInt(ICommandSender commandSender, string arg, out int value)
    {
        if (int.TryParse(arg, out value) && value >= 0) return true;

        commandSender.SendMessage($"Invalid integer '{arg}'");
        return false;
    }

    protected static string FormatDuration(TimeSpan span)
    {
        if (span.Seconds < 60) return $"{span.Seconds}s";
        else if (span.Seconds < 600)
        {
            int minutes = span.Minutes;
            int seconds = span.Seconds % 60;
            return seconds > 0 ? $"{minutes}m{seconds}s" : $"{minutes}m";
        }
        else if (span.Seconds < 3600) return $"{span.Minutes}m";
        else if (span.Seconds < 36000) return $"{span.Hours}h{span.Minutes % 60}m";
        else return $"{span.Hours}h";
    }

    public abstract bool Execute(T parent, ICommandSender commandSender, string[] arguments);
}
