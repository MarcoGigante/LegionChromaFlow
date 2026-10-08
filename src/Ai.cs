using System.Drawing.Imaging;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LegionChromaFlow;

/// <summary>A lighting "scene" designed by the AI for what is currently on screen.</summary>
internal sealed record AiSceneData(float[][] Palette, string Pattern, double Speed, double Intensity, string Mood);

/// <summary>Holder used to hand a scene (or "no scene") from the AI thread to the render loop.</summary>
internal sealed class AiBox { public AiSceneData? Data; }

/// <summary>Stores the Anthropic API key encrypted with the current Windows user (DPAPI). Never leaves this PC except in API calls.</summary>
internal static class AiKeyStore
{
    [StructLayout(LayoutKind.Sequential)]
    private struct DataBlob { public int cbData; public IntPtr pbData; }

    [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CryptProtectData(ref DataBlob dataIn, string? desc, IntPtr entropy, IntPtr reserved, IntPtr prompt, int flags, out DataBlob dataOut);

    [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CryptUnprotectData(ref DataBlob dataIn, IntPtr desc, IntPtr entropy, IntPtr reserved, IntPtr prompt, int flags, out DataBlob dataOut);

    [DllImport("kernel32.dll")] private static extern IntPtr LocalFree(IntPtr mem);

    private static string FilePath => Path.Combine(AppPaths.Resolve().Root, "config", "ai.key");

    public const string EnvVar = "ANTHROPIC_API_KEY";

    /// <summary>True if a key was saved from the panel.</summary>
    public static bool HasSavedKey => File.Exists(FilePath);

    /// <summary>True if the key comes from the environment variable instead.</summary>
    public static bool HasEnvKey => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(EnvVar));

    public static bool HasKey => HasSavedKey || HasEnvKey;

    public static string? Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var plain = Unprotect(File.ReadAllBytes(FilePath));
                if (plain is not null) return Encoding.UTF8.GetString(plain).Trim();
            }
        }
        catch (Exception ex) { Log.Warn($"Cannot read the saved AI key: {ex.Message}"); }
        var env = Environment.GetEnvironmentVariable(EnvVar);
        return string.IsNullOrWhiteSpace(env) ? null : env.Trim();
    }

    public static bool Save(string key)
    {
        try
        {
            var enc = Protect(Encoding.UTF8.GetBytes(key.Trim()));
            if (enc is null) return false;
            File.WriteAllBytes(FilePath, enc);
            return true;
        }
        catch (Exception ex) { Log.Warn($"Cannot save the AI key: {ex.Message}"); return false; }
    }

    public static void Clear()
    {
        try { if (File.Exists(FilePath)) File.Delete(FilePath); } catch { }
    }

    private static byte[]? Protect(byte[] data) => Transform(data, protect: true);
    private static byte[]? Unprotect(byte[] data) => Transform(data, protect: false);

    private static byte[]? Transform(byte[] data, bool protect)
    {
        var input = new DataBlob { cbData = data.Length, pbData = Marshal.AllocHGlobal(data.Length) };
        try
        {
            Marshal.Copy(data, 0, input.pbData, data.Length);
            var ok = protect
                ? CryptProtectData(ref input, "LegionChromaFlow", IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, out var o1)
                : CryptUnprotectData(ref input, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, out o1);
            if (!ok) return null;
            var result = new byte[o1.cbData];
            Marshal.Copy(o1.pbData, result, 0, o1.cbData);
            LocalFree(o1.pbData);
            return result;
        }
        finally { Marshal.FreeHGlobal(input.pbData); }
    }
}

