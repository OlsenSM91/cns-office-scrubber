using System.Text.Json.Serialization;

namespace OfficeScrubber.Diagnostics;

/// <summary>A single, structured observation made while analyzing Office.</summary>
public sealed record DiagnosticEvent
{
    public required DateTimeOffset Timestamp { get; init; }
    public required DiagnosticSeverity Severity { get; init; }
    public required string Stage { get; init; }
    public string? Substage { get; init; }
    public required string Status { get; init; }
    public TimeSpan ElapsedTime { get; init; }
    public string? CurrentItem { get; init; }
    public long ProcessedCount { get; init; }
    public long? TotalCount { get; init; }
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
    public int? ProcessId { get; init; }
}

[JsonConverter(typeof(JsonStringEnumConverter<DiagnosticSeverity>))]
public enum DiagnosticSeverity
{
    Trace,
    Debug,
    Information,
    Warning,
    Error,
    Critical
}
