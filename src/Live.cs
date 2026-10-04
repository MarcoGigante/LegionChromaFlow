namespace LegionChromaFlow;

/// <summary>Stato condiviso tra il motore luci (thread in background) e il pannello grafico.</summary>
internal static class Live
{
    public static volatile string Status = "Avvio...";

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
