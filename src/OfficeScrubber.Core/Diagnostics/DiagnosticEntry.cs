namespace OfficeScrubber.Core.Diagnostics;

public sealed record DiagnosticEntry(DateTimeOffset Timestamp, DiagnosticLevel Level, string EventName, string Message);

public enum DiagnosticLevel
{
    Trace,
    Information,
    Warning,
    Error,
}
