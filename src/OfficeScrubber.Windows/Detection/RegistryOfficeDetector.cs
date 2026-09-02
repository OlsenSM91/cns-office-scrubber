using Microsoft.Win32;
using OfficeScrubber.Core.Detection;

namespace OfficeScrubber.Windows.Detection;

public sealed class RegistryOfficeDetector : IOfficeDetector
{
    public DetectionSource Source => DetectionSource.Registry;

    public ValueTask<IReadOnlyList<DetectionFinding>> DetectAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var findings = new List<DetectionFinding>();
        foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
        {
            using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
            using var configuration = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Office\ClickToRun\Configuration");
            if (configuration?.GetValue("ProductReleaseIds") is string products)
            {
                findings.Add(new DetectionFinding(Source, products, configuration.GetValue("VersionToReport") as string,
                    configuration.GetValue("InstallationPath") as string,
                    new Dictionary<string, string> { ["RegistryView"] = view.ToString() }));
            }
        }

        return ValueTask.FromResult<IReadOnlyList<DetectionFinding>>(findings);
    }
}
