using System.Collections.Immutable;
using Microsoft.Win32;
using OfficeScrubber.Core;

namespace OfficeScrubber.Windows;

public sealed class ClickToRunDetector : IClickToRunDetector
{
    public string Name => nameof(ClickToRunDetector);
    public ValueTask<DetectorResult<ClickToRunConfiguration>> DetectAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); var warnings = new List<DetectionWarning>();
        foreach (var (_, key) in RegistryReader.OpenLocalMachine(@"SOFTWARE\Microsoft\Office\ClickToRun\Configuration", warnings, Name)) using (key)
            return ValueTask.FromResult(DetectorResult<ClickToRunConfiguration>.Detected(new(
                RegistryReader.String(key, "ClientVersionToReport"), RegistryReader.String(key, "Platform"),
                RegistryReader.String(key, "CDNBaseUrl"), RegistryReader.String(key, "ProductReleaseIds"),
                RegistryReader.String(key, "InstallationPath"), RegistryReader.Boolean(key, "UpdatesEnabled")), [.. warnings]));
        return ValueTask.FromResult(Result.MissingOrUnknown<ClickToRunConfiguration>(warnings));
    }
}

public sealed class MsiOfficeDetector : IMsiOfficeDetector
{
    private const string Uninstall = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
    public string Name => nameof(MsiOfficeDetector);
    public ValueTask<DetectorResult<ImmutableArray<MsiOfficeProduct>>> DetectAsync(CancellationToken cancellationToken = default)
    {
        var warnings = new List<DetectionWarning>(); var products = new Dictionary<string, MsiOfficeProduct>(StringComparer.OrdinalIgnoreCase);
        foreach (var (view, root) in RegistryReader.OpenLocalMachine(Uninstall, warnings, Name)) using (root)
        {
            string[] names; try { names = root.GetSubKeyNames(); } catch (Exception ex) { warnings.Add(new(Name, ex.Message, root.Name)); continue; }
            foreach (var name in names)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    using var key = root.OpenSubKey(name); if (key is null) continue;
                    var display = RegistryReader.String(key, "DisplayName"); var publisher = RegistryReader.String(key, "Publisher");
                    if (display?.Contains("Microsoft Office", StringComparison.OrdinalIgnoreCase) != true && display?.Contains("Microsoft 365", StringComparison.OrdinalIgnoreCase) != true) continue;
                    var ownership = publisher?.Contains("Microsoft", StringComparison.OrdinalIgnoreCase) == true ? Ownership.MicrosoftOffice : Ownership.Unknown;
                    products[name] = new(name, display, RegistryReader.String(key, "DisplayVersion"), RegistryReader.String(key, "InstallLocation"), view == RegistryView.Registry32 ? OfficeArchitecture.X86 : OfficeArchitecture.X64, ownership);
                }
                catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or System.Security.SecurityException) { warnings.Add(new(Name, ex.Message, name)); }
            }
        }
        var value = products.Values.ToImmutableArray();
        return ValueTask.FromResult(value.Length > 0 ? DetectorResult<ImmutableArray<MsiOfficeProduct>>.Detected(value, [.. warnings]) : Result.MissingOrUnknown<ImmutableArray<MsiOfficeProduct>>(warnings));
    }
}
