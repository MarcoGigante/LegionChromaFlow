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
    public static readonly Color Accent = Color.FromArgb(68, 214, 44);   // "Razer" green
    public static readonly Color Cyan = Color.FromArgb(0, 200, 255);
    public static readonly Color Magenta = Color.FromArgb(255, 60, 200);

    public static Font Font(float size, FontStyle style = FontStyle.Regular) => new("Segoe UI", size, style, GraphicsUnit.Point);

    /// <summary>Monitor scale factor (1.0 = 96 dpi).</summary>
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

/// <summary>Round "i" icon: hovering it shows the explanation.</summary>
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

/// <summary>A setting on a "card": title, info icon, short description, value and slider.</summary>
internal sealed class OptionRow : Control
{
    private readonly Font _cap = Theme.Font(10.5f, FontStyle.Bold);
    private readonly Font _val = Theme.Font(10.5f, FontStyle.Bold);
    private readonly Font _sub = Theme.Font(9f);
    private readonly InfoIcon _info = new();
    private readonly ToolTip _tip;
    private readonly string _key;
    private bool _drag;

    public string Caption => L.T($"o.{_key}.c");
    public string Help => L.T($"o.{_key}.h");
    public string Sub
    {
        get
        {
            var help = Help;
            var cut = help.IndexOfAny(new[] { '.', '\n', '。' });
            return cut > 0 ? help[..cut].Trim() : help;
        }
    }
    public string Note { get; private set; } = "";
    public double Min { get; }
    public double Max { get; }
    public double Value { get; private set; }
    public Func<double, string> Fmt { get; }
    public bool Applies { get; private set; } = true;
    public event Action<double>? Changed;

