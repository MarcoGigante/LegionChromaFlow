using System.Diagnostics;

namespace LegionChromaFlow;

internal static class Program
{
    internal const string MutexName = @"Local\LegionChromaFlow.Instance";
    internal const string StopEventName = @"Local\LegionChromaFlow.Stop";
    internal const string ShowEventName = @"Local\LegionChromaFlow.Show";

    private static int Main(string[] args)
    {
        var mode = args.Length > 0 ? args[0].TrimStart('-', '/').ToLowerInvariant() : "run";
        var paths = AppPaths.Resolve();
        Log.Init(paths.LogDir);

        if (mode == "selftest")
            return SelfTest.Run();

        if (!OperatingSystem.IsWindows())
        {
            Console.WriteLine("LegionChromaFlow only runs on Windows.");
            return 1;
        }

        try
        {
            return mode switch
            {
                "run" => Run(paths, test: false),
                "gui" => Gui.Run(paths, startHidden: args.Skip(1).Any(a => a.TrimStart('-', '/').Equals("tray", StringComparison.OrdinalIgnoreCase))),
                "test" => Run(paths, test: true),
                "probe" => Probe(paths),
                "boot" => Boot(paths),
                "stop" => Stop(),
                _ => Help()
            };
        }
        catch (Exception ex)
        {
            Log.Error($"Unhandled error: {ex}");
            return 1;
        }
    }

    private static int Help()
    {
        Console.WriteLine("""
            LegionChromaFlow - dynamic Legion keyboard lighting driven by your desktop wallpaper

              gui     opens the control panel with a tray icon (gui tray = start hidden)
              run     (default) starts the effect
              boot    pre-sign-in lighting (used by the optional boot task, see install-boot.bat)
              probe   diagnostics: shows keyboard, keys, profile, wallpaper and active window (does not change the lights)
              test    quick test: solid red, green, blue for a few seconds, then restores
              stop    stops the running instance
            """);
        return 0;
    }

    /// <summary>Pre-sign-in instance (runs as SYSTEM from the boot task): lights the keyboard until the panel takes over.</summary>
    private static int Boot(AppPaths paths)
    {
        Live.BootMode = true;
        Live.KeepOnExit = true;
        using var mutex = BootHandover.CreateInstanceMutex(out var created);
        if (!created)
        {
            Log.Warn("A boot instance is already running.");
            return 2;
        }

        using var stop = BootHandover.CreateStopEvent();
        Desktop.EnableDpiAwareness();
        var cfg = Config.Load(paths.ConfigPath);
        Log.Info("Boot mode: lighting before sign-in; the panel takes over at sign-in.");
        RunLoop(cfg, stop, test: false);
        Log.Info("Boot instance finished.");
        return 0;
    }

    private static int Stop()
    {
        try
        {
            using var ev = EventWaitHandle.OpenExisting(StopEventName);
            ev.Set();
            Log.Info("Stop signal sent.");
        }
        catch (WaitHandleCannotBeOpenedException)
        {
            Log.Info("No running instance.");
        }
        return 0;
    }

    // ------------------------------------------------------------------ Diagnostics

    private static int Probe(AppPaths paths)
    {
        Log.Info("=== PROBE ===");
        Log.Info($"Folder: {paths.Root}");
        var cfg = Config.Load(paths.ConfigPath);
        WarnIfVantageRunning();

        var wpPath = string.IsNullOrWhiteSpace(cfg.WallpaperOverride) ? Desktop.GetWallpaperPath() : cfg.WallpaperOverride;
        Log.Info($"Wallpaper: {wpPath ?? "(none)"}");
        if (wpPath is not null)
        {
            var f = Desktop.LoadImage(wpPath);
            Log.Info(f is null ? "Wallpaper NOT readable (a fallback rainbow will be used)." : $"Wallpaper read correctly ({f.W}x{f.H} samples).");
        }

        Desktop.EnableDpiAwareness();
        var win = Desktop.GetForeground();
        Log.Info($"Active window: class='{win.Class}' kind={win.Kind}");

        using var dev = SpectrumDevice.Open();
        if (dev is null)
        {
            Log.Error("Spectrum keyboard NOT found. Try starting the prompt as administrator and check the log.");
            return 2;
        }

        Log.Info($"Keyboard found: {dev.Description}");
        Log.Info($"Key map: {dev.Width} columns x {dev.Height} rows, {CountKeys(dev)} keys + {dev.ExtraKeyCodes.Length} extra lights");
        Log.Info($"Active profile: {dev.GetProfile()}");
        Log.Info("Probe complete. No lights were changed.");
        return 0;
    }

