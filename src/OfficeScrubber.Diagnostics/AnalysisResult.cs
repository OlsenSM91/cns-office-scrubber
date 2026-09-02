namespace OfficeScrubber.Diagnostics;

/// <summary>Stable top-level contract emitted on stdout by an analyzer using --json.</summary>
public sealed record AnalysisResult(
    bool Success,
    string Status,
    long ProcessedCount,
    long? TotalCount,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Errors);
