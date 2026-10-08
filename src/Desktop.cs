using System.Runtime.InteropServices;
using System.Text;

namespace LegionChromaFlow;

/// <summary>Reads the desktop wallpaper (GDI+ flat API) and samples the active window (GDI).</summary>
internal static class Desktop
{
    // ------------------------------------------------------------------ Wallpaper

    private static IntPtr _gdipToken;

    private static bool EnsureGdip()
    {
        if (_gdipToken != IntPtr.Zero) return true;
        var input = new GdiplusStartupInput { GdiplusVersion = 1 };
        return GdiplusStartup(out _gdipToken, ref input, IntPtr.Zero) == 0;
    }

    public static string? GetWallpaperPath()
    {
        var sb = new StringBuilder(520);
        return SystemParametersInfoW(0x0073 /*SPI_GETDESKWALLPAPER*/, (uint)sb.Capacity, sb, 0) && sb.Length > 0
            ? sb.ToString()
            : null;
    }

    /// <summary>Loads an image and downsamples it (block average) to outW x outH, RGB channels in 0..1.</summary>
    public static Field? LoadImage(string path, int outW = 160, int outH = 90)
    {
        if (!File.Exists(path) || !EnsureGdip())
            return null;

        IntPtr bmp = IntPtr.Zero;
        try
        {
            if (GdipCreateBitmapFromFile(path, out bmp) != 0 || bmp == IntPtr.Zero)
                return null;
            if (GdipGetImageWidth(bmp, out var w) != 0 || GdipGetImageHeight(bmp, out var h) != 0 || w == 0 || h == 0)
                return null;

            var rect = new GpRect { X = 0, Y = 0, Width = (int)w, Height = (int)h };
            var data = new BitmapData();
            if (GdipBitmapLockBits(bmp, ref rect, 1 /*ImageLockModeRead*/, 0x26200A /*32bppARGB*/, ref data) != 0)
                return null;

            try
            {
                var sum = new double[outW * outH * 3];
                var cnt = new int[outW * outH];
                var row = new byte[w * 4];
                var stride = data.Stride;

                for (var y = 0; y < h; y++)
                {
                    Marshal.Copy(data.Scan0 + y * stride, row, 0, row.Length);
                    var cy = Math.Min((int)((long)y * outH / h), outH - 1);
                    for (var x = 0; x < w; x++)
                    {
                        var cx = Math.Min((int)((long)x * outW / w), outW - 1);
                        var ci = cy * outW + cx;
                        var o = x * 4; // BGRA
                        sum[ci * 3] += row[o + 2];
                        sum[ci * 3 + 1] += row[o + 1];
                        sum[ci * 3 + 2] += row[o];
                        cnt[ci]++;
                    }
                }

                var f = new Field(outW, outH, 3);
                for (var i = 0; i < outW * outH; i++)
                {
                    var n = Math.Max(cnt[i], 1) * 255.0;
                    f.D[i * 3] = (float)(sum[i * 3] / n);
                    f.D[i * 3 + 1] = (float)(sum[i * 3 + 1] / n);
                    f.D[i * 3 + 2] = (float)(sum[i * 3 + 2] / n);
                }
                return f;
            }
            finally
            {
                GdipBitmapUnlockBits(bmp, ref data);
            }
        }
        catch (Exception ex)
        {
            Log.Warn($"Reading the wallpaper failed: {ex.Message}");
            return null;
        }
        finally
        {
            if (bmp != IntPtr.Zero) GdipDisposeImage(bmp);
        }
    }

    // ------------------------------------------------------------ Active window

    public enum WindowKind { Normal, Desktop, Ignore }

    public readonly record struct WindowInfo(WindowKind Kind, long Handle, string Class);

