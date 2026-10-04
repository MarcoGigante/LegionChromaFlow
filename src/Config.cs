using System.Text.Json;

namespace LegionChromaFlow;

/// <summary>Cartelle dell'applicazione (C:\LegionChromaFlow\...).</summary>
internal sealed class AppPaths
{
    public string Root { get; init; } = "";
    public string ConfigPath { get; init; } = "";
    public string LogDir { get; init; } = "";

    public static AppPaths Resolve()
    {
        var baseDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var root = string.Equals(Path.GetFileName(baseDir), "bin", StringComparison.OrdinalIgnoreCase)
            ? (Path.GetDirectoryName(baseDir) ?? baseDir)
            : baseDir;

        return new AppPaths
        {
            Root = root,
            ConfigPath = Path.Combine(root, "config", "config.json"),
            LogDir = Path.Combine(root, "logs")
        };
    }
}

/// <summary>Impostazioni modificabili in config\config.json.</summary>
internal sealed class Config
{
    /// <summary>Fotogrammi al secondo inviati alla tastiera (10-40).</summary>
    public int Fps { get; set; } = 20;

    /// <summary>Luminosita' globale (0.1 - 1.0).</summary>
    public double Brightness { get; set; } = 1.0;

    /// <summary>Saturazione dei colori (1.0 = originale).</summary>
    public double Saturation { get; set; } = 1.25;

    /// <summary>Gamma: valori piu' alti = colori piu' profondi e meno slavati sui LED.</summary>
    public double Gamma { get; set; } = 1.4;

    /// <summary>Quanto la finestra attiva puo' influenzare i colori (0 = mai, 0.3 = lieve, 1 = totale).</summary>
    public double WindowInfluence { get; set; } = 0.28;

    /// <summary>Durata in secondi dell'onda che parte dal centro quando cambi finestra.</summary>
    public double WaveSeconds { get; set; } = 1.8;

    /// <summary>Larghezza del fronte d'onda (0.1 - 1.0, in frazione della tastiera).</summary>
    public double WaveBand { get; set; } = 0.35;

    /// <summary>Quanto si illumina il fronte d'onda mentre si propaga (0 = per niente).</summary>
    public double WaveGlow { get; set; } = 0.15;

    /// <summary>Stile dell'onda: "smooth" (dissolvenza morbida con bagliore) oppure "barrier" (fronte netto con una barriera di tasti spenti).</summary>
    public string WaveStyle { get; set; } = "smooth";

    /// <summary>Spessore della barriera di tasti spenti nello stile "barrier" (0.05 - 0.6, in frazione della tastiera).</summary>
    public double BarrierWidth { get; set; } = 0.14;

    /// <summary>Secondi con cui i colori della finestra vengono "inseguiti" (0 = subito, piu' alto = piu' fluido ma con ritardo).</summary>
    public double WindowFollowSeconds { get; set; } = 1.2;

    /// <summary>Velocita' con cui il motivo dello sfondo scorre sulla tastiera (1 = default, 0 = fermo).</summary>
    public double DriftSpeed { get; set; } = 1.0;

    /// <summary>Intensita' dello sfarfallio casuale tra tasto e tasto (0 = nessuno).</summary>
    public double Shimmer { get; set; } = 0.10;

    /// <summary>Ogni quanti secondi (media) compare un'onda casuale di luce (0 = mai).</summary>
    public double RandomRippleEverySeconds { get; set; } = 8;

    /// <summary>Soglia di "colore": sotto questa croma i pixel della finestra vengono ignorati (grigi, bianchi, neri).</summary>
    public double ChromaThreshold { get; set; } = 0.14;

    /// <summary>Soglia di luminosita': sotto questo valore i pixel della finestra (sfondo nero) vengono ignorati.</summary>
    public double ValueThreshold { get; set; } = 0.12;

    /// <summary>Ogni quanti millisecondi viene letta la finestra attiva.</summary>
    public int WindowSampleMs { get; set; } = 250;

    /// <summary>Ogni quanti secondi si controlla se lo sfondo del desktop e' cambiato.</summary>
    public int WallpaperRecheckSeconds { get; set; } = 10;

    /// <summary>Percorso di un'immagine da usare al posto dello sfondo del desktop (vuoto = usa lo sfondo di Windows).</summary>
    public string WallpaperOverride { get; set; } = "";

    /// <summary>Profilo Spectrum da usare (0 = quello attualmente attivo sulla tastiera).</summary>
    public int Profile { get; set; } = 0;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = true
    };

    public static Config Load(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                var cfg = JsonSerializer.Deserialize<Config>(File.ReadAllText(path), JsonOpts) ?? new Config();
                cfg.Clamp();
                return cfg;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(new Config(), JsonOpts));
            Log.Info($"Creato file di configurazione: {path}");
        }
        catch (Exception ex)
        {
            Log.Warn($"Configurazione non leggibile, uso i valori predefiniti: {ex.Message}");
        }

        var def = new Config();
        def.Clamp();
        return def;
    }

    /// <summary>Aggiorna alcune voci direttamente nel file, conservando i commenti.</summary>
    public static void SaveValues(string path, IReadOnlyDictionary<string, string> values)
    {
        try
        {
            if (!File.Exists(path)) return;
            var text = File.ReadAllText(path);
            foreach (var (key, val) in values)
            {
                var rx = new System.Text.RegularExpressions.Regex("(\"" + key + "\"" + @"\s*:\s*)(""[^""]*""|[^,\r\n/]+)");
                text = rx.IsMatch(text) ? rx.Replace(text, m => m.Groups[1].Value + val, 1) : text;
            }
            File.WriteAllText(path, text);
        }
        catch (Exception ex) { Log.Warn($"Salvataggio configurazione fallito: {ex.Message}"); }
    }

    public void Clamp()
    {
        Fps = Math.Clamp(Fps, 5, 40);
        Brightness = Math.Clamp(Brightness, 0.05, 1.0);
        Saturation = Math.Clamp(Saturation, 0.0, 3.0);
        Gamma = Math.Clamp(Gamma, 0.6, 3.0);
        WindowInfluence = Math.Clamp(WindowInfluence, 0.0, 1.0);
        WaveSeconds = Math.Clamp(WaveSeconds, 0.3, 10.0);
        WaveBand = Math.Clamp(WaveBand, 0.1, 1.0);
        WaveGlow = Math.Clamp(WaveGlow, 0.0, 1.0);
        WindowFollowSeconds = Math.Clamp(WindowFollowSeconds, 0.0, 10.0);
        WaveStyle = string.Equals(WaveStyle?.Trim(), "barrier", StringComparison.OrdinalIgnoreCase) ? "barrier" : "smooth";
        BarrierWidth = Math.Clamp(BarrierWidth, 0.05, 0.6);
        DriftSpeed = Math.Clamp(DriftSpeed, 0.0, 10.0);
        Shimmer = Math.Clamp(Shimmer, 0.0, 0.6);
        RandomRippleEverySeconds = Math.Clamp(RandomRippleEverySeconds, 0.0, 600.0);
        ChromaThreshold = Math.Clamp(ChromaThreshold, 0.0, 0.9);
        ValueThreshold = Math.Clamp(ValueThreshold, 0.0, 0.9);
        WindowSampleMs = Math.Clamp(WindowSampleMs, 50, 5000);
        WallpaperRecheckSeconds = Math.Clamp(WallpaperRecheckSeconds, 2, 3600);
        Profile = Math.Clamp(Profile, 0, 6);
    }
}
