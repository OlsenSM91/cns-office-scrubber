using System.Collections.Immutable;
using System.Diagnostics;
using OfficeScrubber.Core;

namespace OfficeScrubber.Windows;

public sealed class OfficeLicenseDetector : IOfficeLicenseDetector
{
    public string Name => nameof(OfficeLicenseDetector);
    public ValueTask<DetectorResult<ImmutableArray<OfficeLicense>>> DetectAsync(CancellationToken cancellationToken = default)
    {
        var warnings = new List<DetectionWarning>(); var found = new Dictionary<string, OfficeLicense>(StringComparer.OrdinalIgnoreCase);
        foreach (var version in new[] { "16.0", "15.0" })
        foreach (var (_, root) in RegistryReader.OpenLocalMachine($@"SOFTWARE\Microsoft\Office\{version}\Registration", warnings, Name)) using (root)
        {
            string[] ids;
            try { ids = root.GetSubKeyNames(); }
            catch (Exception ex) { warnings.Add(new(Name, ex.Message, root.Name)); continue; }
            foreach (var id in ids)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try { using var key = root.OpenSubKey(id); if (key is null) continue; found[id] = new(id, RegistryReader.String(key, "ProductName"), RegistryReader.String(key, "LicenseState"), RegistryReader.String(key, "DigitalProductID") is { Length: > 5 } digital ? digital[^5..] : null); }
                catch (Exception ex) { warnings.Add(new(Name, ex.Message, id)); }
            }
        }
        var result = found.Values.ToImmutableArray(); return ValueTask.FromResult(result.Length == 0 ? Result.MissingOrUnknown<ImmutableArray<OfficeLicense>>(warnings) : DetectorResult<ImmutableArray<OfficeLicense>>.Detected(result, [.. warnings]));
    }
}

public sealed class VNextLicenseDetector : IVNextLicenseDetector
{
    public string Name => nameof(VNextLicenseDetector);
    public ValueTask<DetectorResult<VNextLicenseIndicators>> DetectAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); var warnings = new List<DetectionWarning>(); var paths = ImmutableArray.CreateBuilder<string>(); bool? tokens = false, identities = false;
        Probe(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Office", "Licenses"), ref tokens);
        Probe(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "OneAuth"), ref identities);
        var value = new VNextLicenseIndicators(tokens, identities, paths.ToImmutable());
        return ValueTask.FromResult(tokens == true || identities == true ? DetectorResult<VNextLicenseIndicators>.Detected(value, [.. warnings]) : warnings.Count > 0 ? DetectorResult<VNextLicenseIndicators>.Unknown([.. warnings]) : DetectorResult<VNextLicenseIndicators>.NotDetected());
        void Probe(string path, ref bool? indicator) { try { if (Directory.Exists(path)) { indicator = true; paths.Add(path); } } catch (Exception ex) { indicator = null; warnings.Add(new(Name, ex.Message, path)); } }
    }
}

public sealed class WindowsInstallerDetector : IWindowsInstallerDetector
{
    public string Name => nameof(WindowsInstallerDetector);
    public ValueTask<DetectorResult<WindowsInstallerState>> DetectAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); var warnings = new List<DetectionWarning>();
        foreach (var (_, key) in RegistryReader.OpenLocalMachine(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Installer", warnings, Name)) using (key)
            return ValueTask.FromResult(DetectorResult<WindowsInstallerState>.Detected(new(true, RegistryReader.String(key, "InstallerLocation")), [.. warnings]));
        var systemMsi = Path.Combine(Environment.SystemDirectory, "msiexec.exe");
        try { if (File.Exists(systemMsi)) return ValueTask.FromResult(DetectorResult<WindowsInstallerState>.Detected(new(true, FileVersionInfo.GetVersionInfo(systemMsi).FileVersion), [.. warnings])); }
        catch (Exception ex) { warnings.Add(new(Name, ex.Message, systemMsi)); }
        return ValueTask.FromResult(warnings.Count > 0 ? DetectorResult<WindowsInstallerState>.Unknown([.. warnings]) : DetectorResult<WindowsInstallerState>.Detected(new(false, null)));
    }
}

public sealed class RebootStateDetector : IRebootStateDetector
{
    public string Name => nameof(RebootStateDetector);
    public ValueTask<DetectorResult<PendingRebootState>> DetectAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); var warnings = new List<DetectionWarning>(); var reasons = ImmutableArray.CreateBuilder<string>();
        Probe(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending", "Component Based Servicing");
        Probe(@"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired", "Windows Update");
        foreach (var (_, key) in RegistryReader.OpenLocalMachine(@"SYSTEM\CurrentControlSet\Control\Session Manager", warnings, Name)) using (key)
        { try { if (key.GetValue("PendingFileRenameOperations") is not null) reasons.Add("Pending file rename"); } catch (Exception ex) { warnings.Add(new(Name, ex.Message, key.Name)); } break; }
        var value = new PendingRebootState(reasons.Count > 0, reasons.ToImmutable());
        return ValueTask.FromResult(warnings.Count > 0 && reasons.Count == 0 ? DetectorResult<PendingRebootState>.Unknown([.. warnings]) : DetectorResult<PendingRebootState>.Detected(value, [.. warnings]));
        void Probe(string path, string reason) { var any = false; foreach (var (_, key) in RegistryReader.OpenLocalMachine(path, warnings, Name)) { key.Dispose(); any = true; break; } if (any) reasons.Add(reason); }
    }
}

public static class WindowsOfficeEnvironmentDetector
{
    public static OfficeEnvironmentDetector CreateDefault() => new(new ClickToRunDetector(), new MsiOfficeDetector(), new OfficeProcessDetector(), new OfficeServiceDetector(), new OfficeLicenseDetector(), new VNextLicenseDetector(), new WindowsInstallerDetector(), new RebootStateDetector());
}
