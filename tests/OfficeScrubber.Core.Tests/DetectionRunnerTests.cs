using OfficeScrubber.Core.Detection;

namespace OfficeScrubber.Core.Tests;

public sealed class DetectionRunnerTests
{
    [Fact]
    public async Task RunAsync_CollectsDetectorFindings()
    {
        var finding = new DetectionFinding(DetectionSource.Registry, "Microsoft Office");
        var report = await new DetectionRunner([new StubDetector(finding)])
            .RunAsync(TestContext.Current.CancellationToken);

        Assert.Equal([finding], report.Findings);
        Assert.True(report.CompletedAt >= report.StartedAt);
    }

    private sealed class StubDetector(DetectionFinding finding) : IOfficeDetector
    {
        public DetectionSource Source => finding.Source;

        public ValueTask<IReadOnlyList<DetectionFinding>> DetectAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<IReadOnlyList<DetectionFinding>>([finding]);
    }
}
