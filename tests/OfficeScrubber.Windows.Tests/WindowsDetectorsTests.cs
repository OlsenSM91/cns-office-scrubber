using OfficeScrubber.Core.Detection;
using OfficeScrubber.Windows.Detection;

namespace OfficeScrubber.Windows.Tests;

public sealed class WindowsDetectorsTests
{
    [Fact]
    public void CreateDefault_ProvidesEachWindowsDetectionSource()
    {
        var sources = WindowsDetectors.CreateDefault().Select(detector => detector.Source);

        Assert.Equal(
            [DetectionSource.Registry, DetectionSource.WindowsInstaller, DetectionSource.Service, DetectionSource.Process, DetectionSource.Privilege],
            sources);
    }
}
