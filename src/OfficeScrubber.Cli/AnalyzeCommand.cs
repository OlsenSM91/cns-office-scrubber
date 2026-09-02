using System.Text.Json;
using OfficeScrubber.Core;

namespace OfficeScrubber.Cli;

/// <summary>
/// The read-only analyze operation. Its only system-facing dependency is a Core query abstraction.
/// </summary>
public sealed class AnalyzeCommand(IPrivilegeStateProvider privilegeStateProvider, TextWriter output)
{
    public Task<int> ExecuteAsync(CommandLine options, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var privilege = privilegeStateProvider.GetCurrent();
        cancellationToken.ThrowIfCancellationRequested();

        if (options.Json)
        {
            output.WriteLine(JsonSerializer.Serialize(new
            {
                command = "analyze",
                privilege = ToDisplayValue(privilege),
                elevated = privilege == PrivilegeState.Elevated
            }));
        }
        else
        {
            output.WriteLine($"Current privilege state: {ToDisplayValue(privilege)}");
            if (options.Verbose)
                output.WriteLine("Analysis mode is read-only; elevation will not be requested.");
            if (options.Debug)
                output.WriteLine($"Debug: log directory = {options.LogDirectory ?? "(not configured)"}");
        }

        return Task.FromResult(ExitCodes.Success);
    }

    private static string ToDisplayValue(PrivilegeState state) => state switch
    {
        PrivilegeState.StandardUser => "standard-user",
        PrivilegeState.Elevated => "elevated",
        _ => "unavailable"
    };
}
