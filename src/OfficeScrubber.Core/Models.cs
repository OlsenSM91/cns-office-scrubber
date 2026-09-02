using System.Collections.Immutable;

namespace OfficeScrubber.Core;

public enum DetectionStatus { Detected, NotDetected, Unknown, Failed }
public enum OfficeInstallationType { Unknown, ClickToRun, Msi, AppX }
public enum OfficeArchitecture { Unknown, X86, X64, Arm64 }
public enum Ownership { Unknown, MicrosoftOffice, ThirdParty }
public enum RemovalDisposition { Keep = 0, Remove = 1 }

public sealed record OfficeInstallation(
    string Id, OfficeInstallationType Type, OfficeArchitecture Architecture = OfficeArchitecture.Unknown,
    Version? Version = null, string? Channel = null, string? InstallPath = null,
    Ownership Ownership = Ownership.Unknown,
    RemovalDisposition RemovalDisposition = RemovalDisposition.Keep);

public sealed record ClickToRunConfiguration(
    string? ClientVersion, string? Platform, string? UpdateChannel, string? ProductReleaseIds,
    string? InstallPath, bool? UpdatesEnabled);

public sealed record MsiOfficeProduct(
    string ProductCode, string? DisplayName, string? DisplayVersion, string? InstallLocation,
    OfficeArchitecture Architecture, Ownership Ownership = Ownership.Unknown,
    RemovalDisposition RemovalDisposition = RemovalDisposition.Keep);

public sealed record OfficeProcess(int Id, string Name, string? ExecutablePath, Ownership Ownership);
public sealed record OfficeService(string Name, string? DisplayName, string? ImagePath, string? StartMode, string? State, Ownership Ownership);
public sealed record OfficeLicense(string Id, string? Name, string? Status, string? PartialProductKey);
public sealed record VNextLicenseIndicators(bool? HasTokens, bool? HasIdentities, ImmutableArray<string> Paths);
public sealed record WindowsInstallerState(bool? IsAvailable, string? Version);
public sealed record PendingRebootState(bool? IsPending, ImmutableArray<string> Reasons);
public sealed record DetectionWarning(string Detector, string Message, string? Resource = null);

public sealed record DetectorResult<T>(DetectionStatus Status, T? Value, ImmutableArray<DetectionWarning> Warnings)
{
    public static DetectorResult<T> Detected(T value, params DetectionWarning[] warnings) => new(DetectionStatus.Detected, value, [.. warnings]);
    public static DetectorResult<T> NotDetected(params DetectionWarning[] warnings) => new(DetectionStatus.NotDetected, default, [.. warnings]);
    public static DetectorResult<T> Unknown(params DetectionWarning[] warnings) => new(DetectionStatus.Unknown, default, [.. warnings]);
    public static DetectorResult<T> Failed(string detector, Exception error) => new(DetectionStatus.Failed, default, [new(detector, error.Message)]);
}

public sealed record DetectorReport(string Name, DetectionStatus Status, ImmutableArray<DetectionWarning> Warnings);

/// <summary>An immutable snapshot. Reports retain each detector's result independently.</summary>
public sealed record OfficeEnvironment(
    ImmutableArray<OfficeInstallation> Installations,
    ClickToRunConfiguration? ClickToRun,
    ImmutableArray<MsiOfficeProduct> MsiProducts,
    ImmutableArray<OfficeProcess> RunningProcesses,
    ImmutableArray<OfficeService> Services,
    ImmutableArray<OfficeLicense> Licenses,
    VNextLicenseIndicators? VNext,
    WindowsInstallerState? WindowsInstaller,
    PendingRebootState? PendingReboot,
    ImmutableArray<DetectorReport> DetectionReports)
{
    public ImmutableArray<DetectionWarning> Warnings => [.. DetectionReports.SelectMany(x => x.Warnings)];
    public bool IsIncomplete => DetectionReports.Any(x => x.Status is DetectionStatus.Unknown or DetectionStatus.Failed);
}
