using Microsoft.Win32;
using OfficeScrubber.Core;

namespace OfficeScrubber.Windows;

internal static class RegistryReader
{
    public static IEnumerable<(RegistryView View, RegistryKey Key)> OpenLocalMachine(string path, List<DetectionWarning> warnings, string detector)
    {
        foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
        {
            RegistryKey? key = null;
            try { key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view).OpenSubKey(path); }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or System.Security.SecurityException)
            { warnings.Add(new(detector, ex.Message, $"HKLM({view})\\{path}")); }
            if (key is not null) yield return (view, key);
        }
    }

    public static string? String(RegistryKey key, string name) { try { return key.GetValue(name)?.ToString(); } catch { return null; } }
    public static bool? Boolean(RegistryKey key, string name) => key.GetValue(name) switch { int i => i != 0, string s when int.TryParse(s, out var i) => i != 0, _ => null };
}

internal static class Result
{
    public static DetectorResult<T> MissingOrUnknown<T>(List<DetectionWarning> warnings) => warnings.Count == 0
        ? DetectorResult<T>.NotDetected()
        : DetectorResult<T>.Unknown([.. warnings]);
}
