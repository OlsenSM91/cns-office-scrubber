using System.Diagnostics;
using OfficeScrubber.Core.Detection;

namespace OfficeScrubber.Windows.Detection;

public sealed class ProcessOfficeDetector : IOfficeDetector
{
    private static readonly HashSet<string> OfficeProcesses = new(StringComparer.OrdinalIgnoreCase)
    {
        "excel", "msaccess", "officeclicktorun", "onenote", "outlook", "powerpnt", "winword",
    };

    public DetectionSource Source => DetectionSource.Process;

    public ValueTask<IReadOnlyList<DetectionFinding>> DetectAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var findings = Process.GetProcesses()
            .Where(process => OfficeProcesses.Contains(process.ProcessName))
            .Select(process => new DetectionFinding(Source, process.ProcessName,
                Metadata: new Dictionary<string, string> { ["ProcessId"] = process.Id.ToString(System.Globalization.CultureInfo.InvariantCulture) }))
            .ToArray();
        return ValueTask.FromResult<IReadOnlyList<DetectionFinding>>(findings);
    }
}