    private static int CountKeys(SpectrumDevice dev)
    {
        var n = 0;
        foreach (var k in dev.KeyCodes) if (k > 0) n++;
        return n;
    }

    private static void WarnIfVantageRunning()
    {
        var names = new[] { "LenovoVantage", "Lenovo.Vantage", "LenovoVantageService", "Lenovo.Modern.ImController", "LegionZone", "LenovoLegionToolkit", "Lenovo Legion Toolkit" };
        var running = names.Where(n => Process.GetProcessesByName(n).Length > 0).ToList();
        if (running.Count > 0)
            Log.Warn("Programs that can write to the keyboard lights and conflict are running: "
                     + string.Join(", ", running) + ". Close them if colors flicker or do not change.");
    }

    // ------------------------------------------------------------------ Execution

    private static int Run(AppPaths paths, bool test)
    {
        using var mutex = new Mutex(true, MutexName, out var created);
        if (!created)
        {
            Log.Warn("Another instance is already running. Use stop.bat to stop it.");
            return 2;
        }

        using var stop = new EventWaitHandle(false, EventResetMode.ManualReset, StopEventName);
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; stop.Set(); };

        Desktop.EnableDpiAwareness();
        try { Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal; } catch { }

        var cfg = Config.Load(paths.ConfigPath);
        Log.Info($"Start {(test ? "(test)" : "")}. Config: {paths.ConfigPath}");
        WarnIfVantageRunning();

