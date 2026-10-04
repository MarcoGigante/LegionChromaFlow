using System.Drawing.Drawing2D;
using System.Globalization;
using System.Runtime.InteropServices;
using Timer = System.Windows.Forms.Timer;

namespace LegionChromaFlow;

internal static class Theme
{
    public static readonly Color Bg = Color.FromArgb(11, 11, 13);
    public static readonly Color Side = Color.FromArgb(16, 16, 19);
    public static readonly Color Panel = Color.FromArgb(22, 22, 26);
    public static readonly Color PanelHot = Color.FromArgb(28, 28, 33);
    public static readonly Color Line = Color.FromArgb(44, 44, 50);
    public static readonly Color Text = Color.FromArgb(236, 236, 240);
    public static readonly Color Dim = Color.FromArgb(150, 150, 160);
    public static readonly Color Accent = Color.FromArgb(68, 214, 44);   // verde "Razer"
    public static readonly Color Cyan = Color.FromArgb(0, 200, 255);
    public static readonly Color Magenta = Color.FromArgb(255, 60, 200);

    public static Font Font(float size, FontStyle style = FontStyle.Regular) => new("Segoe UI", size, style, GraphicsUnit.Point);

    /// <summary>Fattore di scala del monitor (1.0 = 96 dpi).</summary>
    public static float K(Control c) => Math.Max(1f, c.DeviceDpi / 96f);

    public static GraphicsPath Round(RectangleF r, float rad)
    {
        var p = new GraphicsPath();
        var d = Math.Max(1f, rad * 2);
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }

    public static void Smooth(Graphics g) => g.SmoothingMode = SmoothingMode.AntiAlias;
}

internal class DbTable : TableLayoutPanel
{
    public DbTable() { DoubleBuffered = true; BackColor = Theme.Bg; Margin = Padding.Empty; }
}

internal sealed class DbFlow : FlowLayoutPanel
{
    public DbFlow() { DoubleBuffered = true; BackColor = Theme.Bg; Margin = Padding.Empty; }
}

/// <summary>Icona tonda "i": passando sopra con il mouse compare la spiegazione.</summary>
internal sealed class InfoIcon : Control
{
    private bool _hot;
    private readonly Font _f = Theme.Font(8.5f, FontStyle.Bold);

    public InfoIcon()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        Cursor = Cursors.Help;
        BackColor = Theme.Panel;
        Size = new Size(20, 20);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        var s = (int)(20 * Theme.K(this));
        Size = new Size(s, s);
    }

    protected override void OnMouseEnter(EventArgs e) { _hot = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hot = false; Invalidate(); base.OnMouseLeave(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        Theme.Smooth(g);
        g.Clear(BackColor);
        var r = new RectangleF(1, 1, Width - 3, Height - 3);
        using (var fill = new SolidBrush(_hot ? Theme.Accent : Color.Transparent)) g.FillEllipse(fill, r);
        using (var pen = new Pen(_hot ? Theme.Accent : Theme.Dim, 1.5f)) g.DrawEllipse(pen, r);
        TextRenderer.DrawText(g, "i", _f, new Rectangle(0, 0, Width, Height), _hot ? Theme.Bg : Theme.Dim,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
    }
}

/// <summary>Una voce di impostazione su una "scheda": titolo, icona info, descrizione breve, valore e cursore.</summary>
internal sealed class OptionRow : Control
{
    private readonly Font _cap = Theme.Font(10.5f, FontStyle.Bold);
    private readonly Font _val = Theme.Font(10.5f, FontStyle.Bold);
    private readonly Font _sub = Theme.Font(9f);
    private readonly InfoIcon _info = new();
    private bool _drag;

    public string Caption { get; }
    public string Sub { get; }
    public string Note { get; private set; } = "";
    public double Min { get; }
    public double Max { get; }
    public double Value { get; private set; }
    public Func<double, string> Fmt { get; }
    public bool Applies { get; private set; } = true;
    public event Action<double>? Changed;

    public OptionRow(string caption, string help, double min, double max, double value, Func<double, string> fmt, ToolTip tip)
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        Caption = caption; Min = min; Max = max; Value = Math.Clamp(value, min, max); Fmt = fmt;
        var cut = help.IndexOfAny(new[] { '.', '\n' });
        Sub = cut > 0 ? help[..cut].Trim() : help;
        BackColor = Theme.Bg;
        Height = 86;
        Controls.Add(_info);
        tip.SetToolTip(_info, help);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        Height = (int)(86 * Theme.K(this));
        var k = Theme.K(this);
        _info.Left = (int)(18 * k) + TextRenderer.MeasureText(Caption, _cap, new Size(int.MaxValue, 0), TextFormatFlags.NoPadding).Width + (int)(8 * k);
        _info.Top = (int)(9 * k);
    }

    public void SetApplies(bool applies, string note)
    {
        Applies = applies; Note = note;
        Invalidate();
    }

    private RectangleF Track
    {
        get
        {
            var k = Theme.K(this);
            return new RectangleF(18 * k, Height - 24 * k, Width - 36 * k, 6 * k);
        }
    }

    private void SetFromX(int x)
    {
        var t = Track;
        var f = Math.Clamp((x - t.X) / Math.Max(1.0, t.Width), 0, 1);
        var v = Min + f * (Max - Min);
        var range = Max - Min;
        var step = range > 20 ? 1.0 : range > 3 ? 0.1 : 0.01;
        v = Math.Round(v / step) * step;
        if (Math.Abs(v - Value) < 1e-9) return;
        Value = Math.Clamp(v, Min, Max);
        Invalidate();
        Changed?.Invoke(Value);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left && e.Y > Height / 2 - 4) { _drag = true; Capture = true; SetFromX(e.X); }
        base.OnMouseDown(e);
    }
    protected override void OnMouseMove(MouseEventArgs e) { if (_drag) SetFromX(e.X); base.OnMouseMove(e); }
    protected override void OnMouseUp(MouseEventArgs e) { _drag = false; Capture = false; base.OnMouseUp(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        Theme.Smooth(g);
        g.Clear(Theme.Bg);
        var k = Theme.K(this);
        var dim = !Applies;

        using (var card = Theme.Round(new RectangleF(0.5f, 0.5f, Width - 2, Height - 2), 10 * k))
        {
            using var b = new SolidBrush(Theme.Panel); g.FillPath(b, card);
            using var p = new Pen(Theme.Line); g.DrawPath(p, card);
        }

        var textCol = dim ? Theme.Dim : Theme.Text;
        TextRenderer.DrawText(g, Caption, _cap, new Point((int)(18 * k), (int)(10 * k)), textCol, TextFormatFlags.NoPadding);
        if (Note.Length > 0)
            TextRenderer.DrawText(g, Note, _sub, new Point(_info.Right + (int)(10 * k), (int)(12 * k)), Theme.Magenta, TextFormatFlags.NoPadding);
        TextRenderer.DrawText(g, Sub, _sub, new Rectangle((int)(18 * k), (int)(36 * k), Width - (int)(36 * k), (int)(20 * k)), Theme.Dim,
            TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);

        var val = Fmt(Value);
        var sz = TextRenderer.MeasureText(val, _val, new Size(int.MaxValue, 0), TextFormatFlags.NoPadding);
        TextRenderer.DrawText(g, val, _val, new Point(Width - sz.Width - (int)(18 * k), (int)(10 * k)), dim ? Theme.Dim : Theme.Accent, TextFormatFlags.NoPadding);

        var t = Track;
        using (var back = Theme.Round(t, 3 * k)) using (var b = new SolidBrush(Theme.Line)) g.FillPath(b, back);
        var f = (float)((Value - Min) / (Max - Min));
        var fr = new RectangleF(t.X, t.Y, Math.Max(6f * k, t.Width * f), t.Height);
        var c1 = dim ? Theme.Dim : Theme.Accent;
        var c2 = dim ? Theme.Dim : Theme.Cyan;
        using (var fp = Theme.Round(fr, 3 * k))
        using (var lg = new LinearGradientBrush(new RectangleF(fr.X - 1, fr.Y, fr.Width + 2, fr.Height), c1, c2, 0f))
            g.FillPath(lg, fp);
        var cx = fr.Right; var cy = t.Y + t.Height / 2;
        var r = 7 * k;
        using (var glow = new SolidBrush(Color.FromArgb(dim ? 20 : 60, c1))) g.FillEllipse(glow, cx - r * 1.7f, cy - r * 1.7f, r * 3.4f, r * 3.4f);
        using (var th = new SolidBrush(Theme.Text)) g.FillEllipse(th, cx - r, cy - r, r * 2, r * 2);
        using (var ring = new Pen(c1, 2f * k)) g.DrawEllipse(ring, cx - r, cy - r, r * 2, r * 2);
    }
}

