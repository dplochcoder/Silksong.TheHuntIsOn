using SSMP.Api.Command.Server;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Silksong.TheHuntIsOn.SsmpAddon;

internal class SubcommandRegister<T>(string self, IEnumerable<Subcommand<T>> subcommands)
{
    private readonly string self = self;
    private readonly List<Subcommand<T>> subcommands = [.. subcommands];

    private string AllSubcommands() => string.Join("|", [.. subcommands.Select(s => s.Name).OrderBy(s => s)]);

    private bool TryGetSubcommand(string name, [MaybeNullWhen(false)] out Subcommand<T> subcommand)
    {
        foreach (var item in subcommands)
        {
            if (item.Name == name || item.Aliases.Contains(name))
            {
                subcommand = item;
                return true;
            }
        }

        subcommand = null;
        return false;
    }
    internal void Execute(T parent, ICommandSender commandSender, string[] arguments)
    {
        if (arguments.Length <= 1)
        {
            commandSender.SendMessage($"Usage: '{self} <{AllSubcommands()}>'");
            commandSender.SendMessage($"Use '{self} help <command>' for details");
            return;
        }

        Subcommand<T>? subcommand;
        string name = arguments[1].ToLower();
        if (name == "help")
        {
            if (arguments.Length == 2) commandSender.SendMessage($"Usage: '{self} help <{AllSubcommands()}>'");
            else if (TryGetSubcommand(arguments[2].ToLower(), out subcommand))
            {
                commandSender.SendMessage($"Usage: {subcommand.Usage}");

                List<string> aliases = [.. subcommand.Aliases];
                if (aliases.Count > 0)
                {
                    aliases.Sort();
                    commandSender.SendMessage($"Aliases: {string.Join("|", aliases)}");
                }
            }
            else
            {
                commandSender.SendMessage($"Unrecognized command '{name}'.");
                commandSender.SendMessage($"Usage: '{self} help <{AllSubcommands()}>'");
            }
            return;
        }

        if (!TryGetSubcommand(name, out subcommand))
        {
            commandSender.SendMessage($"Unrecognized command '{name}'.");
            commandSender.SendMessage($"Usage: '{self} <{AllSubcommands()}>'");
            return;
        }

        if (!subcommand.Execute(parent, commandSender, [.. arguments.Skip(2)])) commandSender.SendMessage($"Usage: {subcommand.Usage}");
    }
}
