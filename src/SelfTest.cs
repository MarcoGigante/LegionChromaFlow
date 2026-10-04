namespace LegionChromaFlow;

/// <summary>Controlli sulla logica degli effetti, eseguibili anche senza tastiera (LegionChromaFlow.dll selftest).</summary>
internal static class SelfTest
{
    private static int _failed;

    private static void Check(bool ok, string what)
    {
        Console.WriteLine($"  [{(ok ? "OK" : "FAILED")}] {what}");
        if (!ok) _failed++;
    }

    public static int Run()
    {
        Console.WriteLine("Effect self-test");

        // --- Peso colore ---
        Console.WriteLine("Color weight of window pixels:");
        const float c = 0.14f, v = 0.12f;
        Check(Desktop.ColorWeight(0, 0, 0, c, v) == 0f, "black -> no influence");
        Check(Desktop.ColorWeight(1, 1, 1, c, v) == 0f, "white -> no influence");
        Check(Desktop.ColorWeight(0.5f, 0.5f, 0.5f, c, v) == 0f, "gray -> no influence");
        Check(Desktop.ColorWeight(0, 0, 0.08f, c, v) == 0f, "near-black blue -> no influence");
        Check(Desktop.ColorWeight(1, 0, 0, c, v) > 0.95f, "vivid red -> full influence");
        Check(Desktop.ColorWeight(0.1f, 0.6f, 0.9f, c, v) > 0.9f, "sky blue -> full influence");

        // --- Tastiera sintetica 22x6 ---
        const int W = 22, H = 6;
        var codes = new ushort[W, H];
        ushort code = 1;
        for (var y = 0; y < H; y++)
            for (var x = 0; x < W; x++)
                codes[x, y] = code++;

        var wall = SyntheticWallpaper();
        var cfg = new Config { RandomRippleEverySeconds = 0, WaveSeconds = 2.0 };
        cfg.Clamp();

        FlowEngine NewEngine(Config cf) => new(cf, codes, wall, seed: 42);
        var n = W * H;

        // --- A: no window ---
        var a = NewEngine(cfg);
        var rgbA = new byte[n * 3];
        a.UpdateWindow(null, true, 0.0);
        a.Render(5.0, rgbA, out _, out _, out _);

        // --- B: finestra nera/senza colore -> identico ad A ---
        Console.WriteLine("Window without colors (black background):");
        var b = NewEngine(cfg);
        var rgbB = new byte[n * 3];
        b.UpdateWindow(WindowField(0, 0, 0), true, 0.0);
        b.Render(5.0, rgbB, out _, out _, out _);
        Check(rgbA.SequenceEqual(rgbB), "colors stay those of the desktop");

        // --- C: finestra colorata (blu) ---
        Console.WriteLine("Colored window (blue), after the wave ends:");
        var cE = NewEngine(cfg);
        var rgbC = new byte[n * 3];
        cE.UpdateWindow(WindowField(0.1f, 0.3f, 1f), true, 0.0);
        cE.Render(5.0, rgbC, out _, out _, out _);
        var diff = 0; var maxDiff = 0;
        for (var i = 0; i < rgbC.Length; i++) { var d = Math.Abs(rgbC[i] - rgbA[i]); diff += d; maxDiff = Math.Max(maxDiff, d); }
        Check(diff > 0, "the window influences the colors");
        Check(maxDiff < 150, $"the influence is subtle (max difference on one channel: {maxDiff}/255)");

        // --- D: the wave starts from the center ---
        Console.WriteLine("Propagation from the center outwards:");
        var dE = NewEngine(cfg);
        var rgbD = new byte[n * 3];
        dE.UpdateWindow(WindowField(0.1f, 0.3f, 1f), true, 0.0);
        dE.Render(0.5, rgbD, out _, out _, out _); // wave at ~25%: only the center has changed color
        var reference = new byte[n * 3];
        var refE = NewEngine(cfg);
        refE.UpdateWindow(null, true, 0.0);
        refE.Render(0.5, reference, out _, out _, out _);

        int centerKey = (H / 2) * W + W / 2, edgeKey = 0;
        var centerChange = KeyDiff(rgbD, reference, centerKey);
        var edgeChange = KeyDiff(rgbD, reference, edgeKey);
        Check(centerChange > edgeChange, $"the center changes before the edges (center={centerChange}, edge={edgeChange})");
        Check(edgeChange == 0, "the edge is still unchanged at the start of the wave");

        // --- D2: "barrier" style: band of switched-off keys at the front, new colors behind, old ones ahead ---
        Console.WriteLine("Barrier style:");
        var bcfg = new Config { RandomRippleEverySeconds = 0, WaveSeconds = 2.0, WaveStyle = "barrier", BarrierWidth = 0.2 };
        bcfg.Clamp();
        var bE = NewEngine(bcfg);
        var rgbBar = new byte[n * 3];
        bE.UpdateWindow(WindowField(0.1f, 0.3f, 1f), true, 0.0);
        bE.Render(1.0, rgbBar, out _, out _, out _); // half wave
        var off = 0; var lit2 = 0;
        for (var k = 0; k < n; k++)
        {
            if (rgbBar[k * 3] + rgbBar[k * 3 + 1] + rgbBar[k * 3 + 2] < 40) off++; else lit2++;
        }
        Check(off > 0 && lit2 > off, $"the barrier switches off a band of keys ({off} off, {lit2} lit)");
        var bEnd = NewEngine(bcfg);
        var rgbEnd = new byte[n * 3];
        bEnd.UpdateWindow(WindowField(0.1f, 0.3f, 1f), true, 0.0);
        bEnd.Render(5.0, rgbEnd, out _, out _, out _);
        var zero = 0;
        for (var k = 0; k < n; k++) if (rgbEnd[k * 3] + rgbEnd[k * 3 + 1] + rgbEnd[k * 3 + 2] == 0) zero++;
        Check(zero == 0, "at the end of the wave all keys are lit again");

        // --- E: tutti i valori restano validi e nessun tasto spento in modo anomalo ---
        Console.WriteLine("Output consistency:");
        var e = NewEngine(cfg);
        var rgbE = new byte[n * 3];
        var lit = 0;
        for (var t = 0; t < 600; t++)
        {
            e.Render(t * 0.05, rgbE, out _, out _, out _);
            if (t % 100 == 0)
                for (var k = 0; k < n; k++)
                    if (rgbE[k * 3] + rgbE[k * 3 + 1] + rgbE[k * 3 + 2] > 30) lit++;
        }
        Check(lit > n * 3, "keys stay lit with wallpaper colors over time");

        // --- F: the pattern changes over time (dynamic) ---
        var f1 = new byte[n * 3]; var f2 = new byte[n * 3];
        var fe = NewEngine(cfg);
        fe.Render(0, f1, out _, out _, out _);
        fe.Render(25, f2, out _, out _, out _);
        Check(!f1.SequenceEqual(f2), "colors evolve over time (dynamic effect)");

        // --- G: the shipped configuration file parses correctly ---
        Console.WriteLine("Configuration:");
        var cfgPath = AppPaths.Resolve().ConfigPath;
        if (File.Exists(cfgPath))
        {
            var loaded = Config.Load(cfgPath);
            Check(loaded.Fps >= 5 && loaded.WindowInfluence >= 0, "config.json parsed (comments included)");
        }
        else Console.WriteLine("  (config.json not present, check skipped)");

        Console.WriteLine(_failed == 0 ? "All checks passed." : $"{_failed} checks failed.");
        return _failed == 0 ? 0 : 1;
    }

