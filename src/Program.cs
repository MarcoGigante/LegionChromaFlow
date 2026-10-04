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
            Console.WriteLine("LegionChromaFlow funziona solo su Windows.");
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
                "stop" => Stop(),
                _ => Help()
            };
        }
        catch (Exception ex)
        {
            Log.Error($"Errore non gestito: {ex}");
            return 1;
        }
    }

    private static int Help()
    {
        Console.WriteLine("""
            LegionChromaFlow - luci dinamiche per la tastiera Legion a partire dallo sfondo del desktop

              gui     apre il pannello con icona nell'area di notifica (gui tray = parte nascosto)
              run     (predefinito) avvia l'effetto
              probe   diagnostica: mostra tastiera, tasti, profilo, sfondo e finestra attiva (non cambia le luci)
              test    prova rapida: rosso, verde, blu a tinta unita per pochi secondi, poi ripristina
              stop    ferma l'istanza in esecuzione
            """);
        return 0;
    }

    private static int Stop()
    {
        try
        {
            using var ev = EventWaitHandle.OpenExisting(StopEventName);
            ev.Set();
            Log.Info("Segnale di stop inviato.");
        }
        catch (WaitHandleCannotBeOpenedException)
        {
            Log.Info("Nessuna istanza in esecuzione.");
        }
        return 0;
    }

    // ------------------------------------------------------------------ Diagnostica

    private static int Probe(AppPaths paths)
    {
        Log.Info("=== PROBE ===");
        Log.Info($"Cartella: {paths.Root}");
        var cfg = Config.Load(paths.ConfigPath);
        WarnIfVantageRunning();

        var wpPath = string.IsNullOrWhiteSpace(cfg.WallpaperOverride) ? Desktop.GetWallpaperPath() : cfg.WallpaperOverride;
        Log.Info($"Sfondo: {wpPath ?? "(nessuno)"}");
        if (wpPath is not null)
        {
            var f = Desktop.LoadImage(wpPath);
            Log.Info(f is null ? "Sfondo NON leggibile (verra' usato un arcobaleno di riserva)." : $"Sfondo letto correttamente ({f.W}x{f.H} campioni).");
        }

        Desktop.EnableDpiAwareness();
        var win = Desktop.GetForeground();
        Log.Info($"Finestra attiva: classe='{win.Class}' tipo={win.Kind}");

        using var dev = SpectrumDevice.Open();
        if (dev is null)
        {
            Log.Error("Tastiera Spectrum NON trovata. Prova ad avviare il prompt come amministratore e controlla il log.");
            return 2;
        }

        Log.Info($"Tastiera trovata: {dev.Description}");
        Log.Info($"Mappa tasti: {dev.Width} colonne x {dev.Height} righe, {CountKeys(dev)} tasti + {dev.ExtraKeyCodes.Length} luci extra");
        Log.Info($"Profilo attivo: {dev.GetProfile()}");
        Log.Info("Probe completato. Nessuna luce e' stata modificata.");
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
            Log.Warn("Sono in esecuzione programmi che possono scrivere sulle luci della tastiera e andare in conflitto: "
                     + string.Join(", ", running) + ". Chiudili se i colori sfarfallano o non cambiano.");
    }

    // ------------------------------------------------------------------ Esecuzione

    private static int Run(AppPaths paths, bool test)
    {
        using var mutex = new Mutex(true, MutexName, out var created);
        if (!created)
        {
            Log.Warn("Un'altra istanza e' gia' in esecuzione. Usa stop.bat per fermarla.");
            return 2;
        }

        using var stop = new EventWaitHandle(false, EventResetMode.ManualReset, StopEventName);
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; stop.Set(); };

        Desktop.EnableDpiAwareness();
        try { Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal; } catch { }

        var cfg = Config.Load(paths.ConfigPath);
        Log.Info($"Avvio {(test ? "(prova)" : "")}. Config: {paths.ConfigPath}");
        WarnIfVantageRunning();

        var code = RunLoop(cfg, stop, test);
        if (code != 0) return code;
        Log.Info("Terminato.");
        return 0;
    }


    internal static int RunLoop(Config cfg, EventWaitHandle stop, bool test)
    {
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
                Log.Warn($"Apertura tastiera fallita: {ex.Message}");
            }

            if (dev is null)
            {
                if (!announcedMissing)
                {
                    Log.Warn("Tastiera Spectrum non trovata, riprovo ogni 3 secondi (se non si trova mai, avvia come amministratore).");
                    announcedMissing = true;
                }
                Live.Status = "In attesa della tastiera...";
                if (test) return 2;
                stop.WaitOne(3000);
                continue;
            }

            announcedMissing = false;
            try
            {
                Log.Info($"Tastiera collegata: {dev.Description}, {dev.Width}x{dev.Height}");
                Live.Status = "Tastiera collegata";
                if (test) RunTest(dev, cfg, stop);
                else RunSession(dev, cfg, stop);
            }
            catch (Exception ex)
            {
                Log.Warn($"Sessione interrotta: {ex.Message}");
            }
            finally
            {
                dev.Dispose();
            }

            if (test) break;
            if (!stop.WaitOne(2000))
                Log.Info("Riconnessione alla tastiera...");
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
            throw new InvalidOperationException("La tastiera non ha accettato il comando di avvio.");

        try
        {
            var colors = new (byte r, byte g, byte b, string n)[] { (255, 0, 0, "rosso"), (0, 255, 0, "verde"), (0, 0, 255, "blu") };
            foreach (var c in colors)
            {
                Log.Info($"Prova: tutti i tasti {c.n}");
                var buf = new byte[codes.Count * 3];
                for (var i = 0; i < codes.Count; i++) { buf[i * 3] = c.r; buf[i * 3 + 1] = c.g; buf[i * 3 + 2] = c.b; }
                var until = Stopwatch.StartNew();
                while (until.Elapsed.TotalSeconds < 2.5 && !stop.WaitOne(0))
                {
                    if (!dev.SendFrame(codes, buf, codes.Count))
                        throw new InvalidOperationException("Invio fotogramma fallito.");
                    stop.WaitOne(60);
                }
            }
        }
        finally
        {
            dev.AuroraStop(profile);
            Log.Info("Prova terminata, luci ripristinate dal profilo.");
        }
    }

    private static void RunSession(SpectrumDevice dev, Config cfg, EventWaitHandle stop)
    {
        var profile = cfg.Profile > 0 ? cfg.Profile : dev.GetProfile();
        Log.Info($"Profilo tastiera: {profile}");

        var wallSig = WallpaperSignature(cfg);
        var wall = LoadWallpaper(cfg) ?? Field.Rainbow();
        var engine = new FlowEngine(cfg, dev.KeyCodes, wall);

        var allCodes = new List<ushort>(engine.KeyCodes);
        allCodes.AddRange(dev.ExtraKeyCodes);
        var rgb = new byte[allCodes.Count * 3];
        var extraCount = dev.ExtraKeyCodes.Length;

        if (!dev.AuroraStart(profile))
            throw new InvalidOperationException("La tastiera non ha accettato il comando di avvio.");

        Log.Info($"Effetto attivo: {engine.KeyCount} tasti, {cfg.Fps} fps.");
        Live.Status = $"Effetto attivo - {engine.KeyCount} tasti";

        var sw = Stopwatch.StartNew();
        var nextFrame = 0.0;
        var nextWin = 0.0;
        double nextWall = cfg.WallpaperRecheckSeconds;

        long lastHandle = long.MinValue;
        var lastKind = Desktop.WindowKind.Desktop;
        var failures = 0;

        try
        {
            while (!stop.WaitOne(0))
            {
                var now = sw.Elapsed.TotalSeconds;

                if (now >= nextWin)
                {
                    nextWin = now + cfg.WindowSampleMs / 1000.0;
                    SampleWindow(engine, cfg, now, ref lastHandle, ref lastKind, Live.ConsumeWave());
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
                            Log.Info("Sfondo del desktop cambiato: aggiornato.");
                        }
                    }
                }

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
                    throw new IOException("Invio dei fotogrammi fallito ripetutamente (sospensione/ripresa?).");

                nextFrame += 1.0 / cfg.Fps;
                var wait = nextFrame - sw.Elapsed.TotalSeconds;
                if (wait > 0) stop.WaitOne((int)(wait * 1000));
                else nextFrame = sw.Elapsed.TotalSeconds;
            }
        }
        finally
        {
            try { dev.AuroraStop(profile); Log.Info("Effetto fermato, luci ripristinate dal profilo."); } catch { }
        }
    }

    private static void SampleWindow(FlowEngine engine, Config cfg, double now, ref long lastHandle, ref Desktop.WindowKind lastKind, bool force)
    {
        var info = Desktop.GetForeground();

        if (info.Kind == Desktop.WindowKind.Ignore)
        {
            // Barra delle applicazioni, menu Start, ecc.: la finestra "vera" resta quella precedente.
            if (lastKind == Desktop.WindowKind.Normal)
                engine.UpdateWindow(Desktop.CaptureWindow(lastHandle, cfg.ChromaThreshold, cfg.ValueThreshold), force, now);
            else if (force)
                engine.UpdateWindow(null, true, now);
            return;
        }

        var changed = force || info.Handle != lastHandle || info.Kind != lastKind;
        Field? f = info.Kind == Desktop.WindowKind.Normal
            ? Desktop.CaptureWindow(info.Handle, cfg.ChromaThreshold, cfg.ValueThreshold)
            : null;

        engine.UpdateWindow(f, changed, now);
        lastHandle = info.Handle;
        lastKind = info.Kind;
    }

    private static string WallpaperSignature(Config cfg)
    {
        var path = string.IsNullOrWhiteSpace(cfg.WallpaperOverride) ? Desktop.GetWallpaperPath() : cfg.WallpaperOverride;
        if (string.IsNullOrEmpty(path)) return "";
        try { return path + "|" + File.GetLastWriteTimeUtc(path).Ticks; } catch { return path; }
    }

    private static Field? LoadWallpaper(Config cfg)
    {
        var path = string.IsNullOrWhiteSpace(cfg.WallpaperOverride) ? Desktop.GetWallpaperPath() : cfg.WallpaperOverride;
        if (string.IsNullOrEmpty(path))
        {
            Log.Warn("Nessuno sfondo immagine impostato: uso un arcobaleno di riserva.");
            return null;
        }

        var f = Desktop.LoadImage(path);
        if (f is null)
            Log.Warn($"Impossibile leggere lo sfondo '{path}': uso un arcobaleno di riserva.");
        else
            Log.Info($"Sfondo letto: {path}");
        return f;
    }
}
