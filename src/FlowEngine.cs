namespace LegionChromaFlow;

/// <summary>
/// Calcola i colori dei tasti.
///  - Base: lo sfondo del desktop, che scorre/ondeggia lentamente e in modo casuale sulla tastiera.
///  - Influenza: i colori della finestra attiva, miscelati in modo lieve (WindowInfluence) e
///    solo dove la finestra ha davvero colore. Quando cambia la finestra, il nuovo colore si propaga
///    come un'onda dal centro della tastiera verso l'esterno.
/// </summary>
internal sealed class FlowEngine
{
    private readonly Config _cfg;
    private readonly Random _rnd;

    private Field _wall;

    private readonly int _n;
    private readonly ushort[] _codes;
    private readonly float[] _nx, _ny, _dist; // posizioni normalizzate e distanza dal centro (0..1)

    // Influenza della finestra, per tasto, in formato premoltiplicato (r*w, g*w, b*w, w)
    private readonly float[] _prev, _target, _infl, _tgtSm;
    private double _lastRender = double.NegativeInfinity;
    private bool _haveTarget;
    private double _waveStart = double.NegativeInfinity;
    private bool _waveActive;

    // Fasi casuali per il moto dello sfondo
    private readonly double[] _ph = new double[8];

    private sealed class Ripple { public float X, Y; public double T0; }
    private readonly List<Ripple> _ripples = new();
    private double _nextRipple;

    private readonly float[] _avg = new float[3];
    private bool _avgInit;

    public int KeyCount => _n;
    public IReadOnlyList<ushort> KeyCodes => _codes;
    public float[] NormX => _nx;
    public float[] NormY => _ny;

    public FlowEngine(Config cfg, ushort[,] keyCodes, Field wallpaper, int? seed = null)
    {
        _cfg = cfg;
        _rnd = seed.HasValue ? new Random(seed.Value) : new Random();
        _wall = wallpaper;

        var w = keyCodes.GetLength(0);
        var h = keyCodes.GetLength(1);

        var codes = new List<ushort>();
        var xs = new List<int>(); var ys = new List<int>();
        for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
                if (keyCodes[x, y] > 0)
                {
                    codes.Add(keyCodes[x, y]);
                    xs.Add(x); ys.Add(y);
                }

        _n = codes.Count;
        _codes = codes.ToArray();
        _nx = new float[_n]; _ny = new float[_n]; _dist = new float[_n];

        double cx = (w - 1) / 2.0, cy = (h - 1) / 2.0;
        double dmax = 1e-6;
        for (var i = 0; i < _n; i++)
        {
            _nx[i] = w > 1 ? xs[i] / (float)(w - 1) : 0.5f;
            _ny[i] = h > 1 ? ys[i] / (float)(h - 1) : 0.5f;
            var d = Math.Sqrt((xs[i] - cx) * (xs[i] - cx) + (ys[i] - cy) * (ys[i] - cy));
            _dist[i] = (float)d;
            dmax = Math.Max(dmax, d);
        }
        for (var i = 0; i < _n; i++) _dist[i] = (float)(_dist[i] / dmax);

        _prev = new float[_n * 4];
        _target = new float[_n * 4];
        _infl = new float[_n * 4];
        _tgtSm = new float[_n * 4];

        for (var i = 0; i < _ph.Length; i++) _ph[i] = _rnd.NextDouble() * Math.PI * 2;
    }

    public void SetWallpaper(Field wallpaper) => _wall = wallpaper;

    /// <summary>
    /// Aggiorna la finestra attiva. changed=true avvia l'onda dal centro.
    /// win=null significa "nessun colore disponibile" (desktop, finestra non leggibile).
    /// </summary>
    public void UpdateWindow(Field? win, bool changed, double now)
    {
        if (changed)
        {
            Array.Copy(_infl, _prev, _infl.Length);
            _waveStart = now;
            _waveActive = true;
        }

        Span<float> s = stackalloc float[4];
        for (var i = 0; i < _n; i++)
        {
            if (win is null) { s[0] = s[1] = s[2] = s[3] = 0f; }
            else win.Sample(_nx[i], _ny[i], false, s);

            for (var c = 0; c < 4; c++)
            {
                var idx = i * 4 + c;
                // Alla prima lettura o al cambio finestra il bersaglio e' netto; altrimenti si smussa.
                _target[idx] = (changed || !_haveTarget) ? s[c] : _target[idx] + (s[c] - _target[idx]) * 0.4f;
            }
        }
        if (changed || !_haveTarget) Array.Copy(_target, _tgtSm, _target.Length); // nuova finestra: scatto netto, ci pensa l'onda
        _haveTarget = true;
    }

