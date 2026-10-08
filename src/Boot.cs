using System.Security.AccessControl;
using System.Security.Principal;
using Microsoft.Win32;

namespace LegionChromaFlow;

/// <summary>
/// Pre-sign-in lighting. An optional scheduled task (installed with install-boot.bat) runs "boot" mode as SYSTEM
/// when Windows starts, so the keyboard lights up at the lock screen. When the user's panel starts it asks the
/// boot instance to stop (leaving the keyboard in Aurora mode) and takes over without flicker.
/// </summary>
internal static class BootHandover
{
    public const string StopEventName = @"Global\LegionChromaFlow.BootStop";
    public const string MutexName = @"Global\LegionChromaFlow.Boot";

    /// <summary>Last wallpaper, downscaled, saved by the panel so the boot instance can use it before anyone signs in.</summary>
    public static string WallpaperCachePath => Path.Combine(AppPaths.Resolve().Root, "config", "boot-wallpaper.jpg");

    private static SecurityIdentifier Everyone => new(WellKnownSidType.WorldSid, null);

    public static EventWaitHandle CreateStopEvent()
    {
        var sec = new EventWaitHandleSecurity();
        sec.AddAccessRule(new EventWaitHandleAccessRule(Everyone, EventWaitHandleRights.Synchronize | EventWaitHandleRights.Modify, AccessControlType.Allow));
        return EventWaitHandleAcl.Create(false, EventResetMode.ManualReset, StopEventName, out _, sec);
    }

    public static Mutex CreateInstanceMutex(out bool created)
    {
        var sec = new MutexSecurity();
        sec.AddAccessRule(new MutexAccessRule(Everyone, MutexRights.Synchronize | MutexRights.Modify, AccessControlType.Allow));
        return MutexAcl.Create(true, MutexName, out created, sec);
    }

    /// <summary>Called by the panel at start: if a boot instance is running, ask it to stop and wait until it is gone.</summary>
    public static void StopBootInstance()
    {
        try
        {
            if (!EventWaitHandle.TryOpenExisting(StopEventName, out var ev)) return;
            using (ev) ev.Set();
            if (Mutex.TryOpenExisting(MutexName, out var m))
            {
                using (m)
                {
                    try { if (m.WaitOne(5000)) m.ReleaseMutex(); }
                    catch (AbandonedMutexException) { }
                }
            }
            Log.Info("Boot instance handed over.");
        }
        catch (Exception ex) { Log.Warn($"Boot handover: {ex.Message}"); }
    }
}

/// <summary>Re-arms the keyboard after sleep/resume and unlock, when the firmware may have gone back to its own mode.</summary>
internal static class PowerWatch
{
    private static bool _started;

    public static void Start()
    {
        if (_started) return;
        _started = true;
        try
        {
            SystemEvents.PowerModeChanged += (_, e) =>
            {
                if (e.Mode == PowerModes.Resume) { Log.Info("Resumed from sleep: re-arming the keyboard."); Live.RequestResume(); }
            };
            SystemEvents.SessionSwitch += (_, e) =>
            {
                if (e.Reason is SessionSwitchReason.SessionUnlock or SessionSwitchReason.SessionLogon
                    or SessionSwitchReason.ConsoleConnect or SessionSwitchReason.RemoteConnect)
                    Live.RequestResume();
            };
        }
        catch (Exception ex) { Log.Warn($"Power events unavailable: {ex.Message}"); }
    }
}
