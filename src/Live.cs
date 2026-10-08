namespace LegionChromaFlow;

/// <summary>State shared between the lighting engine (background thread) and the control panel.</summary>
internal static class Live
{
    private static string _statusKey = "starting";
    private static object[] _statusArgs = Array.Empty<object>();

    public static void SetStatus(string key, params object[] args) { _statusArgs = args; _statusKey = key; }
    public static string StatusText => L.T("status." + _statusKey, _statusArgs);
    // ---- AI scene status (shown in the panel) ----
    private static string _aiKey = "off";
    private static AiSceneData? _aiScene;
    private static string _aiError = "";

    public static void SetAi(string key, AiSceneData? scene = null, string error = "")
    {
        if (key == "ok") _aiScene = scene;
        if (key == "off" || key == "nokey") _aiScene = null;
        _aiError = error;
        _aiKey = key;
    }

    public static string AiKey => _aiKey;
    public static AiSceneData? AiScene => _aiScene;
    public static string AiError => _aiError;

    public static bool StatusOk => _statusKey is "connected" or "active";

    private static int _wave;
    public static void RequestWave() => Interlocked.Exchange(ref _wave, 1);
    public static bool ConsumeWave() => Interlocked.Exchange(ref _wave, 0) == 1;

    private static readonly object Gate = new();
    private static float[] _xs = Array.Empty<float>(), _ys = Array.Empty<float>();
    private static byte[] _rgb = Array.Empty<byte>();
    private static int _n;

    public static void Publish(float[] xs, float[] ys, byte[] rgb, int n)
    {
        lock (Gate)
        {
            _xs = xs; _ys = ys; _n = n;
            if (_rgb.Length != n * 3) _rgb = new byte[n * 3];
            Buffer.BlockCopy(rgb, 0, _rgb, 0, n * 3);
        }
    }

    public static int Snapshot(out float[] xs, out float[] ys, byte[] into)
    {
        lock (Gate)
        {
            xs = _xs; ys = _ys;
            var n = Math.Min(_n, into.Length / 3);
            Buffer.BlockCopy(_rgb, 0, into, 0, n * 3);
            return n;
        }
    }
}