    /// <summary>Calcola un fotogramma. rgb deve contenere almeno KeyCount*3 byte.</summary>
    public void Render(double now, byte[] rgb, out byte extraR, out byte extraG, out byte extraB)
    {
        var cfg = _cfg;

        // Il bersaglio della finestra viene inseguito con una costante di tempo: meno scatti, luci piu' fluide.
        var dt = double.IsNegativeInfinity(_lastRender) ? 0.0 : Math.Clamp(now - _lastRender, 0.0, 0.5);
        _lastRender = now;
        var follow = cfg.WindowFollowSeconds;
        var alpha = follow <= 0.01 ? 1f : (float)(1.0 - Math.Exp(-dt / follow));
        for (var q = 0; q < _tgtSm.Length; q++) _tgtSm[q] += (_target[q] - _tgtSm[q]) * alpha;

        var T = cfg.WaveSeconds;
        var barrier = cfg.WaveStyle == "barrier";
        var band = (float)(barrier ? cfg.BarrierWidth : cfg.WaveBand);

        // Avanzamento dell'onda
        float waveR = 0f;
        if (_waveActive)
        {
            var p = (now - _waveStart) / T;
            waveR = (float)(p * (1.0 + band));
            if (barrier) waveR -= band / 2f; // il centro della barriera parte prima del centro e finisce oltre il bordo
            if (p >= 1.0 + 0.05) _waveActive = false;
        }

        // Onde casuali di luce
        if (cfg.RandomRippleEverySeconds > 0 && now >= _nextRipple)
        {
            var i = _rnd.Next(_n);
            _ripples.Add(new Ripple { X = _nx[i], Y = _ny[i], T0 = now });
            _nextRipple = now + cfg.RandomRippleEverySeconds * (0.5 + _rnd.NextDouble());
        }
        _ripples.RemoveAll(r => now - r.T0 > 3.0);

        // Moto dello sfondo: centro e zoom che vagano lentamente, con una leggera deformazione
        var sp = cfg.DriftSpeed;
        var t = now * sp;
        var ox = 0.5 + 0.36 * Math.Sin(0.110 * t + _ph[0]) + 0.12 * Math.Sin(0.270 * t + _ph[1]);
        var oy = 0.5 + 0.34 * Math.Sin(0.090 * t + _ph[2]) + 0.12 * Math.Sin(0.230 * t + _ph[3]);
        var zoom = 1.0 + 0.18 * Math.Sin(0.060 * t + _ph[4]);
        var scaleX = 0.70 * zoom;
        var scaleY = 0.38 * zoom;

        Span<float> c4 = stackalloc float[4];
        Span<float> c3 = stackalloc float[3];
        double sr = 0, sg = 0, sb = 0;

        for (var k = 0; k < _n; k++)
        {
            // ---- Base: sfondo del desktop ----
            var warpU = 0.045 * Math.Sin(_ny[k] * 6.0 + now * 0.55 * Math.Max(sp, 0.05) + _ph[5]);
            var warpV = 0.045 * Math.Sin(_nx[k] * 5.0 - now * 0.45 * Math.Max(sp, 0.05) + _ph[6]);
            var u = (float)(ox + (_nx[k] - 0.5) * scaleX + warpU);
            var v = (float)(oy + (_ny[k] - 0.5) * scaleY + warpV);
            _wall.Sample(u, v, true, c3);
            float r = c3[0], g = c3[1], b = c3[2];

            // Sfarfallio leggero e casuale tra un tasto e l'altro
            if (cfg.Shimmer > 0)
            {
                var sh = 1.0 + cfg.Shimmer * (0.6 * Math.Sin(now * 0.9 + k * 1.713 + _ph[7])
                                             + 0.4 * Math.Sin(now * 1.7 + k * 0.529));
                r *= (float)sh; g *= (float)sh; b *= (float)sh;
            }

            // Onde casuali: leggero aumento di luminosita' sul fronte
            var gain = 1.0f;
            foreach (var rp in _ripples)
            {
                var age = now - rp.T0;
                var rad = age * 0.55;
                var dx = _nx[k] - rp.X; var dy = (_ny[k] - rp.Y) * 0.55; // la tastiera e' piu' larga che alta
                var d = Math.Sqrt(dx * dx + dy * dy);
                var e = (d - rad) / 0.14;
                gain += (float)(0.30 * (1.0 - age / 3.0) * Math.Exp(-e * e));
            }
            r *= gain; g *= gain; b *= gain;

            // ---- Influenza della finestra attiva ----
            float ir, ig, ib, iw;
            var dark = 1f;
            if (_waveActive)
            {
                // barrier: passaggio netto dentro/fuori dal fronte; smooth: dissolvenza morbida
                var a = barrier ? (_dist[k] <= waveR ? 1f : 0f) : Smooth((waveR - _dist[k]) / band);
                var j = k * 4;
                iw = _prev[j + 3] + (_tgtSm[j + 3] - _prev[j + 3]) * a;
                ir = _prev[j] + (_tgtSm[j] - _prev[j]) * a;
                ig = _prev[j + 1] + (_tgtSm[j + 1] - _prev[j + 1]) * a;
                ib = _prev[j + 2] + (_tgtSm[j + 2] - _prev[j + 2]) * a;

                // Fronte d'onda luminoso
                if (barrier)
                {
                    // Barriera di tasti spenti centrata sul fronte
                    var off = Math.Abs(_dist[k] - waveR) / (band / 2f);
                    dark = Smooth(off);
                }
                else if (cfg.WaveGlow > 0)
                {
                    var e = (_dist[k] - waveR) / band;
                    var glow = (float)(cfg.WaveGlow * Math.Exp(-e * e * 3.0));
                    r += glow * 0.6f; g += glow * 0.6f; b += glow * 0.6f;
                }
            }
            else
            {
                var j = k * 4;
                ir = _tgtSm[j]; ig = _tgtSm[j + 1]; ib = _tgtSm[j + 2]; iw = _tgtSm[j + 3];
            }

            _infl[k * 4] = ir; _infl[k * 4 + 1] = ig; _infl[k * 4 + 2] = ib; _infl[k * 4 + 3] = iw;

            if (iw > 1e-4f && cfg.WindowInfluence > 0)
            {
                // ir,ig,ib sono premoltiplicati: il colore reale e' ir/iw, il peso e' iw.
                var m = (float)(cfg.WindowInfluence * Math.Clamp(iw, 0f, 1f));
                r += (ir / iw - r) * m;
                g += (ig / iw - g) * m;
                b += (ib / iw - b) * m;
            }

            if (dark < 1f) { r *= dark; g *= dark; b *= dark; }

            // ---- Rifinitura: saturazione, luminosita', gamma ----
            var luma = 0.299f * r + 0.587f * g + 0.114f * b;
            var sat = (float)cfg.Saturation;
            r = luma + (r - luma) * sat;
            g = luma + (g - luma) * sat;
            b = luma + (b - luma) * sat;

            r = Out(r, cfg); g = Out(g, cfg); b = Out(b, cfg);
            rgb[k * 3] = (byte)(r * 255f + 0.5f);
            rgb[k * 3 + 1] = (byte)(g * 255f + 0.5f);
            rgb[k * 3 + 2] = (byte)(b * 255f + 0.5f);

            sr += r; sg += g; sb += b;
        }

        // Luci extra della tastiera (bordo/logo): colore medio, smussato
        if (_n > 0)
        {
            var ar = (float)(sr / _n); var ag = (float)(sg / _n); var ab = (float)(sb / _n);
            if (!_avgInit) { _avg[0] = ar; _avg[1] = ag; _avg[2] = ab; _avgInit = true; }
            else { _avg[0] += (ar - _avg[0]) * 0.1f; _avg[1] += (ag - _avg[1]) * 0.1f; _avg[2] += (ab - _avg[2]) * 0.1f; }
        }
        extraR = (byte)(Math.Clamp(_avg[0], 0f, 1f) * 255f + 0.5f);
        extraG = (byte)(Math.Clamp(_avg[1], 0f, 1f) * 255f + 0.5f);
        extraB = (byte)(Math.Clamp(_avg[2], 0f, 1f) * 255f + 0.5f);
    }

    private static float Out(float c, Config cfg)
    {
        c = Math.Clamp(c, 0f, 1f);
        c = (float)Math.Pow(c, cfg.Gamma);
        return Math.Clamp(c * (float)cfg.Brightness, 0f, 1f);
    }

    private static float Smooth(float x)
    {
        x = Math.Clamp(x, 0f, 1f);
        return x * x * (3f - 2f * x);
    }
}
