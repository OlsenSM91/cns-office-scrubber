using OfficeScrubber.Core.Diagnostics;

namespace OfficeScrubber.Diagnostics;

public sealed class TextDiagnosticLog(TextWriter writer) : IDiagnosticLog
{
    public async ValueTask WriteAsync(DiagnosticEntry entry, CancellationToken cancellationToken = default)
    {
        var line = $"{entry.Timestamp:O} [{entry.Level}] {entry.EventName}: {entry.Message}";
        await writer.WriteLineAsync(line.AsMemory(), cancellationToken).ConfigureAwait(false);
    }
}
