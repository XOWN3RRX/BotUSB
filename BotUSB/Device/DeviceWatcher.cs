using System.Runtime.InteropServices;
using BotUSB.Interop;

namespace BotUSB.Device;

/// <summary>
/// Reports HID devices being plugged in or removed. Uses CM_Register_Notification, so it needs no window
/// and keeps working while the app is hidden in the tray.
/// </summary>
internal sealed unsafe class DeviceWatcher : IDisposable
{
    private GCHandle _self;
    private nint _registration;

    /// <summary>Raised on a thread pool thread with the device path and whether it arrived.</summary>
    public event Action<string, bool>? Changed;

    public DeviceWatcher()
    {
        _self = GCHandle.Alloc(this);
        var filter = new CM_NOTIFY_FILTER
        {
            cbSize = (uint)sizeof(CM_NOTIFY_FILTER),
            FilterType = NativeMethods.CM_NOTIFY_FILTER_TYPE_DEVICEINTERFACE,
            ClassGuid = NativeMethods.HidInterfaceGuid,
        };

        int cr = NativeMethods.CM_Register_Notification(filter, GCHandle.ToIntPtr(_self), &OnNotification, out _registration);
        if (cr != NativeMethods.CR_SUCCESS)
        {
            _self.Free();
            throw new InvalidOperationException($"CM_Register_Notification failed (0x{cr:X}).");
        }
    }

    [UnmanagedCallersOnly]
    private static uint OnNotification(nint notify, nint context, int action, byte* eventData, uint eventDataSize)
    {
        // CM_NOTIFY_EVENT_DATA: FilterType, Reserved, then ClassGuid and the null-terminated symbolic link.
        const int SymbolicLinkOffset = 8 + 16;

        if (action is NativeMethods.CM_NOTIFY_ACTION_DEVICEINTERFACEARRIVAL or NativeMethods.CM_NOTIFY_ACTION_DEVICEINTERFACEREMOVAL
            && eventDataSize > SymbolicLinkOffset
            && GCHandle.FromIntPtr(context).Target is DeviceWatcher watcher)
        {
            try
            {
                string path = new((char*)(eventData + SymbolicLinkOffset));
                watcher.Changed?.Invoke(path, action == NativeMethods.CM_NOTIFY_ACTION_DEVICEINTERFACEARRIVAL);
            }
            catch (Exception ex)
            {
                // Never let an exception escape into the native callback.
                AppLog.Error("Device notification failed: " + ex.Message);
            }
        }

        return 0;
    }

    public void Dispose()
    {
        if (_registration != 0)
        {
            // Waits for running callbacks, so the GCHandle can be freed afterwards.
            NativeMethods.CM_Unregister_Notification(_registration);
            _registration = 0;
        }

        if (_self.IsAllocated)
        {
            _self.Free();
        }
    }
}
