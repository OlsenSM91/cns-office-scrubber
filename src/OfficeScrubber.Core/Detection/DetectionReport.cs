namespace OfficeScrubber.Core.Detection;

public sealed record DetectionReport(DateTimeOffset StartedAt, DateTimeOffset CompletedAt, IReadOnlyList<DetectionFinding> Findings);