/// <summary>Scheda di scelta dello stile con una mini anteprima.</summary>
internal sealed class StyleCard : Control
{
    private readonly Font _t = Theme.Font(13f, FontStyle.Bold);
    private readonly Font _s = Theme.Font(9.5f);
    private bool _hot;
    public string Kind { get; }
    public string Title { get; }
    public string Sub { get; }
    public bool Selected { get; set; }

    public StyleCard(string kind, string title, string sub)
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        Kind = kind; Title = title; Sub = sub;
        Cursor = Cursors.Hand; BackColor = Theme.Bg;
    }

    protected override void OnMouseEnter(EventArgs e) { _hot = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hot = false; Invalidate(); base.OnMouseLeave(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        Theme.Smooth(g);
        g.Clear(Theme.Bg);
        var k = Theme.K(this);
        var r = new RectangleF(1, 1, Width - 3, Height - 3);
        using (var path = Theme.Round(r, 12 * k))
        {
            using (var b = new SolidBrush(Selected ? Color.FromArgb(22, 36, 22) : (_hot ? Theme.PanelHot : Theme.Panel))) g.FillPath(b, path);
            using var pen = new Pen(Selected ? Theme.Accent : Theme.Line, Selected ? 2.2f : 1f);
            g.DrawPath(pen, path);
        }
        var pad = (int)(18 * k);
        TextRenderer.DrawText(g, Title, _t, new Point(pad, (int)(12 * k)), Selected ? Theme.Accent : Theme.Text, TextFormatFlags.NoPadding);
        TextRenderer.DrawText(g, Sub, _s, new Rectangle(pad, (int)(40 * k), Width - 2 * pad, (int)(22 * k)), Theme.Dim, TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);

        // mini anteprima: 18 tasti
        const int n = 18;
        var area = Width - 2f * pad;
        var kw = area / n;
        var ky = Height - 24 * k;
        for (var i = 0; i < n; i++)
        {
            Color c;
            var f = i / (float)(n - 1);
            if (Kind == "barrier")
            {
                if (i == 8 || i == 9) c = Color.FromArgb(30, 30, 34);
                else c = i < 8 ? Theme.Cyan : Theme.Magenta;
            }
            else c = Blend(Theme.Cyan, Theme.Magenta, f);
            using var br = new SolidBrush(c);
            g.FillRectangle(br, pad + i * kw, ky, kw - 3 * k, 9 * k);
        }
    }

    private static Color Blend(Color a, Color b, float t)
        => Color.FromArgb((int)(a.R + (b.R - a.R) * t), (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t));
}

/// <summary>Anteprima dal vivo dei colori che stanno andando sulla tastiera.</summary>
internal sealed class KeyPreview : Control
{
    private readonly byte[] _buf = new byte[512 * 3];
    private readonly Font _f = Theme.Font(9.5f);

