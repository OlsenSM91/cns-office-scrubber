using Microsoft.Win32;

namespace OfficeScrubber.Windows.Detection;

internal static class RegistryReader
{
    internal static IEnumerable<(string Name, string? Version, string? Location)> ReadNamedEntries(
        string path,
        Func<string, bool> predicate)
    {
        foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
        {
            using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
            using var parent = baseKey.OpenSubKey(path);
            if (parent is null)
            {
                continue;
            }

            foreach (var childName in parent.GetSubKeyNames())
            {
                using var child = parent.OpenSubKey(childName);
                var name = child?.GetValue("DisplayName") as string ?? childName;
                if (predicate(name))
                {
                    yield return (name, child?.GetValue("DisplayVersion") as string, child?.GetValue("InstallLocation") as string);
                }
            }
        }
    }
}
