namespace OfficeScrubber.Diagnostics;

public interface IDiagnosticSink : IAsyncDisposable
{
    ValueTask WriteAsync(DiagnosticEvent diagnosticEvent, CancellationToken cancellationToken = default);
    ValueTask FlushAsync(CancellationToken cancellationToken = default);
}