    public OptionRow(string key, double min, double max, double value, Func<double, string> fmt, ToolTip tip)
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        _key = key; _tip = tip; Min = min; Max = max; Value = Math.Clamp(value, min, max); Fmt = fmt;
        BackColor = Theme.Bg;
        Height = 86;
        Controls.Add(_info);
        Relang();
    }

    /// <summary>Re-applies texts after a language change.</summary>
    public void Relang()
    {
        _tip.SetToolTip(_info, Help);
        if (IsHandleCreated) PlaceInfo();
        Invalidate();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        Height = (int)(86 * Theme.K(this));
        PlaceInfo();
    }

    private void PlaceInfo()
    {
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

/// <summary>Wave style card with a mini preview.</summary>
internal sealed class StyleCard : Control
{
    private readonly Font _t = Theme.Font(13f, FontStyle.Bold);
    private readonly Font _s = Theme.Font(9.5f);
    private bool _hot;
    public string Kind { get; }
    public string Title { get; }
    public string Sub => L.T($"card.{Kind}.sub");
    public bool Selected { get; set; }

    public StyleCard(string kind, string title)
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        Kind = kind; Title = title;
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

        // mini preview: 18 keys
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

/// <summary>Live preview of the colors currently going to the keyboard.</summary>
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
            TextRenderer.DrawText(g, Live.StatusText, _f, ClientRectangle, Theme.Dim, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            return;
        }

        var pad = 16f * k;
        var w = Width - pad * 2; var h = Height - pad * 2;
        // 20 columns x 7 rows: keys stay square
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

/// <summary>Sidebar entry (macro area).</summary>
internal sealed class NavButton : Control
{
    private readonly Font _f = Theme.Font(10.5f, FontStyle.Bold);
    private readonly Font _s = Theme.Font(8.5f);
    private bool _hot;
    public bool Selected { get; set; }
    public string PageId { get; }
    private string Title => L.T($"p.{PageId}.t");
    private string Sub => L.T($"p.{PageId}.s");

    public NavButton(string id)
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        PageId = id; Cursor = Cursors.Hand; BackColor = Theme.Side;
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
        TextRenderer.DrawText(g, Title, _f, new Point((int)(22 * k), (int)(7 * k)), Selected ? Theme.Accent : Theme.Text, TextFormatFlags.NoPadding);
        TextRenderer.DrawText(g, Sub, _s, new Point((int)(22 * k), (int)(28 * k)), Theme.Dim, TextFormatFlags.NoPadding);
    }
}

/// <summary>Language button in the top bar.</summary>
internal sealed class LangButton : Control
{
    private readonly Font _f = Theme.Font(9.5f, FontStyle.Bold);
    private bool _hot;

    public LangButton()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        Cursor = Cursors.Hand; BackColor = Theme.Bg;
    }

    public string Label => "◉  " + L.Languages.First(l => l.Code == L.Current).Native + "  ▾";

    public Size Measure() => TextRenderer.MeasureText(Label, _f, new Size(int.MaxValue, 0), TextFormatFlags.NoPadding)
        + new Size((int)(24 * Theme.K(this)), (int)(14 * Theme.K(this)));

    protected override void OnMouseEnter(EventArgs e) { _hot = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hot = false; Invalidate(); base.OnMouseLeave(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        Theme.Smooth(g);
        g.Clear(Theme.Bg);
        var k = Theme.K(this);
        var r = new RectangleF(1, 1, Width - 3, Height - 3);
        using (var path = Theme.Round(r, 8 * k))
        {
            using var b = new SolidBrush(_hot ? Theme.PanelHot : Theme.Panel); g.FillPath(b, path);
            using var pen = new Pen(_hot ? Theme.Accent : Theme.Line); g.DrawPath(pen, path);
        }
        TextRenderer.DrawText(g, Label, _f, new Rectangle(0, 0, Width, Height), Theme.Text,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
    }
}

/// <summary>On/off row with a pill switch.</summary>
internal sealed class SwitchRow : Control
{
    private readonly Font _cap = Theme.Font(10.5f, FontStyle.Bold);
    private readonly InfoIcon _info = new();
    private readonly ToolTip _tip;
    private readonly string _capKey, _helpKey;
    public bool On { get; private set; }
    public event Action<bool>? Toggled;

    public SwitchRow(string capKey, string helpKey, bool on, ToolTip tip)
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        _capKey = capKey; _helpKey = helpKey; _tip = tip; On = on;
        BackColor = Theme.Bg; Cursor = Cursors.Hand; Height = 64;
        Controls.Add(_info);
        Relang();
    }

    public void Relang()
    {
        _tip.SetToolTip(_info, L.T(_helpKey));
        if (IsHandleCreated) PlaceInfo();
        Invalidate();
    }

    protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); Height = (int)(64 * Theme.K(this)); PlaceInfo(); }

    private void PlaceInfo()
    {
        var k = Theme.K(this);
        _info.Left = (int)(18 * k) + TextRenderer.MeasureText(L.T(_capKey), _cap, new Size(int.MaxValue, 0), TextFormatFlags.NoPadding).Width + (int)(8 * k);
        _info.Top = (Height - _info.Height) / 2;
    }

    protected override void OnResize(EventArgs e) { base.OnResize(e); if (IsHandleCreated) PlaceInfo(); }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left && !_info.Bounds.Contains(e.Location))
        {
            On = !On; Invalidate(); Toggled?.Invoke(On);
        }
        base.OnMouseUp(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        Theme.Smooth(g);
        g.Clear(Theme.Bg);
        var k = Theme.K(this);
        using (var card = Theme.Round(new RectangleF(0.5f, 0.5f, Width - 2, Height - 2), 10 * k))
        {
            using var b = new SolidBrush(Theme.Panel); g.FillPath(b, card);
            using var p = new Pen(On ? Theme.Accent : Theme.Line); g.DrawPath(p, card);
        }
        var cap = L.T(_capKey);
        var th = TextRenderer.MeasureText(cap, _cap, new Size(int.MaxValue, 0), TextFormatFlags.NoPadding).Height;
        TextRenderer.DrawText(g, cap, _cap, new Point((int)(18 * k), (Height - th) / 2), Theme.Text, TextFormatFlags.NoPadding);

        var sw = 46 * k; var sh = 24 * k;
        var r = new RectangleF(Width - 18 * k - sw, (Height - sh) / 2, sw, sh);
        using (var pill = Theme.Round(r, sh / 2))
        {
            if (On)
            {
                using var lg = new LinearGradientBrush(r, Theme.Accent, Theme.Cyan, 0f); g.FillPath(lg, pill);
            }
            else { using var off = new SolidBrush(Theme.Line); g.FillPath(off, pill); }
        }
        var d = sh - 6 * k;
        var x = On ? r.Right - d - 3 * k : r.Left + 3 * k;
        using (var knob = new SolidBrush(Theme.Text)) g.FillEllipse(knob, x, r.Top + 3 * k, d, d);
    }
}

/// <summary>Highlighted note (privacy notice).</summary>
internal sealed class NoteCard : Control
{
    private readonly Font _f = Theme.Font(9.5f);
    private readonly string _key;

    public NoteCard(string key)
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        _key = key; BackColor = Theme.Bg; Height = 80;
    }

    public void Relang() { Fit(); Invalidate(); }

    public void Fit()
    {
        if (Width < 80) return;
        var k = Theme.K(this);
        var s = TextRenderer.MeasureText(L.T(_key), _f, new Size(Width - (int)(44 * k), 0), TextFormatFlags.WordBreak);
        var h = s.Height + (int)(28 * k);
        if (Math.Abs(h - Height) > 1) Height = h;
    }

    protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); Fit(); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        Theme.Smooth(g);
        g.Clear(Theme.Bg);
        var k = Theme.K(this);
        using (var card = Theme.Round(new RectangleF(0.5f, 0.5f, Width - 2, Height - 2), 10 * k))
        {
            using var b = new SolidBrush(Color.FromArgb(32, 18, 29)); g.FillPath(b, card);
            using var p = new Pen(Color.FromArgb(120, Theme.Magenta)); g.DrawPath(p, card);
        }
        using (var bar = new SolidBrush(Theme.Magenta)) g.FillRectangle(bar, 0, 10 * k, 4 * k, Height - 20 * k);
        TextRenderer.DrawText(g, L.T(_key), _f, new Rectangle((int)(22 * k), (int)(14 * k), Width - (int)(44 * k), Height - (int)(20 * k)), Theme.Text, TextFormatFlags.WordBreak);
    }
}

