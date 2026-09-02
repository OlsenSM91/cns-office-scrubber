using Microsoft.Win32;
using OfficeScrubber.Core.Detection;

namespace OfficeScrubber.Windows.Detection;

public sealed class ServiceOfficeDetector : IOfficeDetector
{
    private static readonly string[] ServiceNames = ["ClickToRunSvc", "OfficeSvc", "ose"];

    public DetectionSource Source => DetectionSource.Service;

    public ValueTask<IReadOnlyList<DetectionFinding>> DetectAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var findings = new List<DetectionFinding>();
        using var services = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services");
        foreach (var serviceName in ServiceNames)
        {
            using var service = services?.OpenSubKey(serviceName);
            if (service is not null)
            {
                findings.Add(new DetectionFinding(Source, serviceName, Location: service.GetValue("ImagePath") as string));
            }
        }

        return ValueTask.FromResult<IReadOnlyList<DetectionFinding>>(findings);
    }
}
