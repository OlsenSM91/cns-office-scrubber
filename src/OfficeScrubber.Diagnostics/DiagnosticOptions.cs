namespace OfficeScrubber.Diagnostics;

/// <summary>Logging-related command-line options shared by analyzer hosts.</summary>
public sealed record DiagnosticOptions(string? LogDirectory, bool Json)
{
    public static DiagnosticOptions Parse(IEnumerable<string> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        var values = arguments.ToArray();
        string? logDirectory = null;
        var json = false;

        for (var index = 0; index < values.Length; index++)
        {
            var argument = values[index];
            if (string.Equals(argument, "--json", StringComparison.OrdinalIgnoreCase))
            {
                json = true;
            }
            else if (string.Equals(argument, "--log", StringComparison.OrdinalIgnoreCase))
            {
                if (++index >= values.Length || string.IsNullOrWhiteSpace(values[index]))
                    throw new ArgumentException("--log requires a directory.", nameof(arguments));
                logDirectory = values[index];
            }
            else if (argument.StartsWith("--log=", StringComparison.OrdinalIgnoreCase))
            {
                logDirectory = argument[6..];
                if (string.IsNullOrWhiteSpace(logDirectory))
                    throw new ArgumentException("--log requires a directory.", nameof(arguments));
            }
        }

        return new DiagnosticOptions(logDirectory, json);
    }
}
