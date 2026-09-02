namespace OfficeScrubber.Diagnostics;

/// <summary>Indicates that the requested complete diagnostic log could not be used.</summary>
public sealed class DiagnosticLogException : IOException
{
    public DiagnosticLogException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