    public KeyPreview()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        BackColor = Theme.Bg;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        Theme.Smooth(g);
        g.Clear(Theme.Bg);
        var k = Theme.K(this);
        var frame = new RectangleF(0.5f, 0.5f, Width - 2, Height - 2);
        using (var path = Theme.Round(frame, 12 * k))
        {
            using var b = new SolidBrush(Color.FromArgb(7, 7, 8)); g.FillPath(b, path);
            using var pen = new Pen(Theme.Line); g.DrawPath(pen, path);
        }

        var n = Live.Snapshot(out var xs, out var ys, _buf);
        if (n == 0)
        {
            TextRenderer.DrawText(g, Live.Status, _f, ClientRectangle, Theme.Dim, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            return;
        }

        var pad = 16f * k;
        var w = Width - pad * 2; var h = Height - pad * 2;
        // 20 colonne x 7 righe: i tasti restano quadrati
        var kw = Math.Min(w / 21f, h / 7.6f);
        var gw = kw * 20.4f; var gh = kw * 7.0f;
        var ox = (Width - gw) / 2f; var oy = (Height - gh) / 2f;
        for (var i = 0; i < n; i++)
        {
            var cx = ox + xs[i] * (gw - kw);
            var cy = oy + ys[i] * (gh - kw);
            var c = Color.FromArgb(_buf[i * 3], _buf[i * 3 + 1], _buf[i * 3 + 2]);
            using (var glow = new SolidBrush(Color.FromArgb(38, c))) g.FillEllipse(glow, cx - kw * 0.25f, cy - kw * 0.25f, kw * 1.5f, kw * 1.5f);
            using var br = new SolidBrush(c);
            using var kp = Theme.Round(new RectangleF(cx + 1, cy + 1, kw - 2, kw - 2), Math.Max(2f, kw * 0.18f));
            g.FillPath(br, kp);
        }
    }
}

/// <summary>Voce della barra laterale (macro area).</summary>
internal sealed class NavButton : Control
{
    private readonly Font _f = Theme.Font(10.5f, FontStyle.Bold);
    private readonly Font _s = Theme.Font(8.5f);
    private bool _hot;
    public bool Selected { get; set; }
    public string Sub { get; }

    public NavButton(string text, string sub)
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        Text = text; Sub = sub; Cursor = Cursors.Hand; BackColor = Theme.Side;
        Height = 50;
    }

    protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); Height = (int)(50 * Theme.K(this)); }
    protected override void OnMouseEnter(EventArgs e) { _hot = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hot = false; Invalidate(); base.OnMouseLeave(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        var k = Theme.K(this);
        g.Clear(Selected ? Theme.PanelHot : (_hot ? Color.FromArgb(21, 21, 25) : Theme.Side));
        if (Selected)
        {
            using var b = new SolidBrush(Theme.Accent);
            g.FillRectangle(b, 0, 0, (int)(4 * k), Height);
        }
        TextRenderer.DrawText(g, Text, _f, new Point((int)(22 * k), (int)(7 * k)), Selected ? Theme.Accent : Theme.Text, TextFormatFlags.NoPadding);
        TextRenderer.DrawText(g, Sub, _s, new Point((int)(22 * k), (int)(28 * k)), Theme.Dim, TextFormatFlags.NoPadding);
    }
}