/// <summary>API key entry: masked box, save/remove buttons and the current key state.</summary>
internal sealed class KeyCard : Control
{
    private readonly Font _cap = Theme.Font(10.5f, FontStyle.Bold);
    private readonly Font _sub = Theme.Font(9f);
    private readonly InfoIcon _info = new();
    private readonly TextBox _box = new()
    {
        UseSystemPasswordChar = true, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.FromArgb(30, 30, 34),
        ForeColor = Theme.Text, Font = Theme.Font(10f), PlaceholderText = "sk-ant-..."
    };
    private readonly Button _save = new(), _clear = new();
    private readonly ToolTip _tip;

    public KeyCard(ToolTip tip)
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        _tip = tip; BackColor = Theme.Bg; Height = 130;
        foreach (var b in new[] { _save, _clear })
        {
            b.FlatStyle = FlatStyle.Flat; b.FlatAppearance.BorderSize = 0; b.Cursor = Cursors.Hand; b.Font = Theme.Font(9.5f, FontStyle.Bold);
        }
        _save.BackColor = Theme.Accent; _save.ForeColor = Theme.Bg;
        _clear.BackColor = Theme.Line; _clear.ForeColor = Theme.Text;
        _save.Click += (_, _) =>
        {
            var key = _box.Text.Trim();
            if (key.Length < 10) return;
            if (AiKeyStore.Save(key)) _box.Clear();
            Invalidate();
        };
        _clear.Click += (_, _) => { AiKeyStore.Clear(); _box.Clear(); Invalidate(); };
        Controls.AddRange(new Control[] { _info, _box, _save, _clear });
        Relang();
    }

    public void Relang()
    {
        _save.Text = L.T("ai.key.save"); _clear.Text = L.T("ai.key.clear");
        _tip.SetToolTip(_info, L.T("ai.key.help"));
        DoLayout(); Invalidate();
    }

    protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); Height = (int)(130 * Theme.K(this)); DoLayout(); }
    protected override void OnResize(EventArgs e) { base.OnResize(e); if (IsHandleCreated) DoLayout(); }

    private void DoLayout()
    {
        var k = Theme.K(this);
        _info.Left = (int)(18 * k) + TextRenderer.MeasureText(L.T("ai.key.cap"), _cap, new Size(int.MaxValue, 0), TextFormatFlags.NoPadding).Width + (int)(8 * k);
        _info.Top = (int)(9 * k);
        int Bw(Button b) => Math.Max((int)(120 * k), TextRenderer.MeasureText(b.Text, b.Font, new Size(int.MaxValue, 0), TextFormatFlags.NoPadding).Width + (int)(36 * k));
        var bh = (int)(32 * k); var by = (int)(46 * k);
        var wClear = Bw(_clear); var wSave = Bw(_save);
        _clear.SetBounds(Width - (int)(18 * k) - wClear, by, wClear, bh);
        _save.SetBounds(_clear.Left - (int)(8 * k) - wSave, by, wSave, bh);
        var bx = (int)(18 * k);
        _box.SetBounds(bx, by + (bh - _box.Height) / 2, Math.Max(60, _save.Left - (int)(12 * k) - bx), _box.Height);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        Theme.Smooth(g);
        g.Clear(Theme.Bg);
        var k = Theme.K(this);
        using (var card = Theme.Round(new RectangleF(0.5f, 0.5f, Width - 2, Height - 2), 10 * k))
        {
            using var b = new SolidBrush(Theme.Panel); g.FillPath(b, card);
            using var p = new Pen(Theme.Line); g.DrawPath(p, card);
        }
        TextRenderer.DrawText(g, L.T("ai.key.cap"), _cap, new Point((int)(18 * k), (int)(10 * k)), Theme.Text, TextFormatFlags.NoPadding);
        string text; Color col;
        if (AiKeyStore.HasSavedKey) { text = L.T("ai.key.saved"); col = Theme.Accent; }
        else if (AiKeyStore.HasEnvKey) { text = L.T("ai.key.env"); col = Theme.Accent; }
        else { text = L.T("ai.key.none"); col = Theme.Dim; }
        TextRenderer.DrawText(g, text, _sub, new Point((int)(18 * k), Height - (int)(34 * k)), col, TextFormatFlags.NoPadding);
    }
}

/// <summary>Shows the state of the AI scenes and the last scene received (palette swatches).</summary>
internal sealed class AiStatusCard : Control
{
    private readonly Font _small = Theme.Font(8.5f, FontStyle.Bold);
    private readonly Font _text = Theme.Font(10.5f);