    private static int KeyDiff(byte[] a, byte[] b, int key)
        => Math.Abs(a[key * 3] - b[key * 3]) + Math.Abs(a[key * 3 + 1] - b[key * 3 + 1]) + Math.Abs(a[key * 3 + 2] - b[key * 3 + 2]);

    /// <summary>Uniform window of color (r,g,b); if the color has no chroma/brightness the weight is 0.</summary>
    private static Field WindowField(float r, float g, float b)
    {
        var f = new Field(64, 36, 4);
        var w = Desktop.ColorWeight(r, g, b, 0.14f, 0.12f);
        for (var i = 0; i < 64 * 36; i++)
        {
            f.D[i * 4] = r * w; f.D[i * 4 + 1] = g * w; f.D[i * 4 + 2] = b * w; f.D[i * 4 + 3] = w;
        }
        return f;
    }

    /// <summary>Test wallpaper: orange/purple/green color blobs on a dark background.</summary>
    private static Field SyntheticWallpaper()
    {
        var f = new Field(160, 90, 3);
        for (var y = 0; y < 90; y++)
            for (var x = 0; x < 160; x++)
            {
                var hue = (float)((Math.Sin(x * 0.07) + Math.Cos(y * 0.11) + 2) / 4);
                var val = 0.35f + 0.6f * (float)(0.5 + 0.5 * Math.Sin(x * 0.13 + y * 0.09));
                var (r, g, b) = Field.HsvToRgb(hue, 0.85f, val);
                var i = (y * 160 + x) * 3;
                f.D[i] = r; f.D[i + 1] = g; f.D[i + 2] = b;
            }
        return f;
    }
}
