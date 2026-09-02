using System.Text.Json;
using OfficeScrubber.Core.Diagnostics;

namespace OfficeScrubber.Diagnostics;

public sealed class JsonLinesDiagnosticLog(TextWriter writer, JsonSerializerOptions? options = null) : IDiagnosticLog
{
    private readonly JsonSerializerOptions _options = options ?? new JsonSerializerOptions(JsonSerializerDefaults.Web);

    public async ValueTask WriteAsync(DiagnosticEntry entry, CancellationToken cancellationToken = default)
    {
        var line = JsonSerializer.Serialize(entry, _options);
        await writer.WriteLineAsync(line.AsMemory(), cancellationToken).ConfigureAwait(false);
    }
}
