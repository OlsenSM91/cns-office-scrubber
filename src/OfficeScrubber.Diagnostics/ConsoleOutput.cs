using System.Text.Json;

namespace OfficeScrubber.Diagnostics;

/// <summary>Keeps stdout a single machine-readable stream when JSON output is requested.</summary>
public sealed class ConsoleOutput
{
    private readonly TextWriter _standardOutput;
    private readonly TextWriter _standardError;

    public ConsoleOutput(bool json, TextWriter? standardOutput = null, TextWriter? standardError = null)
    {
        Json = json;
        _standardOutput = standardOutput ?? Console.Out;
        _standardError = standardError ?? Console.Error;
    }

    public bool Json { get; }

    public Task WriteBannerAsync(string message) => Json ? Task.CompletedTask : _standardOutput.WriteLineAsync(message);
    public Task WriteProgressAsync(string message) => Json ? _standardError.WriteLineAsync(message) : _standardOutput.WriteLineAsync(message);
    public Task WriteDiagnosticAsync(string message) => _standardError.WriteLineAsync(message);

    public Task WriteResultAsync<T>(T result)
    {
        if (Json)
            return _standardOutput.WriteLineAsync(JsonSerializer.Serialize(result, DiagnosticJson.Options));
        return _standardOutput.WriteLineAsync(result?.ToString() ?? string.Empty);
    }
}
