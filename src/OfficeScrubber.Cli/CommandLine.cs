namespace OfficeScrubber.Cli;

public enum CommandKind { None, Analyze, Help, Version }

public sealed record CommandLine(
    CommandKind Command,
    bool Verbose = false,
    bool Debug = false,
    bool Json = false,
    string? LogDirectory = null);

public sealed record ParseResult(CommandLine? Value, string? Error)
{
    public bool IsSuccess => Value is not null;
}

public static class CommandLineParser
{
    public static ParseResult Parse(IReadOnlyList<string> args)
    {
        if (args.Count == 0)
            return Fail("A command is required. Use '--help' to see the available commands.");

        var command = CommandKind.None;
        var verbose = false;
        var debug = false;
        var json = false;
        string? logDirectory = null;

        for (var index = 0; index < args.Count; index++)
        {
            var argument = args[index];
            switch (argument)
            {
                case "analyze":
                    if (command != CommandKind.None)
                        return Fail($"Unexpected command '{argument}'. Only one command may be specified.");
                    command = CommandKind.Analyze;
                    break;
                case "--help":
                case "-h":
                    if (args.Count != 1)
                        return Fail("The --help option cannot be combined with a command or other options.");
                    return Success(new(CommandKind.Help));
                case "--version":
                    if (args.Count != 1)
                        return Fail("The --version option cannot be combined with a command or other options.");
                    return Success(new(CommandKind.Version));
                case "--verbose":
                    if (verbose) return Duplicate(argument);
                    verbose = true;
                    break;
                case "--debug":
                    if (debug) return Duplicate(argument);
                    debug = true;
                    break;
                case "--json":
                    if (json) return Duplicate(argument);
                    json = true;
                    break;
                case "--log":
                    if (logDirectory is not null) return Duplicate(argument);
                    if (++index >= args.Count || string.IsNullOrWhiteSpace(args[index]) || args[index].StartsWith('-'))
                        return Fail("The --log option requires a directory argument.");
                    logDirectory = args[index];
                    break;
                default:
                    return argument.StartsWith('-')
                        ? Fail($"Unknown option '{argument}'. Use '--help' to see supported options.")
                        : Fail($"Unsupported command or argument '{argument}'. Use '--help' to see available commands.");
            }
        }

        if (command == CommandKind.None)
            return Fail("The options must be used with the 'analyze' command.");

        return Success(new(command, verbose, debug, json, logDirectory));
    }

    private static ParseResult Success(CommandLine value) => new(value, null);
    private static ParseResult Fail(string error) => new(null, error);
    private static ParseResult Duplicate(string option) => Fail($"Option '{option}' may only be specified once.");
}
