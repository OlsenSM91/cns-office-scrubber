namespace OfficeScrubber.Core.Diagnostics;

public interface IDiagnosticLog
{
    ValueTask WriteAsync(DiagnosticEntry entry, CancellationToken cancellationToken = default);
}
