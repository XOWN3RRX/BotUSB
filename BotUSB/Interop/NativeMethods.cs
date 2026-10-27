using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace BotUSB.Interop;

/// <summary>Filter for <see cref="NativeMethods.CM_Register_Notification"/> (CM_NOTIFY_FILTER, device interface variant).</summary>
[StructLayout(LayoutKind.Explicit, Size = 416)]
internal struct CM_NOTIFY_FILTER
{
    [FieldOffset(0)] public uint cbSize;
    [FieldOffset(4)] public uint Flags;
    [FieldOffset(8)] public int FilterType;
    [FieldOffset(12)] public uint Reserved;
    [FieldOffset(16)] public Guid ClassGuid;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct HIDP_CAPS
{
    public ushort Usage;
    public ushort UsagePage;
    public ushort InputReportByteLength;
    public ushort OutputReportByteLength;
    public ushort FeatureReportByteLength;
    public fixed ushort Reserved[17];
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

internal static unsafe partial class NativeMethods
{
    /// <summary>GUID_DEVINTERFACE_HID.</summary>
    public static readonly Guid HidInterfaceGuid = new(0x4D1E55B2, 0xF16F, 0x11CF, 0x88, 0xCB, 0x00, 0x11, 0x11, 0x00, 0x00, 0x30);

    public const uint GENERIC_READ = 0x80000000;
    public const uint GENERIC_WRITE = 0x40000000;
    public const uint FILE_SHARE_READ = 0x1;
    public const uint FILE_SHARE_WRITE = 0x2;
    public const uint OPEN_EXISTING = 3;

    public const int ERROR_DEVICE_NOT_CONNECTED = 1167;
    public const int ERROR_GEN_FAILURE = 31;
    public const int ERROR_INVALID_HANDLE = 6;
    public const int ERROR_BAD_COMMAND = 22;
    public const int ERROR_OPERATION_ABORTED = 995;

    public const int CR_SUCCESS = 0;
    public const int CR_BUFFER_SMALL = 0x1A;
    public const uint CM_GET_DEVICE_INTERFACE_LIST_PRESENT = 0;
    public const int CM_NOTIFY_FILTER_TYPE_DEVICEINTERFACE = 0;
    public const int CM_NOTIFY_ACTION_DEVICEINTERFACEARRIVAL = 0;
    public const int CM_NOTIFY_ACTION_DEVICEINTERFACEREMOVAL = 1;

    public const int HIDP_STATUS_SUCCESS = 0x00110000;

    public const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    public const int ASFW_ANY = -1;

    // cfgmgr32

    [LibraryImport("cfgmgr32.dll", EntryPoint = "CM_Get_Device_Interface_List_SizeW")]
    public static partial int CM_Get_Device_Interface_List_Size(out uint pulLen, in Guid interfaceClassGuid, char* pDeviceID, uint ulFlags);

    [LibraryImport("cfgmgr32.dll", EntryPoint = "CM_Get_Device_Interface_ListW")]
    public static partial int CM_Get_Device_Interface_List(in Guid interfaceClassGuid, char* pDeviceID, char* buffer, uint bufferLen, uint ulFlags);

    [LibraryImport("cfgmgr32.dll")]
    public static partial int CM_Register_Notification(
        in CM_NOTIFY_FILTER pFilter,
        nint pContext,
        delegate* unmanaged<nint, nint, int, byte*, uint, uint> pCallback,
        out nint pNotifyContext);

    [LibraryImport("cfgmgr32.dll")]
    public static partial int CM_Unregister_Notification(nint notifyContext);

    // kernel32

    [LibraryImport("kernel32.dll", EntryPoint = "CreateFileW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    public static partial SafeFileHandle CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode, nint lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, nint hTemplateFile);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool WriteFile(SafeFileHandle hFile, byte* lpBuffer, int nNumberOfBytesToWrite, out int lpNumberOfBytesWritten, nint lpOverlapped);

    // hid

    [LibraryImport("hid.dll")]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool HidD_GetPreparsedData(SafeFileHandle hidDeviceObject, out nint preparsedData);

    [LibraryImport("hid.dll")]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool HidD_FreePreparsedData(nint preparsedData);

    [LibraryImport("hid.dll")]
    public static partial int HidP_GetCaps(nint preparsedData, out HIDP_CAPS capabilities);

    [LibraryImport("hid.dll")]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool HidD_GetProductString(SafeFileHandle hidDeviceObject, char* buffer, int bufferLength);

    [LibraryImport("hid.dll")]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool HidD_GetManufacturerString(SafeFileHandle hidDeviceObject, char* buffer, int bufferLength);

    // user32 / dwmapi

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetForegroundWindow(nint hWnd);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool AllowSetForegroundWindow(int dwProcessId);

    [LibraryImport("dwmapi.dll")]
    public static partial int DwmSetWindowAttribute(nint hwnd, int dwAttribute, void* pvAttribute, int cbAttribute);

    // Helpers

    public static void SetDarkTitleBar(nint hWnd, bool dark)
    {
        int value = dark ? 1 : 0;
        DwmSetWindowAttribute(hWnd, DWMWA_USE_IMMERSIVE_DARK_MODE, &value, sizeof(int));
    }

    /// <summary>Paths of all present device interfaces of a class (e.g. every HID collection).</summary>
    public static List<string> GetDeviceInterfaces(Guid interfaceClass)
    {
        var result = new List<string>();

        // The list can grow between the size query and the read; retry on CR_BUFFER_SMALL.
        for (int attempt = 0; attempt < 5; attempt++)
        {
            if (CM_Get_Device_Interface_List_Size(out uint length, interfaceClass, null, CM_GET_DEVICE_INTERFACE_LIST_PRESENT) != CR_SUCCESS || length <= 1)
            {
                return result;
            }

            char[] buffer = new char[length];
            int cr;
            fixed (char* p = buffer)
            {
                cr = CM_Get_Device_Interface_List(interfaceClass, null, p, length, CM_GET_DEVICE_INTERFACE_LIST_PRESENT);
            }

            if (cr == CR_BUFFER_SMALL)
            {
                continue;
            }

            if (cr != CR_SUCCESS)
            {
                return result;
            }

            // Double-null-terminated list of strings.
            foreach (string path in new string(buffer).Split('\0', StringSplitOptions.RemoveEmptyEntries))
            {
                result.Add(path);
            }

            return result;
        }

        return result;
    }

    public static string? GetHidString(SafeFileHandle handle, bool manufacturer)
    {
        // HID strings are limited to 126 characters plus the terminator.
        char* buffer = stackalloc char[128];
        bool ok = manufacturer
            ? HidD_GetManufacturerString(handle, buffer, 256)
            : HidD_GetProductString(handle, buffer, 256);
        return ok ? new string(buffer).Trim() : null;
    }

    public static bool TryGetCaps(SafeFileHandle handle, out HIDP_CAPS caps)
    {
        caps = default;
        if (!HidD_GetPreparsedData(handle, out nint preparsed))
        {
            return false;
        }

        try
        {
            return HidP_GetCaps(preparsed, out caps) == HIDP_STATUS_SUCCESS;
        }
        finally
        {
            HidD_FreePreparsedData(preparsed);
        }
    }
}
