using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.Win32;
using OfficeScrubber.Core;

namespace OfficeScrubber.Windows;

public sealed class OfficeProcessDetector : IOfficeProcessDetector
{
    private static readonly HashSet<string> Names = new(StringComparer.OrdinalIgnoreCase) { "winword", "excel", "powerpnt", "outlook", "onenote", "msaccess", "mspub", "visio", "officeclicktorun" };
    public string Name => nameof(OfficeProcessDetector);
    public ValueTask<DetectorResult<ImmutableArray<OfficeProcess>>> DetectAsync(CancellationToken cancellationToken = default)
    {
        var found = ImmutableArray.CreateBuilder<OfficeProcess>(); var warnings = new List<DetectionWarning>();
        Process[] all; try { all = Process.GetProcesses(); } catch (Exception ex) { return ValueTask.FromResult(DetectorResult<ImmutableArray<OfficeProcess>>.Failed(Name, ex)); }
        foreach (var process in all) using (process)
        {
            cancellationToken.ThrowIfCancellationRequested(); string name;
            try { name = process.ProcessName; } catch { continue; }
            if (!Names.Contains(name)) continue;
            string? path = null; try { path = process.MainModule?.FileName; } catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception or NotSupportedException) { warnings.Add(new(Name, ex.Message, name)); }
            var ownership = path?.Contains("Microsoft Office", StringComparison.OrdinalIgnoreCase) == true || name.Equals("officeclicktorun", StringComparison.OrdinalIgnoreCase) ? Ownership.MicrosoftOffice : Ownership.Unknown;
            try { found.Add(new(process.Id, name, path, ownership)); } catch (InvalidOperationException) { }
        }
        var value = found.ToImmutable(); return ValueTask.FromResult(value.Length == 0 ? DetectorResult<ImmutableArray<OfficeProcess>>.NotDetected([.. warnings]) : DetectorResult<ImmutableArray<OfficeProcess>>.Detected(value, [.. warnings]));
    }
}

public sealed class OfficeServiceDetector : IOfficeServiceDetector
{
    public string Name => nameof(OfficeServiceDetector);
    public ValueTask<DetectorResult<ImmutableArray<OfficeService>>> DetectAsync(CancellationToken cancellationToken = default)
    {
        const string path = @"SYSTEM\CurrentControlSet\Services"; var warnings = new List<DetectionWarning>(); var found = ImmutableArray.CreateBuilder<OfficeService>();
        foreach (var (_, root) in RegistryReader.OpenLocalMachine(path, warnings, Name)) using (root)
        {
            string[] serviceNames;
            try { serviceNames = root.GetSubKeyNames(); }
            catch (Exception ex) { warnings.Add(new(Name, ex.Message, root.Name)); continue; }
            foreach (var serviceName in serviceNames)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!serviceName.Contains("Office", StringComparison.OrdinalIgnoreCase) && !serviceName.Equals("ClickToRunSvc", StringComparison.OrdinalIgnoreCase)) continue;
                try { using var key = root.OpenSubKey(serviceName); if (key is null) continue; var image = RegistryReader.String(key, "ImagePath"); found.Add(new(serviceName, RegistryReader.String(key, "DisplayName"), image, RegistryReader.String(key, "Start"), null, image?.Contains("Microsoft", StringComparison.OrdinalIgnoreCase) == true || serviceName.Equals("ClickToRunSvc", StringComparison.OrdinalIgnoreCase) ? Ownership.MicrosoftOffice : Ownership.Unknown)); }
                catch (Exception ex) { warnings.Add(new(Name, ex.Message, serviceName)); }
            }
            break; // SYSTEM isn't redirected; do not duplicate it.
        }
        var value = found.ToImmutable(); return ValueTask.FromResult(value.Length == 0 ? Result.MissingOrUnknown<ImmutableArray<OfficeService>>(warnings) : DetectorResult<ImmutableArray<OfficeService>>.Detected(value, [.. warnings]));
    }
}
