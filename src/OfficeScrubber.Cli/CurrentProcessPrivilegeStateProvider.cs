using System.Security.Principal;
using OfficeScrubber.Core;

namespace OfficeScrubber.Cli;

internal sealed class CurrentProcessPrivilegeStateProvider : IPrivilegeStateProvider
{
    public PrivilegeState GetCurrent()
    {
        if (!OperatingSystem.IsWindows())
            return PrivilegeState.Unavailable;

        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator)
            ? PrivilegeState.Elevated
            : PrivilegeState.StandardUser;
    }
}
