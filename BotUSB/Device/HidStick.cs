using BotUSB.Interop;
using Microsoft.Win32.SafeHandles;

namespace BotUSB.Device;

/// <summary>
/// The command channel of the stick: the vendor-defined HID collection (COL01) of VID 0x1606 / PID 0x1988.
/// Each command is one output report: [0] = report ID 3, [1] = 0, [2] = <see cref="KeyCode"/>, [3] = argument.
/// Not thread-safe: <see cref="BotEngine"/> is the only user.
/// </summary>
internal sealed unsafe class HidStick : IDisposable
{
    public const ushort VendorId = 0x1606;
    public const ushort ProductId = 0x1988;
    private const string CommandCollection = "vid_1606&pid_1988&col01";
    private const byte ReportId = 3;
    private const int DefaultReportLength = 65;

    private SafeFileHandle? _handle;
    private byte[] _report = GC.AllocateArray<byte>(DefaultReportLength, pinned: true);

    public bool IsOpen => _handle is { IsInvalid: false, IsClosed: false };

    public string? DevicePath { get; private set; }

    public string? ProductName { get; private set; }

    public static bool IsCommandInterface(string devicePath) =>
        devicePath.Contains(CommandCollection, StringComparison.OrdinalIgnoreCase);

    public static string? FindDevicePath() =>
        NativeMethods.GetDeviceInterfaces(NativeMethods.HidInterfaceGuid).FirstOrDefault(IsCommandInterface);

    /// <summary>Opens the stick if it is plugged in. Returns false with a reason otherwise.</summary>
    public bool TryOpen(out string? error)
    {
        Close();
        error = null;

        string? path = FindDevicePath();
        if (path is null)
        {
            error = "Stick not found";
            return false;
        }

        SafeFileHandle handle = NativeMethods.CreateFile(path, NativeMethods.GENERIC_WRITE,
            NativeMethods.FILE_SHARE_READ | NativeMethods.FILE_SHARE_WRITE, 0, NativeMethods.OPEN_EXISTING, 0, 0);
        if (handle.IsInvalid)
        {
            error = $"Could not open the stick ({new System.ComponentModel.Win32Exception().Message})";
            handle.Dispose();
            return false;
        }

        int length = NativeMethods.TryGetCaps(handle, out HIDP_CAPS caps) && caps.OutputReportByteLength >= 4
            ? caps.OutputReportByteLength
            : DefaultReportLength;

        if (_report.Length != length)
        {
            _report = GC.AllocateArray<byte>(length, pinned: true);
        }

        _report[0] = ReportId;
        _handle = handle;
        DevicePath = path;
        string? manufacturer = NativeMethods.GetHidString(handle, manufacturer: true);
        string? product = NativeMethods.GetHidString(handle, manufacturer: false);
        ProductName = string.Join(' ', new[] { manufacturer, product }.Where(s => !string.IsNullOrEmpty(s))) is { Length: > 0 } name ? name : "USB stick";
        return true;
    }

    /// <summary>Sends one command. Returns the Win32 error code, or 0 on success.</summary>
    public int Write(KeyCode code, byte argument)
    {
        if (_handle is null || _handle.IsClosed)
        {
            return NativeMethods.ERROR_DEVICE_NOT_CONNECTED;
        }

        _report[2] = (byte)code;
        _report[3] = argument;

        fixed (byte* p = _report)
        {
            if (NativeMethods.WriteFile(_handle, p, _report.Length, out _, 0))
            {
                return 0;
            }
        }

        int error = System.Runtime.InteropServices.Marshal.GetLastPInvokeError();
        return error == 0 ? NativeMethods.ERROR_GEN_FAILURE : error;
    }

    public void Close()
    {
        _handle?.Dispose();
        _handle = null;
        DevicePath = null;
    }

    public void Dispose() => Close();
}
