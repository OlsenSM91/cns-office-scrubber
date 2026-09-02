using OfficeScrubber.Core.Detection;

namespace OfficeScrubber.Windows.Detection;

public sealed class WindowsInstallerOfficeDetector : IOfficeDetector
{
    private const string UninstallPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";

    public DetectionSource Source => DetectionSource.WindowsInstaller;

    public ValueTask<IReadOnlyList<DetectionFinding>> DetectAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var findings = RegistryReader.ReadNamedEntries(UninstallPath,
                name => name.Contains("Microsoft Office", StringComparison.OrdinalIgnoreCase))
            .Select(entry => new DetectionFinding(Source, entry.Name, entry.Version, entry.Location))
            .ToArray();
        return ValueTask.FromResult<IReadOnlyList<DetectionFinding>>(findings);
    }
}