/// <summary>Asks Claude to design a lighting scene for a screenshot of the active window.</summary>
internal static class AiDirector
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(40) };

    private const string SystemPrompt =
        "You are a lighting designer for a per-key RGB laptop keyboard. You receive a screenshot of the window the user is looking at. " +
        "Design a short lighting scene that matches the mood and colors of what you see. " +
        "Reply with ONLY a JSON object, no markdown, with exactly these fields: " +
        "{\"palette\":[\"#rrggbb\", ... 3 to 5 vivid colors taken from or inspired by the image, ordered from dark/background to bright/accent],"
        + "\"pattern\":\"one of: aurora, pulse, wave, sparkle, rain, fire, breathe\","
        + "\"speed\":number from 0 (very slow) to 1 (fast),"
        + "\"intensity\":number from 0 (dim) to 1 (bright),"
        + "\"mood\":\"two or three words\"}. "
        + "Pick the pattern that fits best: calm landscapes/documents -> breathe or aurora; code/terminals/matrix-like -> rain; action games/video -> pulse or wave; "
        + "warm/sunset/explosions -> fire; night sky/stars/cities -> sparkle. Prefer saturated colors, avoid white and gray.";

    private static readonly string[] Patterns = { "aurora", "pulse", "wave", "sparkle", "rain", "fire", "breathe" };

    public static async Task<AiSceneData> GenerateAsync(byte[] jpeg, string apiKey, string model, CancellationToken ct)
    {
        var body = new
        {
            model,
            max_tokens = 400,
            system = SystemPrompt,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "image", source = new { type = "base64", media_type = "image/jpeg", data = Convert.ToBase64String(jpeg) } },
                        new { type = "text", text = "Design the keyboard lighting scene for this window." },
                    }
                }
            }
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
        req.Headers.Add("x-api-key", apiKey);
        req.Headers.Add("anthropic-version", "2023-06-01");
        req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        using var resp = await Http.SendAsync(req, ct).ConfigureAwait(false);
        var text = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode)
        {
            var msg = text;
            try { msg = JsonDocument.Parse(text).RootElement.GetProperty("error").GetProperty("message").GetString() ?? text; } catch { }
            throw new InvalidOperationException($"HTTP {(int)resp.StatusCode}: {Shorten(msg)}");
        }

        using var doc = JsonDocument.Parse(text);
        var sb = new StringBuilder();
        foreach (var part in doc.RootElement.GetProperty("content").EnumerateArray())
            if (part.TryGetProperty("text", out var t)) sb.Append(t.GetString());
        return Parse(sb.ToString());
    }

    private static string Shorten(string s) => s.Length <= 140 ? s : s[..140] + "...";

    /// <summary>Parses the model reply (tolerates markdown fences and extra text) into a validated scene.</summary>
    public static AiSceneData Parse(string reply)
    {
        var a = reply.IndexOf('{');
        var b = reply.LastIndexOf('}');
        if (a < 0 || b <= a) throw new FormatException("No JSON object in the reply.");
        using var doc = JsonDocument.Parse(reply[a..(b + 1)]);
        var root = doc.RootElement;

        var palette = new List<float[]>();
        if (root.TryGetProperty("palette", out var pal) && pal.ValueKind == JsonValueKind.Array)
            foreach (var c in pal.EnumerateArray())
                if (TryHex(c.GetString(), out var rgb)) palette.Add(rgb);
        if (palette.Count < 2) throw new FormatException("The scene needs at least two valid colors.");
        if (palette.Count > 6) palette = palette.Take(6).ToList();

        var pattern = root.TryGetProperty("pattern", out var p) ? (p.GetString() ?? "").Trim().ToLowerInvariant() : "";
        if (!Patterns.Contains(pattern)) pattern = "aurora";

        double Num(string name, double def)
            => root.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? Math.Clamp(v.GetDouble(), 0, 1) : def;

        var mood = root.TryGetProperty("mood", out var m) ? (m.GetString() ?? "") : "";
        if (mood.Length > 40) mood = mood[..40];
        return new AiSceneData(palette.ToArray(), pattern, Num("speed", 0.5), Num("intensity", 0.7), mood.Trim());
    }

    private static bool TryHex(string? s, out float[] rgb)
    {
        rgb = new float[3];
        if (string.IsNullOrWhiteSpace(s)) return false;
        s = s.Trim().TrimStart('#');
        if (s.Length != 6) return false;
        try
        {
            for (var i = 0; i < 3; i++) rgb[i] = Convert.ToInt32(s.Substring(i * 2, 2), 16) / 255f;
            return true;
        }
        catch { return false; }
    }
}

/// <summary>Decides when to ask the AI, captures the window and hands the result to the engine.</summary>
internal sealed class AiController
{
    private readonly Config _cfg;
    private readonly FlowEngine _engine;
    private long _pendingHandle;
    private double _pendingAt;
    private double _lastRequest = double.NegativeInfinity;
    private int _inFlight;
    private string _lastTitleSkipped = "";

    public AiController(Config cfg, FlowEngine engine) { _cfg = cfg; _engine = engine; }

    /// <summary>Called when the foreground window changes. kind != Normal means "no real window".</summary>
    public void OnWindowChanged(Desktop.WindowKind kind, long handle, double now)
    {
        if (kind != Desktop.WindowKind.Normal)
        {
            _pendingHandle = 0;
            _engine.SetAiScene(null);
            return;
        }
        _pendingHandle = handle;
        _pendingAt = now + 1.0; // let the new window finish painting
    }

    public void Tick(double now)
    {
        if (!_cfg.AiEnabled)
        {
            if (_pendingHandle != 0) { _pendingHandle = 0; _engine.SetAiScene(null); }
            Live.SetAi("off");
            return;
        }
        var key = AiKeyStore.Load();
        if (key is null) { Live.SetAi("nokey"); return; }
        if (Live.AiKey is "off" or "nokey") Live.SetAi("idle");

        if (_pendingHandle == 0 || now < _pendingAt) return;
        if (now - _lastRequest < _cfg.AiMinSeconds) return;
        if (Interlocked.CompareExchange(ref _inFlight, 1, 0) != 0) return;

        var handle = _pendingHandle;
        _pendingHandle = 0;

        var title = Desktop.GetTitle(handle);
        if (IsSkipped(title))
        {
            Interlocked.Exchange(ref _inFlight, 0);
            _engine.SetAiScene(null);
            Live.SetAi("skipped");
            return;
        }

        var jpeg = Desktop.CaptureJpeg(handle, 768);
        if (jpeg is null) { Interlocked.Exchange(ref _inFlight, 0); return; }

        _lastRequest = now;
        Live.SetAi("requesting");
        var model = _cfg.AiModel;
        _ = Task.Run(async () =>
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(45));
                var scene = await AiDirector.GenerateAsync(jpeg, key, model, cts.Token).ConfigureAwait(false);
                _engine.SetAiScene(scene);
                Live.SetAi("ok", scene);
                Log.Info($"AI scene: {scene.Pattern}, mood '{scene.Mood}', {scene.Palette.Length} colors.");
            }
            catch (Exception ex)
            {
                Live.SetAi("error", null, ex.Message);
                Log.Warn($"AI scene failed: {ex.Message}");
            }
            finally { Interlocked.Exchange(ref _inFlight, 0); }
        });
    }

    private bool IsSkipped(string title)
    {
        if (string.IsNullOrWhiteSpace(_cfg.AiSkipTitles) || string.IsNullOrEmpty(title)) return false;
        foreach (var word in _cfg.AiSkipTitles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            if (title.Contains(word, StringComparison.OrdinalIgnoreCase)) { _lastTitleSkipped = word; return true; }
        return false;
    }
}
