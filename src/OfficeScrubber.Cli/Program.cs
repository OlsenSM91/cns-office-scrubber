using OfficeScrubber.Core.Detection;
using OfficeScrubber.Core.Diagnostics;
using OfficeScrubber.Diagnostics;
using OfficeScrubber.Windows.Detection;

if (!OperatingSystem.IsWindows())
{
    Console.Error.WriteLine("Office Scrubber detection requires Windows.");
    return 1;
}

var log = new TextDiagnosticLog(Console.Out);
await log.WriteAsync(new DiagnosticEntry(DateTimeOffset.UtcNow, DiagnosticLevel.Information, "DetectionStarted",
    "Starting read-only Office detection."));

var report = await new DetectionRunner(WindowsDetectors.CreateDefault()).RunAsync();
foreach (var finding in report.Findings)
{
    await log.WriteAsync(new DiagnosticEntry(DateTimeOffset.UtcNow, DiagnosticLevel.Information, "Finding",
        $"{finding.Source}: {finding.Name}"));
}

await log.WriteAsync(new DiagnosticEntry(DateTimeOffset.UtcNow, DiagnosticLevel.Information, "DetectionCompleted",
    $"Detection completed with {report.Findings.Count} finding(s)."));
return 0;
