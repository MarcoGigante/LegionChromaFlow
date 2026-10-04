using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace LegionChromaFlow;

/// <summary>Accesso HID minimale (SetupAPI + hid.dll), senza librerie esterne.</summary>
internal sealed class HidDevice : IDisposable
{
    public SafeFileHandle Handle { get; }
    public ushort VendorId { get; }
    public ushort ProductId { get; }
    public int FeatureReportLength { get; }
    public string DevicePath { get; }

    private HidDevice(SafeFileHandle h, ushort vid, ushort pid, int featLen, string path)
    {
        Handle = h; VendorId = vid; ProductId = pid; FeatureReportLength = featLen; DevicePath = path;
    }

    public void Dispose() => Handle.Dispose();

    public bool SetFeature(byte[] data) => HidD_SetFeature(Handle, data, (uint)data.Length);

    public bool GetFeature(byte[] buffer) => HidD_GetFeature(Handle, buffer, (uint)buffer.Length);

    /// <summary>Elenca i dispositivi HID del produttore indicato con la lunghezza di feature report richiesta.</summary>
    public static List<HidDevice> Enumerate(ushort vendorId, int featureLength)
    {
        var result = new List<HidDevice>();
        HidD_GetHidGuid(out var guid);

        var info = SetupDiGetClassDevsW(ref guid, null, IntPtr.Zero, DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);
        if (info == IntPtr.Zero || info == new IntPtr(-1))
            return result;

        try
        {
            for (uint i = 0; ; i++)
            {
                var data = new SP_DEVICE_INTERFACE_DATA { cbSize = (uint)Marshal.SizeOf<SP_DEVICE_INTERFACE_DATA>() };
                if (!SetupDiEnumDeviceInterfaces(info, IntPtr.Zero, ref guid, i, ref data))
                    break;

                SetupDiGetDeviceInterfaceDetailW(info, ref data, IntPtr.Zero, 0, out var required, IntPtr.Zero);
                if (required == 0)
                    continue;

                var buf = Marshal.AllocHGlobal((int)required);
                string? path = null;
                try
                {
                    Marshal.WriteInt32(buf, IntPtr.Size == 8 ? 8 : 6); // cbSize di SP_DEVICE_INTERFACE_DETAIL_DATA_W
                    if (SetupDiGetDeviceInterfaceDetailW(info, ref data, buf, required, out _, IntPtr.Zero))
                        path = Marshal.PtrToStringUni(buf + 4);
                }
                finally { Marshal.FreeHGlobal(buf); }

                if (string.IsNullOrEmpty(path))
                    continue;

                var handle = CreateFileW(path, FILE_READ_DATA | FILE_WRITE_DATA, FILE_SHARE_READ | FILE_SHARE_WRITE,
                    IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
                if (handle.IsInvalid)
                {
                    handle.Dispose();
                    continue;
                }

                var attr = new HIDD_ATTRIBUTES { Size = (uint)Marshal.SizeOf<HIDD_ATTRIBUTES>() };
                if (!HidD_GetAttributes(handle, ref attr) || attr.VendorID != vendorId)
                {
                    handle.Dispose();
                    continue;
                }

                int featLen = -1;
                if (HidD_GetPreparsedData(handle, out var pre))
                {
                    try
                    {
                        if (HidP_GetCaps(pre, out var caps) == HIDP_STATUS_SUCCESS)
                            featLen = caps.FeatureReportByteLength;
                    }
                    finally { HidD_FreePreparsedData(pre); }
                }

                if (featLen != featureLength)
                {
                    handle.Dispose();
                    continue;
                }

                result.Add(new HidDevice(handle, attr.VendorID, attr.ProductID, featLen, path));
            }
        }
        finally
        {
            SetupDiDestroyDeviceInfoList(info);
        }

        return result;
    }

    // ---- P/Invoke ----

    private const uint DIGCF_PRESENT = 0x2;
    private const uint DIGCF_DEVICEINTERFACE = 0x10;
    private const uint FILE_READ_DATA = 0x1;
    private const uint FILE_WRITE_DATA = 0x2;
    private const uint FILE_SHARE_READ = 0x1;
    private const uint FILE_SHARE_WRITE = 0x2;
    private const uint OPEN_EXISTING = 3;
    private const int HIDP_STATUS_SUCCESS = 0x110000;

    [StructLayout(LayoutKind.Sequential)]
    private struct SP_DEVICE_INTERFACE_DATA
    {
        public uint cbSize;
        public Guid InterfaceClassGuid;
        public uint Flags;
        public UIntPtr Reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HIDD_ATTRIBUTES
    {
        public uint Size;
        public ushort VendorID;
        public ushort ProductID;
        public ushort VersionNumber;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HIDP_CAPS
    {
        public ushort Usage;
        public ushort UsagePage;
        public ushort InputReportByteLength;
        public ushort OutputReportByteLength;
        public ushort FeatureReportByteLength;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
        public ushort[] Reserved;
        public ushort NumberLinkCollectionNodes;
        public ushort NumberInputButtonCaps;
        public ushort NumberInputValueCaps;
        public ushort NumberInputDataIndices;
        public ushort NumberOutputButtonCaps;
        public ushort NumberOutputValueCaps;
        public ushort NumberOutputDataIndices;
        public ushort NumberFeatureButtonCaps;
        public ushort NumberFeatureValueCaps;
        public ushort NumberFeatureDataIndices;
    }

    [DllImport("hid.dll")]
    private static extern void HidD_GetHidGuid(out Guid hidGuid);

    [DllImport("hid.dll", SetLastError = true)]
    private static extern bool HidD_GetAttributes(SafeFileHandle h, ref HIDD_ATTRIBUTES attr);

    [DllImport("hid.dll", SetLastError = true)]
    private static extern bool HidD_GetPreparsedData(SafeFileHandle h, out IntPtr preparsed);

    [DllImport("hid.dll", SetLastError = true)]
    private static extern bool HidD_FreePreparsedData(IntPtr preparsed);

    [DllImport("hid.dll")]
    private static extern int HidP_GetCaps(IntPtr preparsed, out HIDP_CAPS caps);

    [DllImport("hid.dll", SetLastError = true)]
    private static extern bool HidD_SetFeature(SafeFileHandle h, byte[] buffer, uint length);

    [DllImport("hid.dll", SetLastError = true)]
    private static extern bool HidD_GetFeature(SafeFileHandle h, byte[] buffer, uint length);

    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr SetupDiGetClassDevsW(ref Guid classGuid, string? enumerator, IntPtr hwnd, uint flags);

    [DllImport("setupapi.dll", SetLastError = true)]
    private static extern bool SetupDiEnumDeviceInterfaces(IntPtr devInfo, IntPtr devInfoData, ref Guid interfaceClassGuid, uint index, ref SP_DEVICE_INTERFACE_DATA data);

    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool SetupDiGetDeviceInterfaceDetailW(IntPtr devInfo, ref SP_DEVICE_INTERFACE_DATA data, IntPtr detail, uint detailSize, out uint requiredSize, IntPtr devInfoData);

    [DllImport("setupapi.dll", SetLastError = true)]
    private static extern bool SetupDiDestroyDeviceInfoList(IntPtr devInfo);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeFileHandle CreateFileW(string fileName, uint access, uint share, IntPtr security, uint disposition, uint flags, IntPtr template);
}
