namespace LegionChromaFlow;

/// <summary>
/// Protocollo della tastiera Lenovo Legion Spectrum (per-key RGB), ricavato dal codice di
/// Lenovo Legion Toolkit (LenovoLegionToolkit-Team). Tutti i report sono feature report da 960 byte,
/// con intestazione { 0x07, tipo, 0xC0, 0x03 }.
/// </summary>
internal sealed class SpectrumDevice : IDisposable
{
    private const ushort LenovoVendorId = 0x048D;
    private const int ReportLength = 960;
    private const int HeaderLength = 4;
    private const int MaxBitmapItems = (ReportLength - HeaderLength) / 5;

    private const byte OpCompatibility = 0xD1;
    private const byte OpKeyCount = 0xC4;
    private const byte OpKeyPage = 0xC5;
    private const byte OpGetProfile = 0xCA;
    private const byte OpAuroraStartStop = 0xD0;
    private const byte OpAuroraSendBitmap = 0xA1;

    private readonly HidDevice _hid;

    public int Width { get; private set; }
    public int Height { get; private set; }
    public ushort[,] KeyCodes { get; private set; } = new ushort[0, 0];
    public ushort[] ExtraKeyCodes { get; private set; } = Array.Empty<ushort>();
    public string Description => $"VID={_hid.VendorId:X4} PID={_hid.ProductId:X4}";

    private SpectrumDevice(HidDevice hid) => _hid = hid;

    public void Dispose() => _hid.Dispose();

    /// <summary>Trova e apre la tastiera Spectrum. Restituisce null se non e' presente/compatibile.</summary>
    public static SpectrumDevice? Open()
    {
        var candidates = HidDevice.Enumerate(LenovoVendorId, ReportLength);
        SpectrumDevice? found = null;

        foreach (var c in candidates)
        {
            if (found is null && IsCompatible(c))
                found = new SpectrumDevice(c);
            else
                c.Dispose();
        }

        if (found is null)
            return null;

        found.ReadKeyMap();
        return found;
    }

    private static bool IsCompatible(HidDevice hid)
    {
        try
        {
            if (!hid.SetFeature(Request(OpCompatibility)))
                return false;

            var resp = new byte[ReportLength];
            resp[0] = 7;
            return hid.GetFeature(resp) && resp[4] == 0;
        }
        catch
        {
            return false;
        }
    }

    private static byte[] Request(byte type, params byte[] payload)
    {
        var buf = new byte[ReportLength];
        buf[0] = 7;
        buf[1] = type;
        buf[2] = 0xC0;
        buf[3] = 3;
        for (var i = 0; i < payload.Length; i++)
            buf[HeaderLength + i] = payload[i];
        return buf;
    }

    private byte[]? SetAndGet(byte[] request)
    {
        if (!_hid.SetFeature(request))
            return null;
        var resp = new byte[ReportLength];
        resp[0] = 7;
        return _hid.GetFeature(resp) ? resp : null;
    }

    private void ReadKeyMap()
    {
        var count = SetAndGet(Request(OpKeyCount, 7));
        if (count is null)
            throw new InvalidOperationException("Impossibile leggere il numero di tasti.");

        var rows = count[5];     // Indexes
        var cols = Math.Min((int)count[6], 32);  // KeysPerIndex (max 32 per pagina)
        if (rows == 0 || cols == 0)
            throw new InvalidOperationException($"Mappa tasti non valida (righe={rows}, colonne={cols}).");

        var keys = new ushort[cols, rows];
        for (var y = 0; y < rows; y++)
        {
            var page = SetAndGet(Request(OpKeyPage, 7, (byte)y));
            if (page is null)
                throw new InvalidOperationException($"Impossibile leggere la pagina tasti {y}.");
            for (var x = 0; x < cols; x++)
                keys[x, y] = ReadItemKeyCode(page, x);
        }

        var extra = new List<ushort>();
        var secondary = SetAndGet(Request(OpKeyPage, 8, 0));
        if (secondary is not null)
        {
            for (var x = 0; x < cols; x++)
            {
                var code = ReadItemKeyCode(secondary, x);
                if (code > 0) extra.Add(code);
            }
        }

        Width = cols;
        Height = rows;
        KeyCodes = keys;
        ExtraKeyCodes = extra.ToArray();
    }

    // Ogni elemento della pagina e' { byte indice, ushort codice } a partire dal byte 6.
    private static ushort ReadItemKeyCode(byte[] page, int i)
        => (ushort)(page[7 + 3 * i] | (page[8 + 3 * i] << 8));

    public int GetProfile()
    {
        var resp = SetAndGet(Request(OpGetProfile));
        if (resp is null)
            throw new InvalidOperationException("Impossibile leggere il profilo attivo.");
        return resp[4];
    }

    public bool AuroraStart(int profile) => _hid.SetFeature(Request(OpAuroraStartStop, 1, (byte)profile));

    public bool AuroraStop(int profile) => _hid.SetFeature(Request(OpAuroraStartStop, 2, (byte)profile));

    /// <summary>Invia un fotogramma: per ogni tasto { codice (LE), R, G, B }.</summary>
    public bool SendFrame(IReadOnlyList<ushort> codes, byte[] rgb, int count)
    {
        var buf = new byte[ReportLength];
        buf[0] = 7;
        buf[1] = OpAuroraSendBitmap;
        buf[2] = 0xC0;
        buf[3] = 3;

        var n = Math.Min(count, MaxBitmapItems);
        var o = HeaderLength;
        for (var i = 0; i < n; i++)
        {
            buf[o++] = (byte)(codes[i] & 0xFF);
            buf[o++] = (byte)(codes[i] >> 8);
            buf[o++] = rgb[i * 3];
            buf[o++] = rgb[i * 3 + 1];
            buf[o++] = rgb[i * 3 + 2];
        }

        return _hid.SetFeature(buf);
    }
}
