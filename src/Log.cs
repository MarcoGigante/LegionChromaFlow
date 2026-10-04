namespace LegionChromaFlow;

internal static class Log
{
    private static readonly object Gate = new();
    private static string? _file;

    public static void Init(string dir)
    {
        try
        {
            Directory.CreateDirectory(dir);
            _file = Path.Combine(dir, "legionchromaflow.log");
            if (File.Exists(_file) && new FileInfo(_file).Length > 1_000_000)
            {
                var old = _file + ".old";
                if (File.Exists(old)) File.Delete(old);
                File.Move(_file, old);
            }
        }
        catch { _file = null; }
    }

    public static void Info(string msg) => Write("INFO ", msg);
    public static void Warn(string msg) => Write("WARN ", msg);
    public static void Error(string msg) => Write("ERROR", msg);

    private static void Write(string level, string msg)
    {
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {level} {msg}";
        lock (Gate)
        {
            try { Console.WriteLine(line); } catch { }
            if (_file is null) return;
            try { File.AppendAllText(_file, line + Environment.NewLine); } catch { }
        }
    }
}