internal sealed class MainForm : Form
{
    [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hwnd, int cmd);
    [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    private sealed record Opt(string Page, string Key, string Caption, double Min, double Max, Func<Config, double> Get, Action<Config, double> Set,
        Func<double, string> Fmt, string Help, string AppliesTo = "");

    private sealed record PageInfo(string Id, string Title, string Sub, string Heading, string Lead);

    private static readonly PageInfo[] PageDefs =
    {
        new("live", "Luci", "Anteprima e stile", "Luci dal vivo", "Quello che vedi qui e' quello che sta andando sulla tastiera. Scegli lo stile dell'onda."),
        new("wave", "Onda", "Cambio finestra", "Onda al cambio finestra", "Come si propaga l'effetto dal centro della tastiera quando cambi finestra."),
        new("window", "Finestra", "Colori e fluidita'", "Colori della finestra attiva", "Quanto e come i colori della finestra aperta influenzano la tastiera."),
        new("look", "Aspetto", "Resa dei colori", "Aspetto delle luci", "Luminosita', intensita' e profondita' dei colori sui LED."),
        new("desk", "Desktop", "Movimento e velocita'", "Movimento dello sfondo", "Come scorre lo sfondo del desktop sulla tastiera e quanto e' fluido l'invio."),
    };

    private readonly Config _cfg;
    private readonly AppPaths _paths;
    private readonly ToolTip _tip = new();
    private readonly NotifyIcon _tray = new();
    private readonly Timer _saveTimer = new() { Interval = 400 };
    private readonly Timer _uiTimer = new() { Interval = 50 };
    private readonly Dictionary<string, string> _pending = new();
    private readonly List<(Opt opt, OptionRow row)> _rows = new();
    private readonly List<StyleCard> _cards = new();
    private readonly List<(PageInfo info, NavButton nav, Control page)> _pages = new();
    private readonly List<(DbFlow flow, List<OptionRow> rows)> _flows = new();
    private readonly ToolStripMenuItem _miSmooth = new(), _miBarrier = new(), _miAuto = new("Avvia con Windows");
    private readonly Font _small = Theme.Font(9.5f);
    private KeyPreview? _big, _mini;
    private Label _status = new();
    private Panel _dot = new();
    private bool _hideFirst;
    private bool _exiting;
    private bool _balloonShown;

    private static string F(double v) => v.ToString("0.####", CultureInfo.InvariantCulture);

    private IEnumerable<Opt> Options() => new[]
    {
        new Opt("wave", "WaveSeconds", "Durata dell'onda", 0.5, 6, c => c.WaveSeconds, (c, v) => c.WaveSeconds = v,
            v => $"{v:0.0} s", "Quanti secondi impiega l'onda a viaggiare dal centro ai bordi della tastiera quando cambi finestra.\n\nBasso = scatto rapido e vivace.\nAlto = onda lenta e scenografica."),
        new Opt("wave", "BarrierWidth", "Spessore della barriera", 0.05, 0.5, c => c.BarrierWidth, (c, v) => c.BarrierWidth = v,
            v => $"{v * 100:0}%", "Larghezza della fascia di tasti spenti che precede i nuovi colori, in percentuale della tastiera.\n\nBasso = linea sottile e netta.\nAlto = larga fascia scura.\n\nVale solo per lo stile Barrier.", "barrier"),
        new Opt("wave", "WaveBand", "Morbidezza del fronte", 0.1, 1, c => c.WaveBand, (c, v) => c.WaveBand = v,
            v => $"{v * 100:0}%", "Quanto e' sfumato il passaggio tra vecchi e nuovi colori.\n\nBasso = passaggio netto.\nAlto = dissolvenza molto morbida.\n\nVale solo per lo stile Smooth.", "smooth"),
        new Opt("wave", "WaveGlow", "Bagliore del fronte", 0, 1, c => c.WaveGlow, (c, v) => c.WaveGlow = v,
            v => $"{v * 100:0}%", "Quanto si illumina di bianco il fronte dell'onda mentre avanza. 0 = nessun bagliore.\n\nVale solo per lo stile Smooth.", "smooth"),

        new Opt("window", "WindowInfluence", "Influenza della finestra", 0, 1, c => c.WindowInfluence, (c, v) => c.WindowInfluence = v,
            v => $"{v * 100:0}%", "Quanto i colori della finestra attiva coprono quelli del desktop, finche' la finestra resta aperta.\n\n0% = li ignora sempre.\n30% = leggera tinta.\n100% = la tastiera prende i colori della finestra."),
        new Opt("window", "WindowFollowSeconds", "Fluidita' dei colori", 0, 6, c => c.WindowFollowSeconds, (c, v) => c.WindowFollowSeconds = v,
            v => v < 0.05 ? "immediato" : $"{v:0.0} s", "Quanto dolcemente le luci inseguono i colori della finestra quando il suo contenuto cambia (scorrimento, video, pagine).\n\n0 = reagisce subito ma puo' risultare a scatti.\nAlto = transizioni molto fluide, con un po' di ritardo nel cambio colore."),
        new Opt("window", "ChromaThreshold", "Soglia colore", 0, 0.6, c => c.ChromaThreshold, (c, v) => c.ChromaThreshold = v,
            v => $"{v:0.00}", "Sotto questa vivacita' i pixel della finestra sono considerati senza colore (grigi, bianchi) e ignorati.\n\nAlzala per ignorare anche i colori pastello."),
        new Opt("window", "ValueThreshold", "Soglia luminosita'", 0, 0.5, c => c.ValueThreshold, (c, v) => c.ValueThreshold = v,
            v => $"{v:0.00}", "Sotto questa luminosita' i pixel della finestra (nero, sfondi scuri) sono ignorati.\n\nSe la finestra e' nera, la tastiera resta sui colori del desktop."),
        new Opt("window", "WindowSampleMs", "Frequenza di lettura", 50, 2000, c => c.WindowSampleMs, (c, v) => c.WindowSampleMs = (int)v,
            v => $"{v:0} ms", "Ogni quanti millisecondi viene letta la finestra attiva.\n\nBasso = reagisce prima ma usa piu' CPU.\nAlto = piu' leggero."),

        new Opt("look", "Brightness", "Luminosita'", 0.1, 1, c => c.Brightness, (c, v) => c.Brightness = v,
            v => $"{v * 100:0}%", "Luminosita' generale di tutti i tasti."),
        new Opt("look", "Saturation", "Saturazione", 0, 2.5, c => c.Saturation, (c, v) => c.Saturation = v,
            v => $"{v:0.00}", "Intensita' dei colori.\n\n0 = bianco e nero.\n1 = colori originali.\nPiu' di 1 = colori piu' accesi."),
        new Opt("look", "Gamma", "Profondita' (gamma)", 0.6, 3, c => c.Gamma, (c, v) => c.Gamma = v,
            v => $"{v:0.0}", "Alto = colori piu' profondi e meno slavati sui LED, ma piu' scuri.\nBasso = colori piu' chiari e pastello."),

        new Opt("desk", "DriftSpeed", "Velocita' dello sfondo", 0, 4, c => c.DriftSpeed, (c, v) => c.DriftSpeed = v,
            v => v < 0.05 ? "fermo" : $"{v:0.0}x", "Quanto velocemente l'immagine del desktop scorre e ondeggia sui tasti.\n\n0 = ferma.\n1 = lenta (predefinito).\n4 = veloce."),
        new Opt("desk", "Shimmer", "Sfarfallio tra tasti", 0, 0.6, c => c.Shimmer, (c, v) => c.Shimmer = v,
            v => $"{v * 100:0}%", "Piccole variazioni casuali di luminosita' da un tasto all'altro, per un aspetto piu' vivo."),
        new Opt("desk", "RandomRippleEverySeconds", "Onde casuali di luce", 0, 60, c => c.RandomRippleEverySeconds, (c, v) => c.RandomRippleEverySeconds = v,
            v => v < 1 ? "mai" : $"ogni {v:0} s", "Ogni quanti secondi (in media) compare una piccola onda di luce casuale.\n\n0 = mai."),
        new Opt("desk", "Fps", "Fotogrammi al secondo", 5, 40, c => c.Fps, (c, v) => c.Fps = (int)v,
            v => $"{v:0} fps", "Quante volte al secondo vengono inviati i colori alla tastiera.\n\nAlto = movimento piu' fluido.\nBasso = meno carico, ma a scatti."),
    };

    public MainForm(Config cfg, AppPaths paths, bool startHidden)
    {
        _cfg = cfg; _paths = paths; _hideFirst = startHidden;
        AutoScaleDimensions = new SizeF(96f, 96f);
        AutoScaleMode = AutoScaleMode.Dpi;
        Text = "LegionChromaFlow";
        BackColor = Theme.Bg;
        ForeColor = Theme.Text;
        Font = Theme.Font(9f);
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        StartPosition = FormStartPosition.CenterScreen;
        DoubleBuffered = true;
        Icon = MakeIcon();

        _tip.OwnerDraw = true;
        _tip.InitialDelay = 120;
        _tip.AutoPopDelay = 30000;
        _tip.ReshowDelay = 100;
        _tip.Popup += (_, e) =>
        {
            var text = _tip.GetToolTip(e.AssociatedControl!);
            var k = Theme.K(this);
            var s = TextRenderer.MeasureText(text, _small, new Size((int)(340 * k), 0), TextFormatFlags.WordBreak);
            e.ToolTipSize = new Size(s.Width + (int)(24 * k), s.Height + (int)(20 * k));
        };
        _tip.Draw += (_, e) =>
        {
            var g = e.Graphics;
            var k = Theme.K(this);
            using (var b = new SolidBrush(Color.FromArgb(28, 28, 32))) g.FillRectangle(b, e.Bounds);
            using (var p = new Pen(Theme.Accent)) g.DrawRectangle(p, 0, 0, e.Bounds.Width - 1, e.Bounds.Height - 1);
            TextRenderer.DrawText(g, e.ToolTipText, _small, new Rectangle((int)(12 * k), (int)(10 * k), e.Bounds.Width - (int)(24 * k), e.Bounds.Height - (int)(20 * k)),
                Theme.Text, TextFormatFlags.WordBreak);
        };

        // Ordine di inserimento: il riempimento per primo, poi i bordi.
        var content = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Bg, Padding = new Padding(28, 22, 28, 18) };
        var side = BuildSidebar();
        var top = BuildTopBar();
        Controls.Add(content);
        Controls.Add(side);
        Controls.Add(top);
        BuildPages(content);

        BuildTray();
        SelectStyle(_cfg.WaveStyle, save: false);
        SelectPage("live");

        ClientSize = new Size(1180, 760);
        MinimumSize = new Size((int)(860 * DeviceDpi / 96f), (int)(640 * DeviceDpi / 96f));

        _saveTimer.Tick += (_, _) => { _saveTimer.Stop(); Flush(); };
        _uiTimer.Tick += (_, _) =>
        {
            if (!Visible) return;
            _big?.Invalidate(); _mini?.Invalidate();
            _status.Text = Live.Status;
            var ok = Live.Status.StartsWith("Effetto") || Live.Status.StartsWith("Tastiera collegata");
            var c = ok ? Theme.Accent : Theme.Magenta;
            if (_dot.BackColor != c) _dot.BackColor = c;
        };
        _uiTimer.Start();
        Resize += (_, _) => FitRows();
        _ = Handle; // serve per poter usare BeginInvoke anche se parte nascosto
    }

