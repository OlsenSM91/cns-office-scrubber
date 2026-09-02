namespace OfficeScrubber.Core.Detection;

public sealed class DetectionRunner(IEnumerable<IOfficeDetector> detectors)
{
    private readonly IReadOnlyList<IOfficeDetector> _detectors = detectors.ToArray();

    public async ValueTask<DetectionReport> RunAsync(CancellationToken cancellationToken = default)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var findings = new List<DetectionFinding>();

        foreach (var detector in _detectors)
        {
            cancellationToken.ThrowIfCancellationRequested();
            findings.AddRange(await detector.DetectAsync(cancellationToken).ConfigureAwait(false));
        }

        return new DetectionReport(startedAt, DateTimeOffset.UtcNow, findings);
    }
}