    private static readonly HashSet<string> IgnoredClasses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Shell_TrayWnd", "Shell_SecondaryTrayWnd", "TaskListThumbnailWnd", "TaskListOverlayWnd",
        "XamlExplorerHostIslandWindow", "Windows.UI.Core.CoreWindow", "ForegroundStaging",
        "MultitaskingViewFrame", "TopLevelWindowForOverflowXamlIsland", "NotifyIconOverflowWindow",
        "Windows.UI.Composition.DesktopWindowContentBridge", "SysShadow", "tooltips_class32"
    };

    private static readonly HashSet<string> DesktopClasses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Progman", "WorkerW"
    };

    public static void EnableDpiAwareness()
    {
        try { SetProcessDPIAware(); } catch { }
    }

    /// <summary>Describes the foreground window (without capturing its pixels).</summary>
    public static WindowInfo GetForeground()
    {
        var h = GetForegroundWindow();
        if (h == IntPtr.Zero)
            return new WindowInfo(WindowKind.Desktop, 0, "");

        var sb = new StringBuilder(256);
        GetClassNameW(h, sb, sb.Capacity);
        var cls = sb.ToString();

        if (DesktopClasses.Contains(cls))
            return new WindowInfo(WindowKind.Desktop, h.ToInt64(), cls);
        if (IgnoredClasses.Contains(cls))
            return new WindowInfo(WindowKind.Ignore, h.ToInt64(), cls);

        GetWindowThreadProcessId(h, out var pid);
        if (pid == (uint)Environment.ProcessId)
            return new WindowInfo(WindowKind.Ignore, h.ToInt64(), cls);

        if (IsIconic(h))
            return new WindowInfo(WindowKind.Ignore, h.ToInt64(), cls);

        return new WindowInfo(WindowKind.Normal, h.ToInt64(), cls);
    }

    /// <summary>Saves a small copy of the wallpaper so the pre-sign-in boot instance can use it.</summary>
    public static void SaveWallpaperCache(string src, string dst)
    {
        try
        {
            using var bmp = new System.Drawing.Bitmap(src);
            var w = Math.Min(480, bmp.Width);
            var h = Math.Max(1, (int)(bmp.Height * (w / (double)bmp.Width)));
            using var small = new System.Drawing.Bitmap(w, h);
            using (var g = System.Drawing.Graphics.FromImage(small))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(bmp, 0, 0, w, h);
            }
            Directory.CreateDirectory(Path.GetDirectoryName(dst)!);
            var enc = System.Drawing.Imaging.ImageCodecInfo.GetImageEncoders().First(e => e.MimeType == "image/jpeg");
            using var prm = new System.Drawing.Imaging.EncoderParameters(1);
            prm.Param[0] = new System.Drawing.Imaging.EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 90L);
            small.Save(dst, enc, prm);
        }
        catch (Exception ex) { Log.Warn($"Could not cache the wallpaper for boot mode: {ex.Message}"); }
    }

    /// <summary>Title of a window (empty if it has none).</summary>
    public static string GetTitle(long handle)
    {
        var sb = new StringBuilder(512);
        GetWindowTextW(new IntPtr(handle), sb, sb.Capacity);
        return sb.ToString();
    }

    /// <summary>Captures a window as a JPEG downscaled to at most maxWidth pixels (used only for the optional AI scenes).</summary>
    public static byte[]? CaptureJpeg(long handle, int maxWidth)
    {
        try
        {
            var hwnd = new IntPtr(handle);
            if (!GetWindowRect(hwnd, out var r)) return null;
            var vx = GetSystemMetrics(76); var vy = GetSystemMetrics(77);
            var vw = GetSystemMetrics(78); var vh = GetSystemMetrics(79);
            var left = Math.Max(r.Left, vx); var top = Math.Max(r.Top, vy);
            var right = Math.Min(r.Right, vx + vw); var bottom = Math.Min(r.Bottom, vy + vh);
            var w = right - left; var h = bottom - top;
            if (w < 80 || h < 80) return null;

            using var full = new System.Drawing.Bitmap(w, h);
            using (var g = System.Drawing.Graphics.FromImage(full))
                g.CopyFromScreen(left, top, 0, 0, new System.Drawing.Size(w, h));

            var scale = Math.Min(1.0, maxWidth / (double)w);
            var tw = Math.Max(1, (int)(w * scale)); var th = Math.Max(1, (int)(h * scale));
            using var small = new System.Drawing.Bitmap(tw, th);
            using (var g = System.Drawing.Graphics.FromImage(small))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(full, 0, 0, tw, th);
            }

            var enc = System.Drawing.Imaging.ImageCodecInfo.GetImageEncoders().First(e => e.MimeType == "image/jpeg");
            using var prm = new System.Drawing.Imaging.EncoderParameters(1);
            prm.Param[0] = new System.Drawing.Imaging.EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 70L);
            using var ms = new MemoryStream();
            small.Save(ms, enc, prm);
            return ms.ToArray();
        }
        catch (Exception ex)
        {
            Log.Warn($"Window capture for AI failed: {ex.Message}");
            return null;
        }
    }

    private static IntPtr _memDc, _bitmap, _oldBitmap;
    private const int CapW = 64, CapH = 36;

    /// <summary>
    /// Captures the window pixels (downscaled to 64x36) and returns a 4-channel Field
    /// premultiplied: (r*w, g*w, b*w, w), where w is the pixel's "color weight" (0 for black/gray/white).
    /// </summary>
    public static Field? CaptureWindow(long handle, double chromaThr, double valueThr)
    {
        var hwnd = new IntPtr(handle);
        if (!GetWindowRect(hwnd, out var r))
            return null;

        // Clamp the window to the virtual screen (maximized windows overhang by a few pixels).
        var vx = GetSystemMetrics(76); var vy = GetSystemMetrics(77);
        var vw = GetSystemMetrics(78); var vh = GetSystemMetrics(79);
        var left = Math.Max(r.Left, vx); var top = Math.Max(r.Top, vy);
        var right = Math.Min(r.Right, vx + vw); var bottom = Math.Min(r.Bottom, vy + vh);
        var w = right - left; var h = bottom - top;
        if (w < 40 || h < 40)
            return null;

        var screen = GetDC(IntPtr.Zero);
        if (screen == IntPtr.Zero)
            return null;

        try
        {
            if (_memDc == IntPtr.Zero)
            {
                _memDc = CreateCompatibleDC(screen);
                _bitmap = CreateCompatibleBitmap(screen, CapW, CapH);
                if (_memDc == IntPtr.Zero || _bitmap == IntPtr.Zero)
                    return null;
            }

            _oldBitmap = SelectObject(_memDc, _bitmap);
            SetStretchBltMode(_memDc, 4 /*HALFTONE*/);
            var ok = StretchBlt(_memDc, 0, 0, CapW, CapH, screen, left, top, w, h, 0x00CC0020 | 0x40000000 /*SRCCOPY|CAPTUREBLT*/);
            SelectObject(_memDc, _oldBitmap);
            if (!ok)
                return null;

            var bih = new BITMAPINFOHEADER
            {
                biSize = (uint)Marshal.SizeOf<BITMAPINFOHEADER>(),
                biWidth = CapW,
                biHeight = -CapH, // top-down
                biPlanes = 1,
                biBitCount = 32,
                biCompression = 0
            };
            var pixels = new byte[CapW * CapH * 4];
            if (GetDIBits(screen, _bitmap, 0, CapH, pixels, ref bih, 0) == 0)
                return null;

            var f = new Field(CapW, CapH, 4);
            for (var i = 0; i < CapW * CapH; i++)
            {
                float b = pixels[i * 4] / 255f, g = pixels[i * 4 + 1] / 255f, rr = pixels[i * 4 + 2] / 255f;
                var weight = ColorWeight(rr, g, b, (float)chromaThr, (float)valueThr);
                f.D[i * 4] = rr * weight;
                f.D[i * 4 + 1] = g * weight;
                f.D[i * 4 + 2] = b * weight;
                f.D[i * 4 + 3] = weight;
            }
            return f;
        }
        finally
        {
            ReleaseDC(IntPtr.Zero, screen);
        }
    }

    /// <summary>0 for black/gray/white pixels (no "color"), up to 1 for vivid, bright colors.</summary>
    public static float ColorWeight(float r, float g, float b, float chromaThr, float valueThr)
    {
        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var chroma = max - min;
        var wc = Smooth((chroma - chromaThr) / 0.25f);
        var wv = Smooth((max - valueThr) / 0.20f);
        return wc * wv;
    }

    private static float Smooth(float x)
    {
        x = Math.Clamp(x, 0f, 1f);
        return x * x * (3f - 2f * x);
    }

    // ------------------------------------------------------------------ P/Invoke

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAPINFOHEADER
    {
        public uint biSize;
        public int biWidth;
        public int biHeight;
        public ushort biPlanes;
        public ushort biBitCount;
        public uint biCompression;
        public uint biSizeImage;
        public int biXPelsPerMeter;
        public int biYPelsPerMeter;
        public uint biClrUsed;
        public uint biClrImportant;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct GdiplusStartupInput
    {
        public uint GdiplusVersion;
        public IntPtr DebugEventCallback;
        public bool SuppressBackgroundThread;
        public bool SuppressExternalCodecs;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct GpRect { public int X, Y, Width, Height; }

    [StructLayout(LayoutKind.Sequential)]
    private struct BitmapData
    {
        public uint Width;
        public uint Height;
        public int Stride;
        public int PixelFormat;
        public IntPtr Scan0;
        public UIntPtr Reserved;
    }

    [DllImport("gdiplus.dll")] private static extern int GdiplusStartup(out IntPtr token, ref GdiplusStartupInput input, IntPtr output);
    [DllImport("gdiplus.dll", CharSet = CharSet.Unicode)] private static extern int GdipCreateBitmapFromFile(string filename, out IntPtr bitmap);
    [DllImport("gdiplus.dll")] private static extern int GdipGetImageWidth(IntPtr image, out uint width);
    [DllImport("gdiplus.dll")] private static extern int GdipGetImageHeight(IntPtr image, out uint height);
    [DllImport("gdiplus.dll")] private static extern int GdipBitmapLockBits(IntPtr bitmap, ref GpRect rect, uint flags, int format, ref BitmapData data);
    [DllImport("gdiplus.dll")] private static extern int GdipBitmapUnlockBits(IntPtr bitmap, ref BitmapData data);
    [DllImport("gdiplus.dll")] private static extern int GdipDisposeImage(IntPtr image);

    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hwnd, out RECT rect);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowTextW(IntPtr hwnd, StringBuilder sb, int max);
    [DllImport("user32.dll")] private static extern bool IsIconic(IntPtr hwnd);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassNameW(IntPtr hwnd, StringBuilder sb, int max);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint pid);
    [DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr hwnd, IntPtr dc);
    [DllImport("user32.dll")] private static extern int GetSystemMetrics(int index);
    [DllImport("user32.dll")] private static extern bool SetProcessDPIAware();
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern bool SystemParametersInfoW(uint action, uint param, StringBuilder value, uint flags);

    [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleDC(IntPtr dc);
    [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleBitmap(IntPtr dc, int w, int h);
    [DllImport("gdi32.dll")] private static extern IntPtr SelectObject(IntPtr dc, IntPtr obj);
    [DllImport("gdi32.dll")] private static extern int SetStretchBltMode(IntPtr dc, int mode);
    [DllImport("gdi32.dll")] private static extern bool StretchBlt(IntPtr dest, int xd, int yd, int wd, int hd, IntPtr src, int xs, int ys, int ws, int hs, uint rop);
    [DllImport("gdi32.dll")] private static extern int GetDIBits(IntPtr dc, IntPtr bitmap, uint start, uint lines, byte[] bits, ref BITMAPINFOHEADER info, uint usage);
}