    // ------------------------------------------------------------ costruzione interfaccia

    private Control BuildTopBar()
    {
        var bar = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = Theme.Bg };
        var title = new Label { Text = "LEGION CHROMAFLOW", Font = Theme.Font(17f, FontStyle.Bold), ForeColor = Theme.Accent, AutoSize = true, Location = new Point(24, 12), BackColor = Theme.Bg };
        _dot = new Panel { Size = new Size(10, 10), BackColor = Theme.Magenta };
        _status = new Label { Font = _small, ForeColor = Theme.Dim, AutoSize = true, BackColor = Theme.Bg, Text = Live.Status };
        bar.Controls.AddRange(new Control[] { title, _dot, _status });
        void Place()
        {
            var k = Theme.K(bar);
            bar.Height = (int)(64 * k);
            title.Location = new Point((int)(24 * k), (int)(12 * k));
            var tw = TextRenderer.MeasureText(_status.Text, _status.Font, new Size(int.MaxValue, 0), TextFormatFlags.NoPadding).Width + (int)(12 * k);
            _status.AutoSize = false;
            _status.Size = new Size(tw, (int)(24 * k));
            _status.TextAlign = ContentAlignment.MiddleRight;
            _status.Location = new Point(bar.Width - tw - (int)(28 * k), (int)(20 * k));
            _dot.Size = new Size((int)(10 * k), (int)(10 * k));
            _dot.Location = new Point(_status.Left - _dot.Width - (int)(8 * k), (int)(28 * k));
        }
        bar.Resize += (_, _) => Place();
        bar.HandleCreated += (_, _) => Place();
        _status.TextChanged += (_, _) => Place();
        bar.Paint += (_, e) =>
        {
            var k = Theme.K(bar);
            var h = Math.Max(2, (int)(2 * k));
            using var lg = new LinearGradientBrush(new Rectangle(0, bar.Height - h - 1, Math.Max(1, bar.Width), h + 2), Theme.Accent, Theme.Cyan, 0f);
            e.Graphics.FillRectangle(lg, 0, bar.Height - h, bar.Width, h);
        };
        return bar;
    }

    private Control BuildSidebar()
    {
        var side = new Panel { Dock = DockStyle.Left, Width = 230, BackColor = Theme.Side };
        side.HandleCreated += (_, _) => side.Width = (int)(230 * Theme.K(side));

        // in basso: mini anteprima + impostazioni generali
        var bottom = new Panel { Dock = DockStyle.Bottom, BackColor = Theme.Side, Height = 250 };
        bottom.HandleCreated += (_, _) => bottom.Height = (int)(232 * Theme.K(bottom));
        _mini = new KeyPreview();

        var auto = new CheckBox { Text = "Avvia con Windows", ForeColor = Theme.Text, BackColor = Theme.Side, AutoSize = true, Cursor = Cursors.Hand, Checked = AutostartEnabled(), Font = Theme.Font(9.5f) };
        auto.CheckedChanged += (_, _) => { SetAutostart(auto.Checked); _miAuto.Checked = auto.Checked; };
        _miAuto.CheckOnClick = true; _miAuto.Checked = auto.Checked;
        _miAuto.CheckedChanged += (_, _) => auto.Checked = _miAuto.Checked;

        var hint = new Label
        {
            Text = "Trascina l'icona dall'area ^ vicino all'orologio sulla barra per averla sempre visibile.",
            Font = Theme.Font(8.5f), ForeColor = Theme.Dim, BackColor = Theme.Side, AutoSize = false
        };

        bottom.Controls.AddRange(new Control[] { _mini, auto, hint });
        void PlaceBottom()
        {
            var k = Theme.K(bottom);
            var m = (int)(14 * k);
            _mini.SetBounds(m, m, Math.Max(10, bottom.Width - 2 * m), (int)(96 * k));
            auto.Location = new Point(m, _mini.Bottom + (int)(12 * k));
            hint.SetBounds(m, auto.Bottom + (int)(6 * k), Math.Max(10, bottom.Width - 2 * m), (int)(52 * k));
        }
        bottom.Resize += (_, _) => PlaceBottom();
        bottom.HandleCreated += (_, _) => PlaceBottom();

        var nav = new DbFlow { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = Theme.Side };
        nav.Padding = new Padding(0, 10, 0, 0);
        foreach (var info in PageDefs)
        {
            var b = new NavButton(info.Title, info.Sub) { Margin = Padding.Empty };
            var id = info.Id;
            b.Click += (_, _) => SelectPage(id);
            nav.Controls.Add(b);
            _pages.Add((info, b, null!));
        }
        nav.Resize += (_, _) => { foreach (Control c in nav.Controls) c.Width = nav.ClientSize.Width; };

        side.Controls.Add(nav);
        side.Controls.Add(bottom);
        return side;
    }

    private void BuildPages(Panel content)
    {
        for (var i = 0; i < _pages.Count; i++)
        {
            var info = _pages[i].info;
            var inner = info.Id == "live" ? BuildLivePage(info) : BuildOptionsPage(info);
            inner.Dock = DockStyle.Fill;
            inner.Visible = false;
            content.Controls.Add(inner);
            _pages[i] = (info, _pages[i].nav, inner);
        }
    }

    private Control Heading(PageInfo info)
    {
        var p = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Bg, Margin = Padding.Empty };
        var h = new Label { Text = info.Heading, Font = Theme.Font(18f, FontStyle.Bold), ForeColor = Theme.Text, AutoSize = true, Location = new Point(0, 0), BackColor = Theme.Bg };
        var l = new Label { Text = info.Lead, Font = Theme.Font(10f), ForeColor = Theme.Dim, AutoSize = true, BackColor = Theme.Bg };
        p.Controls.AddRange(new Control[] { h, l });
        void Place() { var k = Theme.K(p); l.Location = new Point(0, h.Bottom + (int)(2 * k)); }
        p.Resize += (_, _) => Place();
        p.HandleCreated += (_, _) => Place();
        return p;
    }

    private Control BuildLivePage(PageInfo info)
    {
        var t = new DbTable { ColumnCount = 1, RowCount = 5 };
        t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        t.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        t.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        t.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        t.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
        t.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
        t.HandleCreated += (_, _) =>
        {
            var k = Theme.K(t);
            t.RowStyles[0].Height = 78 * k; t.RowStyles[2].Height = 40 * k; t.RowStyles[3].Height = 112 * k; t.RowStyles[4].Height = 70 * k;
        };

        t.Controls.Add(Heading(info), 0, 0);

        _big = new KeyPreview { Dock = DockStyle.Fill, Margin = new Padding(0, 4, 0, 10) };
        t.Controls.Add(_big, 0, 1);

        var lab = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Bg, Margin = Padding.Empty };
        var lt = new Label { Text = "STILE DELL'ONDA", Font = Theme.Font(9.5f, FontStyle.Bold), ForeColor = Theme.Dim, AutoSize = true, BackColor = Theme.Bg };
        var ii = new InfoIcon { BackColor = Theme.Bg };
        _tip.SetToolTip(ii, "SMOOTH: i nuovi colori si sciolgono dolcemente dal centro verso l'esterno, con un bagliore sul fronte.\n\nBARRIER: una fascia sottile di tasti spenti attraversa la tastiera dal centro. Dietro la fascia compaiono subito i nuovi colori, davanti restano i vecchi.");
        lab.Controls.AddRange(new Control[] { lt, ii });
        void PlaceLab() { var k = Theme.K(lab); lt.Location = new Point(0, (int)(12 * k)); ii.Location = new Point(lt.Right + (int)(8 * k), (int)(10 * k)); }
        lab.Resize += (_, _) => PlaceLab();
        lab.HandleCreated += (_, _) => PlaceLab();
        t.Controls.Add(lab, 0, 2);

        var cards = new DbTable { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        cards.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var smooth = new StyleCard("smooth", "SMOOTH", "Dissolvenza morbida con bagliore") { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 8, 0) };
        var barrier = new StyleCard("barrier", "BARRIER", "Fronte netto con fascia di tasti spenti") { Dock = DockStyle.Fill, Margin = new Padding(8, 0, 0, 0) };
        foreach (var c in new[] { smooth, barrier })
        {
            c.Click += (_, _) => SelectStyle(c.Kind, save: true);
            _cards.Add(c);
        }
        cards.Controls.Add(smooth, 0, 0);
        cards.Controls.Add(barrier, 1, 0);
        t.Controls.Add(cards, 0, 3);

        var wave = new Button
        {
            Text = "ANTEPRIMA ONDA", FlatStyle = FlatStyle.Flat, BackColor = Theme.Accent, ForeColor = Theme.Bg,
            Font = Theme.Font(10.5f, FontStyle.Bold), Cursor = Cursors.Hand, Size = new Size(230, 44)
        };
        wave.FlatAppearance.BorderSize = 0;
        wave.Click += (_, _) => Live.RequestWave();
        _tip.SetToolTip(wave, "Lancia subito l'onda con i colori dell'ultima finestra attiva, senza dover cambiare finestra. Utile per provare le impostazioni.");
        var brow = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Bg, Margin = Padding.Empty };
        brow.Controls.Add(wave);
        void PlaceBtn()
        {
            var k = Theme.K(brow);
            wave.Size = new Size((int)(230 * k), (int)(44 * k));
            wave.Location = new Point(0, Math.Max(0, (brow.Height - wave.Height) / 2));
        }
        brow.Resize += (_, _) => PlaceBtn();
        brow.HandleCreated += (_, _) => PlaceBtn();
        t.Controls.Add(brow, 0, 4);
        return t;
    }

    private Control BuildOptionsPage(PageInfo info)
    {
        var t = new DbTable { ColumnCount = 1, RowCount = 2 };
        t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        t.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        t.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        t.HandleCreated += (_, _) => t.RowStyles[0].Height = 78 * Theme.K(t);
        t.Controls.Add(Heading(info), 0, 0);

        var flow = new DbFlow { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true };
        flow.MouseEnter += (_, _) => flow.Focus();
        var rows = new List<OptionRow>();
        foreach (var o in Options().Where(x => x.Page == info.Id))
        {
            var row = new OptionRow(o.Caption, o.Help, o.Min, o.Max, o.Get(_cfg), o.Fmt, _tip) { Margin = new Padding(0, 0, 0, 10) };
            var opt = o;
            row.Changed += v => { opt.Set(_cfg, v); _cfg.Clamp(); Queue(opt.Key, F(opt.Get(_cfg))); };
            row.MouseEnter += (_, _) => flow.Focus();
            flow.Controls.Add(row);
            rows.Add(row);
            _rows.Add((o, row));
        }
        flow.Resize += (_, _) => FitRows();
        _flows.Add((flow, rows));
        t.Controls.Add(flow, 0, 1);
        return t;
    }

    private void FitRows()
    {
        foreach (var (flow, rows) in _flows)
        {
            var w = flow.ClientSize.Width - (int)(6 * Theme.K(flow));
            if (w < 100) continue;
            w = Math.Min(w, (int)(980 * Theme.K(flow)));
            foreach (var r in rows) r.Width = w;
        }
    }

    private void SelectPage(string id)
    {
        foreach (var (info, nav, page) in _pages)
        {
            var on = info.Id == id;
            nav.Selected = on; nav.Invalidate();
            page.Visible = on;
        }
        FitRows();
    }

    private void RefreshApplies()
    {
        foreach (var (o, row) in _rows)
        {
            if (o.AppliesTo.Length == 0) continue;
            var ok = _cfg.WaveStyle == o.AppliesTo;
            row.SetApplies(ok, ok ? "" : $"solo stile {o.AppliesTo.ToUpperInvariant()}");
        }
    }

    private void SelectStyle(string kind, bool save)
    {
        kind = kind == "barrier" ? "barrier" : "smooth";
        _cfg.WaveStyle = kind;
        foreach (var c in _cards) { c.Selected = c.Kind == kind; c.Invalidate(); }
        _miSmooth.Checked = kind == "smooth";
        _miBarrier.Checked = kind == "barrier";
        _tray.Text = $"LegionChromaFlow - {kind}";
        RefreshApplies();
        if (save) Queue("WaveStyle", $"\"{kind}\"");
    }

    // ------------------------------------------------------------ salvataggio

    private void Queue(string key, string value)
    {
        _pending[key] = value;
        _saveTimer.Stop(); _saveTimer.Start();
    }

    private void Flush()
    {
        if (_pending.Count == 0) return;
        Config.SaveValues(_paths.ConfigPath, new Dictionary<string, string>(_pending));
        _pending.Clear();
    }

    // ------------------------------------------------------------ avvio automatico

    private string StartupLink => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "LegionChromaFlow.lnk");

    private bool AutostartEnabled() => File.Exists(StartupLink);

    private void SetAutostart(bool on)
    {
        try
        {
            if (!on) { if (File.Exists(StartupLink)) File.Delete(StartupLink); return; }
            dynamic sh = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell")!)!;
            var l = sh.CreateShortcut(StartupLink);
            l.TargetPath = "wscript.exe";
            l.Arguments = $"\"{Path.Combine(_paths.Root, "run-hidden.vbs")}\"";
            l.WorkingDirectory = _paths.Root;
            l.Save();
        }
        catch (Exception ex) { Log.Warn($"Avvio automatico non modificato: {ex.Message}"); }
    }

    // ------------------------------------------------------------ tray

    private void BuildTray()
    {
        var menu = new ContextMenuStrip { Renderer = new ToolStripProfessionalRenderer(new DarkColors()), BackColor = Color.FromArgb(24, 24, 28), ForeColor = Theme.Text, ShowImageMargin = false, Font = Theme.Font(9.5f) };
        var open = new ToolStripMenuItem("Apri impostazioni") { Font = Theme.Font(9.5f, FontStyle.Bold) };
        open.Click += (_, _) => ShowPanel();

        var style = new ToolStripMenuItem("Stile onda");
        _miSmooth.Text = "Smooth - dissolvenza morbida";
        _miBarrier.Text = "Barrier - fascia di tasti spenti";
        _miSmooth.Click += (_, _) => SelectStyle("smooth", true);
        _miBarrier.Click += (_, _) => SelectStyle("barrier", true);
        style.DropDownItems.AddRange(new ToolStripItem[] { _miSmooth, _miBarrier });

        var wave = new ToolStripMenuItem("Anteprima onda");
        wave.Click += (_, _) => Live.RequestWave();
        var exit = new ToolStripMenuItem("Esci (ripristina le luci)");
        exit.Click += (_, _) => ExitApp();

        menu.Items.AddRange(new ToolStripItem[] { open, new ToolStripSeparator(), style, wave, _miAuto, new ToolStripSeparator(), exit });
        foreach (ToolStripItem i in style.DropDownItems) { i.BackColor = Color.FromArgb(24, 24, 28); i.ForeColor = Theme.Text; }

        _tray.Icon = Icon;
        _tray.ContextMenuStrip = menu;
        _tray.Text = "LegionChromaFlow";
        _tray.Visible = true;
        _tray.MouseClick += (_, e) =>
        {
            if (e.Button != MouseButtons.Left) return;
            if (Visible && WindowState != FormWindowState.Minimized) Hide(); else ShowPanel();
        };
    }

    private sealed class DarkColors : ProfessionalColorTable
    {
        public override Color MenuItemSelected => Color.FromArgb(40, 52, 40);
        public override Color MenuItemBorder => Theme.Accent;
        public override Color MenuBorder => Theme.Line;
        public override Color ToolStripDropDownBackground => Color.FromArgb(24, 24, 28);
        public override Color ImageMarginGradientBegin => Color.FromArgb(24, 24, 28);
        public override Color ImageMarginGradientMiddle => Color.FromArgb(24, 24, 28);
        public override Color ImageMarginGradientEnd => Color.FromArgb(24, 24, 28);
        public override Color SeparatorDark => Theme.Line;
        public override Color SeparatorLight => Theme.Line;
        public override Color MenuItemSelectedGradientBegin => Color.FromArgb(40, 52, 40);
        public override Color MenuItemSelectedGradientEnd => Color.FromArgb(40, 52, 40);
        public override Color MenuItemPressedGradientBegin => Color.FromArgb(40, 52, 40);
        public override Color MenuItemPressedGradientEnd => Color.FromArgb(40, 52, 40);
    }

    private static Icon MakeIcon()
    {
        using var bmp = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            using (var lg = new LinearGradientBrush(new Rectangle(0, 0, 32, 32), Theme.Accent, Theme.Magenta, 45f))
                g.FillEllipse(lg, 1, 1, 30, 30);
            using (var inner = new SolidBrush(Color.FromArgb(14, 14, 16))) g.FillEllipse(inner, 7, 7, 18, 18);
            using (var lg2 = new LinearGradientBrush(new Rectangle(0, 0, 32, 32), Theme.Cyan, Theme.Accent, 90f))
                g.FillEllipse(lg2, 11, 11, 10, 10);
        }
        return Icon.FromHandle(bmp.GetHicon());
    }

    // ------------------------------------------------------------ finestra

    public void ShowPanel()
    {
        _hideFirst = false;
        if (!Visible) Show();
        ShowWindow(Handle, 1);
        if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;
        Activate();
        BringToFront();
    }

    public void ExitApp()
    {
        if (_exiting) return;
        _exiting = true;
        Flush();
        _tray.Visible = false;
        Close();
    }

    protected override void SetVisibleCore(bool value)
    {
        if (_hideFirst) value = false;
        base.SetVisibleCore(value);
        // Avviato da un .vbs nascosto, Windows ignora il primo "mostra": lo ripeto esplicitamente.
        if (value && IsHandleCreated) ShowWindow(Handle, 1);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!_exiting && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
            if (!_balloonShown)
            {
                _balloonShown = true;
                _tray.ShowBalloonTip(3000, "LegionChromaFlow", "Continua a lavorare qui: clic sull'icona per riaprire il pannello.", ToolTipIcon.Info);
            }
            return;
        }
        base.OnFormClosing(e);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        var dark = 1;
        DwmSetWindowAttribute(Handle, 20, ref dark, 4);
        var cap = 0x0D0B0B; // COLORREF (BGR) del colore di sfondo
        DwmSetWindowAttribute(Handle, 35, ref cap, 4);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) { _tray.Dispose(); _saveTimer.Dispose(); _uiTimer.Dispose(); _tip.Dispose(); }
        base.Dispose(disposing);
    }
}