    public AiStatusCard()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        BackColor = Theme.Bg; Height = 100;
    }

    protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); Height = (int)(100 * Theme.K(this)); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        Theme.Smooth(g);
        g.Clear(Theme.Bg);
        var k = Theme.K(this);
        using (var card = Theme.Round(new RectangleF(0.5f, 0.5f, Width - 2, Height - 2), 10 * k))
        {
            using var b = new SolidBrush(Theme.Panel); g.FillPath(b, card);
            using var p = new Pen(Theme.Line); g.DrawPath(p, card);
        }
        TextRenderer.DrawText(g, L.T("ai.last"), _small, new Point((int)(18 * k), (int)(12 * k)), Theme.Dim, TextFormatFlags.NoPadding);

        var key = Live.AiKey;
        string status;
        if (key == "ok" && Live.AiScene is { } sc) status = L.T("ai.st.ok", sc.Pattern, string.IsNullOrEmpty(sc.Mood) ? "-" : sc.Mood);
        else if (key == "error") status = L.T("ai.st.error", Live.AiError);
        else status = L.T("ai.st." + key);
        var col = key == "error" ? Theme.Magenta : key == "requesting" ? Theme.Cyan : Theme.Text;
        TextRenderer.DrawText(g, status, _text, new Rectangle((int)(18 * k), (int)(34 * k), Width - (int)(36 * k), (int)(24 * k)), col,
            TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);

        if (Live.AiScene is { } scene && key != "off" && key != "nokey")
        {
            var x = 18 * k;
            foreach (var c in scene.Palette)
            {
                using var br = new SolidBrush(Color.FromArgb((int)(c[0] * 255), (int)(c[1] * 255), (int)(c[2] * 255)));
                using var sw = Theme.Round(new RectangleF(x, Height - 32 * k, 44 * k, 16 * k), 5 * k);
                g.FillPath(br, sw);
                x += 52 * k;
            }
        }
    }
}

