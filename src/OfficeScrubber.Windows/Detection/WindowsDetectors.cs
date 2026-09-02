using OfficeScrubber.Core.Detection;

namespace OfficeScrubber.Windows.Detection;

public static class WindowsDetectors
{
    public static IReadOnlyList<IOfficeDetector> CreateDefault() =>
    [
        new RegistryOfficeDetector(),
        new WindowsInstallerOfficeDetector(),
        new ServiceOfficeDetector(),
        new ProcessOfficeDetector(),
        new PrivilegeDetector(),
    ];
}