internal static class Gui
{
    public static int Run(AppPaths paths, bool startHidden)
    {
        using var mutex = new Mutex(true, Program.MutexName, out var created);
        if (!created)
        {
            // Esiste gia' un'istanza: le chiedo di mostrare il pannello.
            try { using var ev = EventWaitHandle.OpenExisting(Program.ShowEventName); ev.Set(); } catch { }
            return 0;
        }

        using var stop = new EventWaitHandle(false, EventResetMode.ManualReset, Program.StopEventName);
        using var show = new EventWaitHandle(false, EventResetMode.AutoReset, Program.ShowEventName);

        Desktop.EnableDpiAwareness();
        try { System.Diagnostics.Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.BelowNormal; } catch { }

        var cfg = Config.Load(paths.ConfigPath);
        Log.Info($"Avvio pannello. Config: {paths.ConfigPath}");

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        var form = new MainForm(cfg, paths, startHidden);

        var engine = new Thread(() =>
        {
            try { Program.RunLoop(cfg, stop, test: false); }
            catch (Exception ex) { Log.Error($"Motore luci terminato: {ex}"); }
        }) { IsBackground = true, Name = "LegionChromaFlow.Engine" };
        engine.Start();

        var watcher = new Thread(() =>
        {
            var handles = new WaitHandle[] { stop, show };
            while (true)
            {
                var i = WaitHandle.WaitAny(handles);
                try
                {
                    if (i == 0) { form.BeginInvoke(form.ExitApp); return; }
                    form.BeginInvoke(form.ShowPanel);
                }
                catch (ObjectDisposedException) { return; }
                catch (InvalidOperationException) { return; }
            }
        }) { IsBackground = true, Name = "LegionChromaFlow.Signals" };
        watcher.Start();

        Application.Run(form);

        stop.Set();
        engine.Join(8000);
        Log.Info("Terminato.");
        return 0;
    }
}
