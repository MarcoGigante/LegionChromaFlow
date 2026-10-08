using System.Text.Json;

namespace LegionChromaFlow;

/// <summary>Application folders (C:\LegionChromaFlow\...).</summary>
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

/// <summary>Settings editable in config\config.json.</summary>
internal sealed class Config
{
    /// <summary>Frames per second sent to the keyboard (5-40).</summary>
    public int Fps { get; set; } = 20;

    /// <summary>Global brightness (0.1 - 1.0).</summary>
    public double Brightness { get; set; } = 1.0;

    /// <summary>Color saturation (1.0 = original).</summary>
    public double Saturation { get; set; } = 1.25;

    /// <summary>Gamma: higher values = deeper, less washed-out colors on the LEDs.</summary>
    public double Gamma { get; set; } = 1.4;

    /// <summary>How much the active window can influence the colors (0 = never, 0.3 = subtle, 1 = total).</summary>
    public double WindowInfluence { get; set; } = 0.28;

    /// <summary>Duration in seconds of the wave that starts from the center when you switch window.</summary>
    public double WaveSeconds { get; set; } = 1.8;

    /// <summary>Width of the wave front (0.1 - 1.0, as a fraction of the keyboard).</summary>
    public double WaveBand { get; set; } = 0.35;

    /// <summary>How much the wave front lights up while it travels (0 = not at all).</summary>
    public double WaveGlow { get; set; } = 0.15;

    /// <summary>Interface language: "auto" (Windows display language), "en", "it", "es", "fr", "de", "pt" or "zh".</summary>
    public string Language { get; set; } = "auto";

    /// <summary>Enables AI scenes: a downscaled screenshot of the active window is sent to the Anthropic API (opt-in, needs your own API key).</summary>
    public bool AiEnabled { get; set; } = false;

    /// <summary>How strongly the AI scene covers the normal effect (0 = not at all, 1 = fully).</summary>
    public double AiStrength { get; set; } = 0.75;

    /// <summary>Minimum seconds between two AI requests (limits cost and traffic).</summary>
    public double AiMinSeconds { get; set; } = 12;

    /// <summary>Seconds to cross-fade between AI scenes.</summary>
    public double AiFadeSeconds { get; set; } = 2.0;

    /// <summary>Anthropic model used for scenes.</summary>
    public string AiModel { get; set; } = "claude-haiku-4-5-20251001";

    /// <summary>Comma-separated words: windows whose title contains one of them are never sent to the AI.</summary>
    public string AiSkipTitles { get; set; } = "password,bitwarden,1password,keepass,lastpass,bank,incognito,private browsing,inprivate";

    /// <summary>Wave style: "smooth" (soft dissolve with glow) or "barrier" (crisp front with a band of switched-off keys).</summary>
    public string WaveStyle { get; set; } = "smooth";

    /// <summary>Thickness of the band of switched-off keys in the "barrier" style (0.05 - 0.6, as a fraction of the keyboard).</summary>
    public double BarrierWidth { get; set; } = 0.14;

    /// <summary>Seconds over which the window colors are "chased" (0 = instantly, higher = smoother but delayed).</summary>
    public double WindowFollowSeconds { get; set; } = 1.2;

    /// <summary>Speed at which the wallpaper pattern drifts across the keyboard (1 = default, 0 = still).</summary>
    public double DriftSpeed { get; set; } = 1.0;

    /// <summary>Intensity of the random shimmer from key to key (0 = none).</summary>
    public double Shimmer { get; set; } = 0.10;

    /// <summary>Average seconds between random ripples of light (0 = never).</summary>
    public double RandomRippleEverySeconds { get; set; } = 8;

    /// <summary>"Color" threshold: window pixels below this chroma are ignored (grays, whites, blacks).</summary>
    public double ChromaThreshold { get; set; } = 0.14;

    /// <summary>Brightness threshold: window pixels below this value (black backgrounds) are ignored.</summary>
    public double ValueThreshold { get; set; } = 0.12;

    /// <summary>Milliseconds between reads of the active window.</summary>
    public int WindowSampleMs { get; set; } = 250;

    /// <summary>Seconds between checks for a changed desktop wallpaper.</summary>
    public int WallpaperRecheckSeconds { get; set; } = 10;

    /// <summary>Path of an image to use instead of the desktop wallpaper (empty = use the Windows wallpaper).</summary>
    public string WallpaperOverride { get; set; } = "";

    /// <summary>Spectrum profile to use (0 = the one currently active on the keyboard).</summary>
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
            Log.Info($"Created configuration file: {path}");
        }
        catch (Exception ex)
        {
            Log.Warn($"Configuration not readable, using defaults: {ex.Message}");
        }

        var def = new Config();
        def.Clamp();
        return def;
    }

    /// <summary>Updates some entries directly in the file, preserving the comments.</summary>
    public static void SaveValues(string path, IReadOnlyDictionary<string, string> values)
    {
        try
        {
            if (!File.Exists(path)) return;
            var text = File.ReadAllText(path);
            foreach (var (key, val) in values)
            {
                var rx = new System.Text.RegularExpressions.Regex("(\"" + key + "\"" + @"\s*:\s*)(""[^""]*""|[^,\r\n/]+)");
                if (rx.IsMatch(text)) text = rx.Replace(text, m => m.Groups[1].Value + val, 1);
                else text = InsertKey(text, key, val);
            }
            File.WriteAllText(path, text);
        }
        catch (Exception ex) { Log.Warn($"Saving the configuration failed: {ex.Message}"); }
    }

    /// <summary>Appends a missing key at the end of the JSON object (keeps comments of older config files intact).</summary>
    private static string InsertKey(string text, string key, string val)
    {
        var end = text.LastIndexOf('}');
        if (end < 0) return text;
        var i = end - 1;
        while (i >= 0 && char.IsWhiteSpace(text[i])) i--;
        var comma = i >= 0 && text[i] != ',' && text[i] != '{' ? "," : "";
        return text[..(i + 1)] + comma + Environment.NewLine + "  \"" + key + "\": " + val + Environment.NewLine + text[end..];
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
        AiStrength = Math.Clamp(AiStrength, 0.0, 1.0);
        AiMinSeconds = Math.Clamp(AiMinSeconds, 3.0, 600.0);
        AiFadeSeconds = Math.Clamp(AiFadeSeconds, 0.2, 10.0);
        if (string.IsNullOrWhiteSpace(AiModel)) AiModel = "claude-haiku-4-5-20251001";
        AiSkipTitles ??= "";
        Language = (Language ?? "auto").Trim().ToLowerInvariant();
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
