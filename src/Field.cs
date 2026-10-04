namespace LegionChromaFlow;

/// <summary>Immagine a bassa risoluzione con N canali float, campionabile con interpolazione bilineare.</summary>
internal sealed class Field
{
    public int W { get; }
    public int H { get; }
    public int C { get; }
    public float[] D { get; }

    public Field(int w, int h, int channels)
    {
        W = w; H = h; C = channels;
        D = new float[w * h * channels];
    }

    private static float Mirror(float x)
    {
        x %= 2f;
        if (x < 0) x += 2f;
        return x > 1f ? 2f - x : x;
    }

    /// <summary>Campiona in (u,v) normalizzati. mirror=true riflette i bordi (niente giunture), altrimenti clamp.</summary>
    public void Sample(float u, float v, bool mirror, Span<float> o)
    {
        if (mirror) { u = Mirror(u); v = Mirror(v); }
        else { u = Math.Clamp(u, 0f, 1f); v = Math.Clamp(v, 0f, 1f); }

        var fx = u * (W - 1);
        var fy = v * (H - 1);
        var x0 = (int)fx; var y0 = (int)fy;
        var x1 = Math.Min(x0 + 1, W - 1); var y1 = Math.Min(y0 + 1, H - 1);
        var tx = fx - x0; var ty = fy - y0;

        for (var c = 0; c < C; c++)
        {
            var a = D[(y0 * W + x0) * C + c];
            var b = D[(y0 * W + x1) * C + c];
            var d = D[(y1 * W + x0) * C + c];
            var e = D[(y1 * W + x1) * C + c];
            var top = a + (b - a) * tx;
            var bot = d + (e - d) * tx;
            o[c] = top + (bot - top) * ty;
        }
    }

    /// <summary>Immagine di riserva: arcobaleno diagonale, usata se lo sfondo non e' leggibile.</summary>
    public static Field Rainbow(int w = 64, int h = 36)
    {
        var f = new Field(w, h, 3);
        for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var hue = ((float)x / w * 0.8f + (float)y / h * 0.2f) % 1f;
                var (r, g, b) = HsvToRgb(hue, 0.85f, 0.95f);
                var i = (y * w + x) * 3;
                f.D[i] = r; f.D[i + 1] = g; f.D[i + 2] = b;
            }
        return f;
    }

    public static (float r, float g, float b) HsvToRgb(float h, float s, float v)
    {
        h = (h % 1f + 1f) % 1f * 6f;
        var i = (int)h; var f = h - i;
        var p = v * (1 - s); var q = v * (1 - s * f); var t = v * (1 - s * (1 - f));
        return i switch
        {
            0 => (v, t, p),
            1 => (q, v, p),
            2 => (p, v, t),
            3 => (p, q, v),
            4 => (t, p, v),
            _ => (v, p, q)
        };
    }
}
