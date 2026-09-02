using System.Security.Principal;
using OfficeScrubber.Core.Detection;

namespace OfficeScrubber.Windows.Detection;

public sealed class PrivilegeDetector : IOfficeDetector
{
    public DetectionSource Source => DetectionSource.Privilege;

    public ValueTask<IReadOnlyList<DetectionFinding>> DetectAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        var elevated = principal.IsInRole(WindowsBuiltInRole.Administrator);
        IReadOnlyList<DetectionFinding> findings =
        [
            new(Source, elevated ? "Administrator" : "StandardUser",
                Metadata: new Dictionary<string, string> { ["Identity"] = identity.Name }),
        ];
        return ValueTask.FromResult(findings);
    }
}
