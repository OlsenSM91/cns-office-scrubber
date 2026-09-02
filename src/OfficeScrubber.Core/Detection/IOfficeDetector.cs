namespace OfficeScrubber.Core.Detection;

public interface IOfficeDetector
{
    DetectionSource Source { get; }

    ValueTask<IReadOnlyList<DetectionFinding>> DetectAsync(CancellationToken cancellationToken = default);
}
