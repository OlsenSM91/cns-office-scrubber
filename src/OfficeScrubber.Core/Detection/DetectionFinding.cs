namespace OfficeScrubber.Core.Detection;

public sealed record DetectionFinding(
    DetectionSource Source,
    string Name,
    string? Version = null,
    string? Location = null,
    IReadOnlyDictionary<string, string>? Metadata = null);
