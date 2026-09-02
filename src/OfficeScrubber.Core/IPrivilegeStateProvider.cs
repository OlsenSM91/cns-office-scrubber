namespace OfficeScrubber.Core;

/// <summary>Reads the privilege state of the current process without changing it.</summary>
public interface IPrivilegeStateProvider
{
    PrivilegeState GetCurrent();
}

public enum PrivilegeState
{
    StandardUser,
    Elevated,
    Unavailable
}