        var code = RunLoop(cfg, stop, test);
        if (code != 0) return code;
        Log.Info("Finished.");
        return 0;
    }


    internal static int RunLoop(Config cfg, EventWaitHandle stop, bool test)
    {
        PowerWatch.Start();
        var announcedMissing = false;
        while (!stop.WaitOne(0))
        {
            SpectrumDevice? dev = null;
            try
            {
                dev = SpectrumDevice.Open();
            }
            catch (Exception ex)
            {
                Log.Warn($"Opening the keyboard failed: {ex.Message}");
            }

            if (dev is null)
            {
                if (!announcedMissing)
                {
                    Log.Warn("Spectrum keyboard not found, retrying every second (if never found, run as administrator).");
                    announcedMissing = true;
                }
                Live.SetStatus("waiting");
                if (test) return 2;
                stop.WaitOne(1000);
                continue;
            }

            announcedMissing = false;
            try
            {
                Log.Info($"Keyboard connected: {dev.Description}, {dev.Width}x{dev.Height}");
                Live.SetStatus("connected");
                if (test) RunTest(dev, cfg, stop);
                else RunSession(dev, cfg, stop);
            }
            catch (Exception ex)
            {
                Log.Warn($"Session interrupted: {ex.Message}");
            }
            finally
            {
                dev.Dispose();
            }

            if (test) break;
            if (!stop.WaitOne(2000))
                Log.Info("Reconnecting to the keyboard...");
        }
        return 0;
    }

    private static void RunTest(SpectrumDevice dev, Config cfg, EventWaitHandle stop)
    {
        var profile = cfg.Profile > 0 ? cfg.Profile : dev.GetProfile();
        var codes = new List<ushort>();
        foreach (var k in dev.KeyCodes) if (k > 0) codes.Add(k);
        codes.AddRange(dev.ExtraKeyCodes);

        if (!dev.AuroraStart(profile))
            throw new InvalidOperationException("The keyboard did not accept the start command.");

        try
        {
            var colors = new (byte r, byte g, byte b, string n)[] { (255, 0, 0, "rosso"), (0, 255, 0, "verde"), (0, 0, 255, "blu") };
            foreach (var c in colors)
            {
                Log.Info($"Test: all keys {c.n}");
                var buf = new byte[codes.Count * 3];
                for (var i = 0; i < codes.Count; i++) { buf[i * 3] = c.r; buf[i * 3 + 1] = c.g; buf[i * 3 + 2] = c.b; }
                var until = Stopwatch.StartNew();
                while (until.Elapsed.TotalSeconds < 2.5 && !stop.WaitOne(0))
                {
                    if (!dev.SendFrame(codes, buf, codes.Count))
                        throw new InvalidOperationException("Sending a frame failed.");
                    stop.WaitOne(60);
                }
            }
        }
        finally
        {
            dev.AuroraStop(profile);
            Log.Info("Test finished, lights restored from the profile.");
        }
    }

    private static void RunSession(SpectrumDevice dev, Config cfg, EventWaitHandle stop)
    {
        var profile = cfg.Profile > 0 ? cfg.Profile : dev.GetProfile();
        Log.Info($"Keyboard profile: {profile}");

        var wallSig = WallpaperSignature(cfg);
        var wall = LoadWallpaper(cfg) ?? Field.Rainbow();
        var engine = new FlowEngine(cfg, dev.KeyCodes, wall);
        var ai = new AiController(cfg, engine);

        var allCodes = new List<ushort>(engine.KeyCodes);
        allCodes.AddRange(dev.ExtraKeyCodes);
        var rgb = new byte[allCodes.Count * 3];
        var extraCount = dev.ExtraKeyCodes.Length;

        if (!dev.AuroraStart(profile))
            throw new InvalidOperationException("The keyboard did not accept the start command.");

        Log.Info($"Effect active: {engine.KeyCount} keys, {cfg.Fps} fps.");
        Live.SetStatus("active", engine.KeyCount);

        var sw = Stopwatch.StartNew();
        var nextFrame = 0.0;
        var nextWin = 0.0;
        double nextWall = cfg.WallpaperRecheckSeconds;

        long lastHandle = long.MinValue;
        var resumeQueue = new Queue<double>();
        var lastKind = Desktop.WindowKind.Desktop;
        var failures = 0;

        try
        {
            while (!stop.WaitOne(0))
            {
                var now = sw.Elapsed.TotalSeconds;

                // After sleep/resume or unlock the keyboard firmware may fall back to its own mode: re-arm Aurora a few times.
                if (Live.ConsumeResume()) { resumeQueue.Clear(); foreach (var delay in new[] { 0.0, 0.8, 2.5, 6.0 }) resumeQueue.Enqueue(now + delay); }
                while (resumeQueue.Count > 0 && now >= resumeQueue.Peek()) { resumeQueue.Dequeue(); dev.AuroraStart(profile); }

                if (now >= nextWin)
                {
                    nextWin = now + cfg.WindowSampleMs / 1000.0;
                    SampleWindow(engine, ai, cfg, now, ref lastHandle, ref lastKind, Live.ConsumeWave());
                }

                if (now >= nextWall)
                {
                    nextWall = now + cfg.WallpaperRecheckSeconds;
                    var sig = WallpaperSignature(cfg);
                    if (sig != wallSig)
                    {
                        wallSig = sig;
                        var f = LoadWallpaper(cfg);
                        if (f is not null)
                        {
                            engine.SetWallpaper(f);
                            Log.Info("Desktop wallpaper changed: updated.");
                        }
                    }
                }

                ai.Tick(now);
                engine.Render(now, rgb, out var er, out var eg, out var eb);
                for (var i = 0; i < extraCount; i++)
                {
                    var o = (engine.KeyCount + i) * 3;
                    rgb[o] = er; rgb[o + 1] = eg; rgb[o + 2] = eb;
                }

                Live.Publish(engine.NormX, engine.NormY, rgb, engine.KeyCount);
                if (dev.SendFrame(allCodes, rgb, allCodes.Count))
                    failures = 0;
                else if (++failures >= 15)
                    throw new IOException("Sending frames failed repeatedly (suspend/resume?).");

                nextFrame += 1.0 / cfg.Fps;
                var wait = nextFrame - sw.Elapsed.TotalSeconds;
                if (wait > 0) stop.WaitOne((int)(wait * 1000));
                else nextFrame = sw.Elapsed.TotalSeconds;
            }
        }
        finally
        {
            if (Live.KeepOnExit) Log.Info("Session ended, keyboard left in Aurora mode for the next instance.");
            else try { dev.AuroraStop(profile); Log.Info("Effect stopped, lights restored from the profile."); } catch { }
        }
    }

    private static void SampleWindow(FlowEngine engine, AiController ai, Config cfg, double now, ref long lastHandle, ref Desktop.WindowKind lastKind, bool force)
    {
        var info = Desktop.GetForeground();

        if (info.Kind == Desktop.WindowKind.Ignore)
        {
            // Taskbar, Start menu, etc.: the "real" window stays the previous one.
            if (lastKind == Desktop.WindowKind.Normal)
                engine.UpdateWindow(Desktop.CaptureWindow(lastHandle, cfg.ChromaThreshold, cfg.ValueThreshold), force, now);
            else if (force)
                engine.UpdateWindow(null, true, now);
            return;
        }

        var realChange = info.Handle != lastHandle || info.Kind != lastKind;
        if (realChange) ai.OnWindowChanged(info.Kind, info.Handle, now);
        var changed = force || realChange;
        Field? f = info.Kind == Desktop.WindowKind.Normal
            ? Desktop.CaptureWindow(info.Handle, cfg.ChromaThreshold, cfg.ValueThreshold)
            : null;

        engine.UpdateWindow(f, changed, now);
        lastHandle = info.Handle;
        lastKind = info.Kind;
    }

    private static string WallpaperSignature(Config cfg)
    {
        if (Live.BootMode)
        {
            var cache = BootHandover.WallpaperCachePath;
            try { return cache + "|" + File.GetLastWriteTimeUtc(cache).Ticks; } catch { return cache; }
        }
        var path = string.IsNullOrWhiteSpace(cfg.WallpaperOverride) ? Desktop.GetWallpaperPath() : cfg.WallpaperOverride;
        if (string.IsNullOrEmpty(path)) return "";
        try { return path + "|" + File.GetLastWriteTimeUtc(path).Ticks; } catch { return path; }
    }

    private static Field? LoadWallpaper(Config cfg)
    {
        if (Live.BootMode)
        {
            var cache = BootHandover.WallpaperCachePath;
            var cached = File.Exists(cache) ? Desktop.LoadImage(cache) : null;
            Log.Info(cached is null ? "No cached wallpaper yet: using the fallback rainbow." : "Wallpaper loaded from the boot cache.");
            return cached;
        }

        var path = string.IsNullOrWhiteSpace(cfg.WallpaperOverride) ? Desktop.GetWallpaperPath() : cfg.WallpaperOverride;
        if (string.IsNullOrEmpty(path))
        {
            Log.Warn("No wallpaper image set: using a fallback rainbow.");
            return null;
        }

        var f = Desktop.LoadImage(path);
        if (f is null)
            Log.Warn($"Cannot read the wallpaper '{path}': using a fallback rainbow.");
        else
            { Log.Info($"Wallpaper read: {path}"); Desktop.SaveWallpaperCache(path, BootHandover.WallpaperCachePath); }
        return f;
    }
}