internal sealed class MainForm : Form
{
    [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hwnd, int cmd);
    [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    private sealed record Opt(string Page, string Key, double Min, double Max, Func<Config, double> Get, Action<Config, double> Set,
        Func<double, string> Fmt, string AppliesTo = "");

    private static readonly string[] PageIds = { "live", "wave", "window", "look", "desk", "ai" };

    private readonly Config _cfg;
    private readonly AppPaths _paths;
    private readonly ToolTip _tip = new();
    private readonly NotifyIcon _tray = new();
    private readonly Timer _saveTimer = new() { Interval = 400 };
    private readonly Timer _uiTimer = new() { Interval = 50 };
    private readonly Dictionary<string, string> _pending = new();
    private readonly List<(Opt opt, OptionRow row)> _rows = new();
    private readonly List<StyleCard> _cards = new();
    private readonly List<(string id, NavButton nav, Control page)> _pages = new();
    private readonly List<(DbFlow flow, List<Control> rows)> _flows = new();
    private AiStatusCard? _aiCard;
    private readonly List<Action> _relang = new();
    private readonly ToolStripMenuItem _miOpen = new(), _miStyle = new(), _miSmooth = new(), _miBarrier = new(), _miWave = new(), _miExit = new(), _miAuto = new();
    private readonly Font _small = Theme.Font(9.5f);
    private KeyPreview? _big, _mini;
    private Label _status = new();
    private Panel _dot = new();
    private LangButton _langBtn = new();
    private bool _hideFirst;
    private bool _exiting;
    private bool _balloonShown;

    private static string F(double v) => v.ToString("0.####", CultureInfo.InvariantCulture);

    private static IEnumerable<Opt> Options() => new[]
    {
        new Opt("wave", "WaveSeconds", 0.5, 6, c => c.WaveSeconds, (c, v) => c.WaveSeconds = v, v => $"{v:0.0} s"),
        new Opt("wave", "BarrierWidth", 0.05, 0.5, c => c.BarrierWidth, (c, v) => c.BarrierWidth = v, v => $"{v * 100:0}%", "barrier"),
        new Opt("wave", "WaveBand", 0.1, 1, c => c.WaveBand, (c, v) => c.WaveBand = v, v => $"{v * 100:0}%", "smooth"),
        new Opt("wave", "WaveGlow", 0, 1, c => c.WaveGlow, (c, v) => c.WaveGlow = v, v => $"{v * 100:0}%", "smooth"),

        new Opt("window", "WindowInfluence", 0, 1, c => c.WindowInfluence, (c, v) => c.WindowInfluence = v, v => $"{v * 100:0}%"),
        new Opt("window", "WindowFollowSeconds", 0, 6, c => c.WindowFollowSeconds, (c, v) => c.WindowFollowSeconds = v,
            v => v < 0.05 ? L.T("f.instant") : $"{v:0.0} s"),
        new Opt("window", "ChromaThreshold", 0, 0.6, c => c.ChromaThreshold, (c, v) => c.ChromaThreshold = v, v => $"{v:0.00}"),
        new Opt("window", "ValueThreshold", 0, 0.5, c => c.ValueThreshold, (c, v) => c.ValueThreshold = v, v => $"{v:0.00}"),
        new Opt("window", "WindowSampleMs", 50, 2000, c => c.WindowSampleMs, (c, v) => c.WindowSampleMs = (int)v, v => $"{v:0} ms"),

        new Opt("look", "Brightness", 0.1, 1, c => c.Brightness, (c, v) => c.Brightness = v, v => $"{v * 100:0}%"),
        new Opt("look", "Saturation", 0, 2.5, c => c.Saturation, (c, v) => c.Saturation = v, v => $"{v:0.00}"),
        new Opt("look", "Gamma", 0.6, 3, c => c.Gamma, (c, v) => c.Gamma = v, v => $"{v:0.0}"),

        new Opt("desk", "DriftSpeed", 0, 4, c => c.DriftSpeed, (c, v) => c.DriftSpeed = v, v => v < 0.05 ? L.T("f.still") : $"{v:0.0}x"),
        new Opt("desk", "Shimmer", 0, 0.6, c => c.Shimmer, (c, v) => c.Shimmer = v, v => $"{v * 100:0}%"),
        new Opt("desk", "RandomRippleEverySeconds", 0, 60, c => c.RandomRippleEverySeconds, (c, v) => c.RandomRippleEverySeconds = v,
            v => v < 1 ? L.T("f.never") : L.T("f.every", v.ToString("0"))),
        new Opt("desk", "Fps", 5, 40, c => c.Fps, (c, v) => c.Fps = (int)v, v => $"{v:0} fps"),

        new Opt("ai", "AiStrength", 0, 1, c => c.AiStrength, (c, v) => c.AiStrength = v, v => $"{v * 100:0}%"),
        new Opt("ai", "AiMinSeconds", 3, 120, c => c.AiMinSeconds, (c, v) => c.AiMinSeconds = v, v => $"{v:0} s"),
        new Opt("ai", "AiFadeSeconds", 0.2, 8, c => c.AiFadeSeconds, (c, v) => c.AiFadeSeconds = v, v => $"{v:0.0} s"),
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

        // Insertion order: the fill control first, then the edges.
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

        L.Changed += () => { if (IsHandleCreated) BeginInvoke(new Action(ApplyLanguage)); };
        ApplyLanguage();

        _saveTimer.Tick += (_, _) => { _saveTimer.Stop(); Flush(); };
        _uiTimer.Tick += (_, _) =>
        {
            if (!Visible) return;
            _big?.Invalidate(); _mini?.Invalidate();
            if (_aiCard is not null && _aiCard.Visible) _aiCard.Invalidate();
            if (_status.Text != Live.StatusText) _status.Text = Live.StatusText;
            var c = Live.StatusOk ? Theme.Accent : Theme.Magenta;
            if (_dot.BackColor != c) _dot.BackColor = c;
        };
        _uiTimer.Start();
        Resize += (_, _) => FitRows();
        _ = Handle; // needed to use BeginInvoke even when starting hidden
    }

    // ------------------------------------------------------------ UI construction

    private Control BuildTopBar()
    {
        var bar = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = Theme.Bg };
        var title = new Label { Text = "LEGION CHROMAFLOW", Font = Theme.Font(17f, FontStyle.Bold), ForeColor = Theme.Accent, AutoSize = true, Location = new Point(24, 12), BackColor = Theme.Bg };
        _dot = new Panel { Size = new Size(10, 10), BackColor = Theme.Magenta };
        _status = new Label { Font = _small, ForeColor = Theme.Dim, AutoSize = true, BackColor = Theme.Bg, Text = Live.StatusText };
        _langBtn = new LangButton();
        _tip.SetToolTip(_langBtn, L.T("lang.tip"));
        _langBtn.Click += (_, _) => ShowLanguageMenu();
        bar.Controls.AddRange(new Control[] { title, _langBtn, _dot, _status });
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
            var ls = _langBtn.Measure();
            _langBtn.Size = ls;
            _langBtn.Location = new Point(_dot.Left - ls.Width - (int)(28 * k), (int)(32 * k - ls.Height / 2f + 4 * k));
        }
        bar.Resize += (_, _) => Place();
        bar.HandleCreated += (_, _) => Place();
        _status.TextChanged += (_, _) => Place();
        _relang.Add(() => { _tip.SetToolTip(_langBtn, L.T("lang.tip")); _langBtn.Invalidate(); Place(); });
        bar.Paint += (_, e) =>
        {
            var k = Theme.K(bar);
            var h = Math.Max(2, (int)(2 * k));
            using var lg = new LinearGradientBrush(new Rectangle(0, bar.Height - h - 1, Math.Max(1, bar.Width), h + 2), Theme.Accent, Theme.Cyan, 0f);
            e.Graphics.FillRectangle(lg, 0, bar.Height - h, bar.Width, h);
        };
        return bar;
    }

    private void ShowLanguageMenu()
    {
        var menu = new ContextMenuStrip { Renderer = new ToolStripProfessionalRenderer(new DarkColors()), BackColor = Color.FromArgb(24, 24, 28), ForeColor = Theme.Text, ShowImageMargin = false, Font = Theme.Font(10f) };
        var setting = _cfg.Language;
        var auto = new ToolStripMenuItem(L.T("lang.auto")) { Checked = setting == "auto" };
        auto.Click += (_, _) => ChangeLanguage("auto");
        menu.Items.Add(auto);
        menu.Items.Add(new ToolStripSeparator());
        foreach (var (code, native) in L.Languages)
        {
            var item = new ToolStripMenuItem(native) { Checked = setting == code };
            var c = code;
            item.Click += (_, _) => ChangeLanguage(c);
            menu.Items.Add(item);
        }
        menu.Show(_langBtn, new Point(0, _langBtn.Height));
    }

    private void ChangeLanguage(string setting)
    {
        _cfg.Language = setting;
        Queue("Language", $"\"{setting}\"");
        L.Set(setting);
        ApplyLanguage();
    }

    private void ApplyLanguage()
    {
        foreach (var a in _relang) a();
        foreach (var (_, row) in _rows) row.Relang();
        foreach (var c in _cards) c.Invalidate();
        foreach (var (_, nav, _) in _pages) nav.Invalidate();
        RefreshApplies();
        _miOpen.Text = L.T("tray.open");
        _miStyle.Text = L.T("tray.style");
        _miSmooth.Text = L.T("tray.smooth");
        _miBarrier.Text = L.T("tray.barrier");
        _miWave.Text = L.T("tray.wave");
        _miExit.Text = L.T("tray.exit");
        _miAuto.Text = L.T("autostart");
        _status.Text = Live.StatusText;
    }

    private Control BuildSidebar()
    {
        var side = new Panel { Dock = DockStyle.Left, Width = 230, BackColor = Theme.Side };
        side.HandleCreated += (_, _) => side.Width = (int)(230 * Theme.K(side));

        // bottom: mini preview + general settings
        var bottom = new Panel { Dock = DockStyle.Bottom, BackColor = Theme.Side, Height = 232 };
        bottom.HandleCreated += (_, _) => bottom.Height = (int)(232 * Theme.K(bottom));
        _mini = new KeyPreview();

        var auto = new CheckBox { Text = L.T("autostart"), ForeColor = Theme.Text, BackColor = Theme.Side, AutoSize = true, Cursor = Cursors.Hand, Checked = AutostartEnabled(), Font = Theme.Font(9.5f) };
        auto.CheckedChanged += (_, _) => { SetAutostart(auto.Checked); _miAuto.Checked = auto.Checked; };
        _miAuto.CheckOnClick = true; _miAuto.Checked = auto.Checked;
        _miAuto.CheckedChanged += (_, _) => auto.Checked = _miAuto.Checked;

        var hint = new Label { Text = L.T("hint"), Font = Theme.Font(8.5f), ForeColor = Theme.Dim, BackColor = Theme.Side, AutoSize = false };
        _relang.Add(() => { auto.Text = L.T("autostart"); hint.Text = L.T("hint"); });

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
        foreach (var id in PageIds)
        {
            var b = new NavButton(id) { Margin = Padding.Empty };
            var pid = id;
            b.Click += (_, _) => SelectPage(pid);
            nav.Controls.Add(b);
            _pages.Add((id, b, null!));
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
            var id = _pages[i].id;
            var inner = id == "live" ? BuildLivePage(id) : BuildOptionsPage(id);
            inner.Dock = DockStyle.Fill;
            inner.Visible = false;
            content.Controls.Add(inner);
            _pages[i] = (id, _pages[i].nav, inner);
        }
    }

    private Control Heading(string id)
    {
        var p = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Bg, Margin = Padding.Empty };
        var h = new Label { Text = L.T($"p.{id}.h"), Font = Theme.Font(18f, FontStyle.Bold), ForeColor = Theme.Text, AutoSize = true, Location = new Point(0, 0), BackColor = Theme.Bg };
        var l = new Label { Text = L.T($"p.{id}.l"), Font = Theme.Font(10f), ForeColor = Theme.Dim, AutoSize = true, BackColor = Theme.Bg };
        p.Controls.AddRange(new Control[] { h, l });
        void Place() { var k = Theme.K(p); l.Location = new Point(0, h.Bottom + (int)(2 * k)); }
        p.Resize += (_, _) => Place();
        p.HandleCreated += (_, _) => Place();
        _relang.Add(() => { h.Text = L.T($"p.{id}.h"); l.Text = L.T($"p.{id}.l"); Place(); });
        return p;
    }

    private Control BuildLivePage(string id)
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

        t.Controls.Add(Heading(id), 0, 0);

        _big = new KeyPreview { Dock = DockStyle.Fill, Margin = new Padding(0, 4, 0, 10) };
        t.Controls.Add(_big, 0, 1);

        var lab = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Bg, Margin = Padding.Empty };
        var lt = new Label { Text = L.T("live.style"), Font = Theme.Font(9.5f, FontStyle.Bold), ForeColor = Theme.Dim, AutoSize = true, BackColor = Theme.Bg };
        var ii = new InfoIcon { BackColor = Theme.Bg };
        _tip.SetToolTip(ii, L.T("live.style.tip"));
        lab.Controls.AddRange(new Control[] { lt, ii });
        void PlaceLab() { var k = Theme.K(lab); lt.Location = new Point(0, (int)(12 * k)); ii.Location = new Point(lt.Right + (int)(8 * k), (int)(10 * k)); }
        lab.Resize += (_, _) => PlaceLab();
        lab.HandleCreated += (_, _) => PlaceLab();
        _relang.Add(() => { lt.Text = L.T("live.style"); _tip.SetToolTip(ii, L.T("live.style.tip")); PlaceLab(); });
        t.Controls.Add(lab, 0, 2);

        var cards = new DbTable { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        cards.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var smooth = new StyleCard("smooth", "SMOOTH") { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 8, 0) };
        var barrier = new StyleCard("barrier", "BARRIER") { Dock = DockStyle.Fill, Margin = new Padding(8, 0, 0, 0) };
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
            Text = L.T("btn.wave"), FlatStyle = FlatStyle.Flat, BackColor = Theme.Accent, ForeColor = Theme.Bg,
            Font = Theme.Font(10.5f, FontStyle.Bold), Cursor = Cursors.Hand, Size = new Size(230, 44)
        };
        wave.FlatAppearance.BorderSize = 0;
        wave.Click += (_, _) => Live.RequestWave();
        _tip.SetToolTip(wave, L.T("btn.wave.tip"));
        var brow = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Bg, Margin = Padding.Empty };
        brow.Controls.Add(wave);
        void PlaceBtn()
        {
            var k = Theme.K(brow);
            var tw = TextRenderer.MeasureText(wave.Text, wave.Font, new Size(int.MaxValue, 0), TextFormatFlags.NoPadding).Width;
            wave.Size = new Size(Math.Max((int)(230 * k), tw + (int)(48 * k)), (int)(44 * k));
            wave.Location = new Point(0, Math.Max(0, (brow.Height - wave.Height) / 2));
        }
        brow.Resize += (_, _) => PlaceBtn();
        brow.HandleCreated += (_, _) => PlaceBtn();
        _relang.Add(() => { wave.Text = L.T("btn.wave"); _tip.SetToolTip(wave, L.T("btn.wave.tip")); PlaceBtn(); });
        t.Controls.Add(brow, 0, 4);
        return t;
    }

    private Control BuildOptionsPage(string id)
    {
        var t = new DbTable { ColumnCount = 1, RowCount = 2 };
        t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        t.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        t.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        t.HandleCreated += (_, _) => t.RowStyles[0].Height = 78 * Theme.K(t);
        t.Controls.Add(Heading(id), 0, 0);

        var flow = new DbFlow { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true };
        flow.MouseEnter += (_, _) => flow.Focus();
        var rows = new List<Control>();
        if (id == "ai") AddAiControls(flow, rows);
        foreach (var o in Options().Where(x => x.Page == id))
        {
            var row = new OptionRow(o.Key, o.Min, o.Max, o.Get(_cfg), o.Fmt, _tip) { Margin = new Padding(0, 0, 0, 10) };
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

    private void AddAiControls(DbFlow flow, List<Control> rows)
    {
        var note = new NoteCard("ai.privacy") { Margin = new Padding(0, 0, 0, 10) };
        var sw = new SwitchRow("ai.enable", "ai.enable.h", _cfg.AiEnabled, _tip) { Margin = new Padding(0, 0, 0, 10) };
        sw.Toggled += on => { _cfg.AiEnabled = on; Queue("AiEnabled", on ? "true" : "false"); };
        var key = new KeyCard(_tip) { Margin = new Padding(0, 0, 0, 10) };
        _aiCard = new AiStatusCard { Margin = new Padding(0, 0, 0, 10) };
        foreach (var c in new Control[] { note, sw, key, _aiCard })
        {
            flow.Controls.Add(c);
            rows.Add(c);
            c.MouseEnter += (_, _) => flow.Focus();
        }
        _relang.Add(() => { note.Relang(); sw.Relang(); key.Relang(); _aiCard?.Invalidate(); });
    }

    private void FitRows()
    {
        foreach (var (flow, rows) in _flows)
        {
            var w = flow.ClientSize.Width - (int)(6 * Theme.K(flow));
            if (w < 100) continue;
            w = Math.Min(w, (int)(980 * Theme.K(flow)));
            foreach (var r in rows) { r.Width = w; if (r is NoteCard nc) nc.Fit(); }
        }
    }

    private void SelectPage(string id)
    {
        foreach (var (pid, nav, page) in _pages)
        {
            var on = pid == id;
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
            row.SetApplies(ok, ok ? "" : L.T("n.only", o.AppliesTo.ToUpperInvariant()));
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

    // ------------------------------------------------------------ saving

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

    // ------------------------------------------------------------ autostart

    private const string TaskName = "LegionChromaFlow";
    private string StartupLink => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "LegionChromaFlow.lnk");

    private static int RunHidden(string file, string args)
    {
        try
        {
            using var p = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(file, args)
            { CreateNoWindow = true, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true });
            if (p is null) return -1;
            p.StandardOutput.ReadToEnd(); p.StandardError.ReadToEnd();
            p.WaitForExit(8000);
            return p.HasExited ? p.ExitCode : -1;
        }
        catch { return -1; }
    }

    private bool AutostartEnabled() => RunHidden("schtasks.exe", $"/Query /TN {TaskName}") == 0 || File.Exists(StartupLink);

    /// <summary>
    /// Autostart through a logon scheduled task: unlike the Startup folder it is not subject to the
    /// Windows startup delay, so the lights come up right after sign-in.
    /// </summary>
    private void SetAutostart(bool on)
    {
        try
        {
            if (File.Exists(StartupLink)) File.Delete(StartupLink);
            if (!on) { RunHidden("schtasks.exe", $"/Delete /TN {TaskName} /F"); return; }

            var user = System.Security.SecurityElement.Escape($"{Environment.UserDomainName}\\{Environment.UserName}");
            var root = System.Security.SecurityElement.Escape(_paths.Root);
            var xml = $@"<?xml version=""1.0"" encoding=""UTF-16""?>
<Task version=""1.2"" xmlns=""http://schemas.microsoft.com/windows/2004/02/mit/task"">
  <RegistrationInfo><Description>Starts LegionChromaFlow (keyboard lighting) at sign-in</Description></RegistrationInfo>
  <Triggers><LogonTrigger><Enabled>true</Enabled><UserId>{user}</UserId></LogonTrigger></Triggers>
  <Principals><Principal id=""Author""><UserId>{user}</UserId><LogonType>InteractiveToken</LogonType><RunLevel>LeastPrivilege</RunLevel></Principal></Principals>
  <Settings><MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy><DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries><StopIfGoingOnBatteries>false</StopIfGoingOnBatteries><ExecutionTimeLimit>PT0S</ExecutionTimeLimit><StartWhenAvailable>true</StartWhenAvailable></Settings>
  <Actions Context=""Author""><Exec><Command>wscript.exe</Command><Arguments>""{root}\run-hidden.vbs""</Arguments><WorkingDirectory>{root}</WorkingDirectory></Exec></Actions>
</Task>";
            var file = Path.Combine(Path.GetTempPath(), "lcf-task.xml");
            File.WriteAllText(file, xml, System.Text.Encoding.Unicode);
            var code = RunHidden("schtasks.exe", $"/Create /TN {TaskName} /XML \"{file}\" /F");
            try { File.Delete(file); } catch { }
            if (code != 0) throw new InvalidOperationException($"schtasks exit code {code}");
        }
        catch (Exception ex) { Log.Warn($"Autostart not changed: {ex.Message}"); }
    }

    // ------------------------------------------------------------ tray

    private void BuildTray()
    {
        var menu = new ContextMenuStrip { Renderer = new ToolStripProfessionalRenderer(new DarkColors()), BackColor = Color.FromArgb(24, 24, 28), ForeColor = Theme.Text, ShowImageMargin = false, Font = Theme.Font(9.5f) };
        _miOpen.Font = Theme.Font(9.5f, FontStyle.Bold);
        _miOpen.Click += (_, _) => ShowPanel();

        _miSmooth.Click += (_, _) => SelectStyle("smooth", true);
        _miBarrier.Click += (_, _) => SelectStyle("barrier", true);
        _miStyle.DropDownItems.AddRange(new ToolStripItem[] { _miSmooth, _miBarrier });

        _miWave.Click += (_, _) => Live.RequestWave();
        _miExit.Click += (_, _) => ExitApp();

        menu.Items.AddRange(new ToolStripItem[] { _miOpen, new ToolStripSeparator(), _miStyle, _miWave, _miAuto, new ToolStripSeparator(), _miExit });
        foreach (ToolStripItem i in _miStyle.DropDownItems) { i.BackColor = Color.FromArgb(24, 24, 28); i.ForeColor = Theme.Text; }

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

    // ------------------------------------------------------------ window

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
        // Started from a hidden .vbs, Windows ignores the first "show": repeat it explicitly.
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
                _tray.ShowBalloonTip(3000, "LegionChromaFlow", L.T("balloon"), ToolTipIcon.Info);
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
        var cap = 0x0D0B0B; // COLORREF (BGR) of the background color
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
            // An instance already exists: ask it to show the panel.
            try { using var ev = EventWaitHandle.OpenExisting(Program.ShowEventName); ev.Set(); } catch { }
            return 0;
        }

        using var stop = new EventWaitHandle(false, EventResetMode.ManualReset, Program.StopEventName);
        using var show = new EventWaitHandle(false, EventResetMode.AutoReset, Program.ShowEventName);

        Desktop.EnableDpiAwareness();
        try { System.Diagnostics.Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.BelowNormal; } catch { }

        var cfg = Config.Load(paths.ConfigPath);
        L.Init(cfg.Language);
        Log.Info($"Starting panel. Config: {paths.ConfigPath}");

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        var form = new MainForm(cfg, paths, startHidden);

        var engine = new Thread(() =>
        {
            try { Program.RunLoop(cfg, stop, test: false); }
            catch (Exception ex) { Log.Error($"Lighting engine stopped: {ex}"); }
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
        Log.Info("Finished.");
        return 0;
    }
}
