using System.Reflection;
using OfficeScrubber.Cli;

var parsed = CommandLineParser.Parse(args);
if (!parsed.IsSuccess)
{
    Console.Error.WriteLine($"Error: {parsed.Error}");
    return ExitCodes.UsageError;
}

var options = parsed.Value!;
if (options.Command == CommandKind.Help)
{
    Console.WriteLine("""
        Usage: office-scrubber analyze [options]
               office-scrubber --help
               office-scrubber --version

        Commands:
          analyze          Report the current privilege state (read-only)

        Options:
          --verbose        Include additional operational detail
          --debug          Include diagnostic detail
          --json           Emit machine-readable JSON
          --log <directory> Configure a log directory (no log is written by analyze)
          --help, -h       Show this help
          --version        Show the version
        """);
    return ExitCodes.Success;
}

if (options.Command == CommandKind.Version)
{
    var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "unknown";
    Console.WriteLine(version);
    return ExitCodes.Success;
}

using var cancellation = new CancellationTokenSource();
ConsoleCancelEventHandler cancelHandler = (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellation.Cancel();
};
Console.CancelKeyPress += cancelHandler;

try
{
    var command = new AnalyzeCommand(new CurrentProcessPrivilegeStateProvider(), Console.Out);
    return await command.ExecuteAsync(options, cancellation.Token);
}
catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
{
    Console.Error.WriteLine("Analysis cancelled.");
    return ExitCodes.Cancelled;
}
catch (Exception exception)
{
    Console.Error.WriteLine(options.Debug ? exception.ToString() : $"Analysis failed: {exception.Message}");
    return ExitCodes.Failure;
}
finally
{
    Console.CancelKeyPress -= cancelHandler;
}
