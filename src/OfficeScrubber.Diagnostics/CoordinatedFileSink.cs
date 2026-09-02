using System.Globalization;
using System.Text;
using System.Text.Json;

namespace OfficeScrubber.Diagnostics;

/// <summary>
/// Writes every event to a human-readable log and a matching JSONL log. Creation is
/// all-or-nothing so callers never continue with a partially initialized logger.
/// </summary>
public sealed class CoordinatedFileSink : IDiagnosticSink
{
    private readonly StreamWriter _text;
    private readonly StreamWriter _jsonl;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _faulted;
    private bool _disposed;

    private CoordinatedFileSink(StreamWriter text, StreamWriter jsonl, string textPath, string jsonlPath)
    {
        _text = text;
        _jsonl = jsonl;
        TextPath = textPath;
        JsonlPath = jsonlPath;
    }

    public string TextPath { get; }
    public string JsonlPath { get; }

    public static CoordinatedFileSink Create(string directory, DateTimeOffset? startedAt = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        var fullDirectory = Path.GetFullPath(directory);
        StreamWriter? text = null;
        string? textPath = null;
        string? jsonlPath = null;

        try
        {
            Directory.CreateDirectory(fullDirectory);
            var stamp = (startedAt ?? DateTimeOffset.Now).ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            var suffix = 0;
            FileStream? textStream = null;
            FileStream? jsonStream = null;
            while (textStream is null)
            {
                var discriminator = suffix == 0 ? string.Empty : $"-{suffix}";
                textPath = Path.Combine(fullDirectory, $"OfficeScrubber-{stamp}{discriminator}.log");
                jsonlPath = Path.Combine(fullDirectory, $"OfficeScrubber-{stamp}{discriminator}.jsonl");
                try
                {
                    textStream = new FileStream(textPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read,
                        4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
                    try
                    {
                        jsonStream = new FileStream(jsonlPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read,
                            4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
                    }
                    catch
                    {
                        textStream.Dispose();
                        textStream = null;
                        File.Delete(textPath!);
                        if (File.Exists(jsonlPath))
                        {
                            suffix++;
                            continue;
                        }
                        throw;
                    }
                }
                catch (IOException) when (File.Exists(textPath) || File.Exists(jsonlPath))
                {
                    suffix++;
                }
            }

            text = NewWriter(textStream);
            var jsonl = NewWriter(jsonStream!);
            return new CoordinatedFileSink(text, jsonl, textPath!, jsonlPath!);
        }
        catch (Exception exception) when (exception is not DiagnosticLogException)
        {
            text?.Dispose();
            TryDelete(textPath);
            TryDelete(jsonlPath);
            throw new DiagnosticLogException($"Could not initialize diagnostic logs in '{fullDirectory}'.", exception);
        }
    }

    public async ValueTask WriteAsync(DiagnosticEvent diagnosticEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(diagnosticEvent);
        var json = JsonSerializer.Serialize(diagnosticEvent, DiagnosticJson.Options);
        var text = FormatText(diagnosticEvent);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ThrowIfUnavailable();
            try
            {
                await _text.WriteLineAsync(text.AsMemory(), cancellationToken).ConfigureAwait(false);
                await _jsonl.WriteLineAsync(json.AsMemory(), cancellationToken).ConfigureAwait(false);
                // Flush each pair: cancellation or a handled failure cannot strand buffered events.
                await _text.FlushAsync(cancellationToken).ConfigureAwait(false);
                await _jsonl.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                _faulted = true;
                throw new DiagnosticLogException("Writing the coordinated diagnostic logs failed.", exception);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask FlushAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ThrowIfUnavailable(allowFaulted: true);
            await _text.FlushAsync(cancellationToken).ConfigureAwait(false);
            await _jsonl.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            _faulted = true;
            throw new DiagnosticLogException("Flushing the coordinated diagnostic logs failed.", exception);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_disposed) return;
            _disposed = true;
            Exception? first = null;
            try { await _text.DisposeAsync().ConfigureAwait(false); } catch (Exception ex) { first = ex; }
            try { await _jsonl.DisposeAsync().ConfigureAwait(false); } catch (Exception ex) { first ??= ex; }
            if (first is not null)
                throw new DiagnosticLogException("Closing the coordinated diagnostic logs failed.", first);
        }
        finally
        {
            _gate.Release();
        }
    }

    private void ThrowIfUnavailable(bool allowFaulted = false)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_faulted && !allowFaulted)
            throw new DiagnosticLogException("The diagnostic logger is unavailable after an earlier failure.", new IOException());
    }

    private static StreamWriter NewWriter(Stream stream) => new(stream, new UTF8Encoding(false), 4096, leaveOpen: false)
    {
        NewLine = "\n"
    };

    private static string FormatText(DiagnosticEvent value)
    {
        var location = value.Substage is null ? value.Stage : $"{value.Stage}/{value.Substage}";
        var progress = value.TotalCount is null ? value.ProcessedCount.ToString(CultureInfo.InvariantCulture)
            : $"{value.ProcessedCount}/{value.TotalCount}";
        var item = value.CurrentItem is null ? string.Empty : $" item=\"{value.CurrentItem.Replace("\"", "'", StringComparison.Ordinal)}\"";
        var warnings = value.Warnings.Count == 0 ? string.Empty : $" warnings={value.Warnings.Count}";
        var errors = value.Errors.Count == 0 ? string.Empty : $" errors={value.Errors.Count}";
        var pid = value.ProcessId is null ? string.Empty : $" pid={value.ProcessId}";
        return $"{value.Timestamp:O} [{value.Severity}] {location} {value.Status} elapsed={value.ElapsedTime:c} progress={progress}{item}{warnings}{errors}{pid}";
    }

    private static void TryDelete(string? path)
    {
        if (path is null) return;
        try { File.Delete(path); } catch { /* Preserve the initialization exception. */ }
    }
}
