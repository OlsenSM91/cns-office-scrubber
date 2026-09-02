using System.Collections.Immutable;

namespace OfficeScrubber.Core;

public interface IOfficeDetector<T> { string Name { get; } ValueTask<DetectorResult<T>> DetectAsync(CancellationToken cancellationToken = default); }
public interface IClickToRunDetector : IOfficeDetector<ClickToRunConfiguration> { }
public interface IMsiOfficeDetector : IOfficeDetector<ImmutableArray<MsiOfficeProduct>> { }
public interface IOfficeProcessDetector : IOfficeDetector<ImmutableArray<OfficeProcess>> { }
public interface IOfficeServiceDetector : IOfficeDetector<ImmutableArray<OfficeService>> { }
public interface IOfficeLicenseDetector : IOfficeDetector<ImmutableArray<OfficeLicense>> { }
public interface IVNextLicenseDetector : IOfficeDetector<VNextLicenseIndicators> { }
public interface IWindowsInstallerDetector : IOfficeDetector<WindowsInstallerState> { }
public interface IRebootStateDetector : IOfficeDetector<PendingRebootState> { }

public sealed class OfficeEnvironmentDetector(
    IClickToRunDetector clickToRun, IMsiOfficeDetector msi, IOfficeProcessDetector processes,
    IOfficeServiceDetector services, IOfficeLicenseDetector licenses, IVNextLicenseDetector vNext,
    IWindowsInstallerDetector installer, IRebootStateDetector reboot)
{
    public async ValueTask<OfficeEnvironment> DetectAsync(CancellationToken cancellationToken = default)
    {
        var c = Safe(clickToRun, cancellationToken); var m = Safe(msi, cancellationToken);
        var p = Safe(processes, cancellationToken); var s = Safe(services, cancellationToken);
        var l = Safe(licenses, cancellationToken); var v = Safe(vNext, cancellationToken);
        var w = Safe(installer, cancellationToken); var r = Safe(reboot, cancellationToken);
        await Task.WhenAll(c, m, p, s, l, v, w, r).ConfigureAwait(false);
        var cr = await c; var mr = await m; var pr = await p; var sr = await s;
        var lr = await l; var vr = await v; var wr = await w; var rr = await r;
        var installs = ImmutableArray.CreateBuilder<OfficeInstallation>();
        if (cr.Status == DetectionStatus.Detected && cr.Value is { } cv)
            installs.Add(new("ClickToRun", OfficeInstallationType.ClickToRun, ParseArchitecture(cv.Platform), ParseVersion(cv.ClientVersion), cv.UpdateChannel, cv.InstallPath, Ownership.MicrosoftOffice));
        if (mr.Status == DetectionStatus.Detected) foreach (var product in mr.Value)
            installs.Add(new(product.ProductCode, OfficeInstallationType.Msi, product.Architecture, ParseVersion(product.DisplayVersion), null, product.InstallLocation, product.Ownership));
        return new(installs.ToImmutable(), cr.Value, mr.Value.OrEmpty(), pr.Value.OrEmpty(), sr.Value.OrEmpty(), lr.Value.OrEmpty(), vr.Value, wr.Value, rr.Value,
            [Report(clickToRun, cr), Report(msi, mr), Report(processes, pr), Report(services, sr), Report(licenses, lr), Report(vNext, vr), Report(installer, wr), Report(reboot, rr)]);
    }
    private static async Task<DetectorResult<T>> Safe<T>(IOfficeDetector<T> detector, CancellationToken token)
    { try { return await detector.DetectAsync(token).ConfigureAwait(false); } catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; } catch (Exception ex) { return DetectorResult<T>.Failed(detector.Name, ex); } }
    private static DetectorReport Report<T>(IOfficeDetector<T> d, DetectorResult<T> r) => new(d.Name, r.Status, r.Warnings);
    private static Version? ParseVersion(string? value) => Version.TryParse(value, out var version) ? version : null;
    private static OfficeArchitecture ParseArchitecture(string? value) => value?.ToLowerInvariant() switch { "x86" => OfficeArchitecture.X86, "x64" => OfficeArchitecture.X64, "arm64" => OfficeArchitecture.Arm64, _ => OfficeArchitecture.Unknown };
}

internal static class ImmutableArrayExtensions
{
    public static ImmutableArray<T> OrEmpty<T>(this ImmutableArray<T> value) => value.IsDefault ? [] : value;
}
