using System.Diagnostics;
using System.Threading.Channels;
using BotUSB.Interop;

namespace BotUSB.Device;

/// <summary>
/// Sends commands to the stick. Every caller (TCP clients, test buttons) only enqueues; one worker owns the
/// device handle and writes the commands in order, so reports never mix.
/// </summary>
internal sealed class BotEngine : IDisposable
{
    private readonly Channel<BotCommand> _queue = Channel.CreateUnbounded<BotCommand>(new UnboundedChannelOptions { SingleReader = true });
    private readonly HidStick _stick = new();
    private readonly DeviceWatcher? _watcher;
    private readonly Task _worker;

    private int _rescanRequested = 1;
    private int _reopen;
    private volatile bool _connected;
    private volatile string? _deviceName;
    private long _lastWriteTicks;
    private long _averageWriteTicks;
    private long _writes;
    private long _dropped;
    private long _lastDropLog;
    private int _queued;

    public BotEngine()
    {
        try
        {
            _watcher = new DeviceWatcher();
            _watcher.Changed += OnDeviceChanged;
        }
        catch (InvalidOperationException ex)
        {
            AppLog.Warning("Plug and unplug detection is not available: " + ex.Message);
        }

        _worker = Task.Run(RunAsync);
    }

    public bool IsConnected => _connected;

    public string? DeviceName => _deviceName;

    /// <summary>Commands waiting to be sent. Counted here because a single-reader channel can't count.</summary>
    public int QueueLength => Volatile.Read(ref _queued);

    public long WriteCount => Interlocked.Read(ref _writes);

    public TimeSpan LastWriteTime => Stopwatch.GetElapsedTime(0, Interlocked.Read(ref _lastWriteTicks));

    public TimeSpan AverageWriteTime => Stopwatch.GetElapsedTime(0, Interlocked.Read(ref _averageWriteTicks));

    public void Enqueue(BotCommand command)
    {
        Interlocked.Increment(ref _queued);
        if (!_queue.Writer.TryWrite(command))
        {
            Interlocked.Decrement(ref _queued); // Shutting down.
        }
    }

    private void OnDeviceChanged(string path, bool arrived)
    {
        if (!HidStick.IsCommandInterface(path))
        {
            return;
        }

        if (!arrived)
        {
            // Show the state right away; the worker closes the handle. A quick replug can reuse the same
            // path, so the old handle must be reopened even if the path is back.
            _connected = false;
            Interlocked.Exchange(ref _reopen, 1);
        }

        Interlocked.Exchange(ref _rescanRequested, 1);
        Enqueue(BotCommand.Rescan);
    }

    private async Task RunAsync()
    {
        Rescan();

        await foreach (BotCommand command in _queue.Reader.ReadAllAsync().ConfigureAwait(false))
        {
            Interlocked.Decrement(ref _queued);
            if (Interlocked.Exchange(ref _rescanRequested, 0) == 1)
            {
                Rescan();
            }

            try
            {
                Execute(command);
            }
            catch (Exception ex)
            {
                AppLog.Error($"{command.Action} failed: {ex.Message}");
            }
        }
    }

    private void Rescan()
    {
        Interlocked.Exchange(ref _rescanRequested, 0);
        bool wasOpen = _stick.IsOpen;
        bool reopen = Interlocked.Exchange(ref _reopen, 0) == 1;

        // A handle to an unplugged device stays "valid" until closed, so check that the device is still there.
        if (wasOpen && !reopen && HidStick.FindDevicePath() == _stick.DevicePath)
        {
            _connected = true;
            return;
        }

        if (_stick.TryOpen(out string? error))
        {
            _deviceName = _stick.ProductName;
            _connected = true;
            AppLog.Success($"Connected to {_stick.ProductName}");
        }
        else
        {
            _connected = false;
            if (wasOpen)
            {
                AppLog.Warning("Stick disconnected");
            }
            else if (error != "Stick not found")
            {
                AppLog.Error(error!);
            }
        }
    }

    private void Execute(BotCommand command)
    {
        switch (command.Action)
        {
            case BotAction.DriverTest:
                RunDriverTest();
                return;
            case BotAction.Rescan:
                return; // The loop already rescanned.
        }

        if (!_connected)
        {
            ReportDropped();
            return;
        }

        switch (command.Action)
        {
            case BotAction.Tap:
                // Tap codes release themselves; a NULL would only halve the speed (the stick takes ~30 ms per write).
                _ = Write(command.Keys[0]) && (KeyCodes.ReleasesItself(command.Keys[0]) || Write(KeyCode.NULL));
                break;
            case BotAction.Hold:
                Write(command.Keys[0]);
                break;
            case BotAction.WithArgument:
                Write(command.Keys[0], command.Argument);
                break;
            case BotAction.Combo:
                WriteCombo(command.Keys);
                break;
            case BotAction.Type:
                TypeText(command.Text ?? string.Empty);
                break;
            case BotAction.ReleaseAll:
                Write(KeyCode.NULL);
                break;
        }
    }

    private bool WriteCombo(ReadOnlySpan<KeyCode> keys)
    {
        foreach (KeyCode key in keys)
        {
            if (!Write(key))
            {
                return false;
            }
        }

        return Write(KeyCode.NULL);
    }

    private void TypeText(string text)
    {
        HashSet<char>? skipped = null;

        foreach (char c in text)
        {
            if (!TextKeyMap.TryGetKeys(c, out KeyCode[] keys))
            {
                (skipped ??= []).Add(c);
                continue;
            }

            if (!WriteCombo(keys))
            {
                return;
            }
        }

        if (skipped is not null)
        {
            AppLog.Warning("Can't type: " + string.Join(' ', skipped.Select(c => char.IsControl(c) ? $"U+{(int)c:X4}" : c.ToString())));
        }
    }

    private void RunDriverTest()
    {
        Rescan();
        if (!_connected)
        {
            AppLog.Error("Driver test failed: the stick is not connected");
            return;
        }

        long start = Stopwatch.GetTimestamp();
        if (Write(KeyCode.NULL))
        {
            AppLog.Success($"Driver test passed: {_stick.ProductName} answered in {Stopwatch.GetElapsedTime(start).TotalMilliseconds:0.00} ms");
        }
        else
        {
            AppLog.Error("Driver test failed");
        }
    }

    private bool Write(KeyCode code, byte argument = 0)
    {
        long start = Stopwatch.GetTimestamp();
        int error = _stick.Write(code, argument);
        long elapsed = Stopwatch.GetTimestamp() - start;

        if (error == 0)
        {
            Interlocked.Exchange(ref _lastWriteTicks, elapsed);
            long average = Interlocked.Read(ref _averageWriteTicks);
            Interlocked.Exchange(ref _averageWriteTicks, average == 0 ? elapsed : average + (elapsed - average) / 16);
            Interlocked.Increment(ref _writes);
            return true;
        }

        _stick.Close();
        _connected = false;
        Interlocked.Exchange(ref _rescanRequested, 1); // Reconnect on the next command if it is back.
        AppLog.Error(error is NativeMethods.ERROR_DEVICE_NOT_CONNECTED or NativeMethods.ERROR_GEN_FAILURE or NativeMethods.ERROR_INVALID_HANDLE or NativeMethods.ERROR_OPERATION_ABORTED
            ? "Stick disconnected while sending"
            : $"Sending to the stick failed: {new System.ComponentModel.Win32Exception(error).Message}");
        return false;
    }

    private void ReportDropped()
    {
        long dropped = Interlocked.Increment(ref _dropped);
        long now = Environment.TickCount64;

        // One message per second at most, however fast scripts send.
        if (now - _lastDropLog >= 1000)
        {
            _lastDropLog = now;
            Interlocked.Exchange(ref _dropped, 0);
            AppLog.Warning(dropped == 1 ? "Stick not connected: command ignored" : $"Stick not connected: {dropped} commands ignored");
        }
    }

    public void Dispose()
    {
        _queue.Writer.TryComplete();
        _watcher?.Dispose();
        _worker.Wait(TimeSpan.FromSeconds(2));

        // Never leave keys or buttons held after exit.
        if (_stick.IsOpen && _worker.IsCompleted)
        {
            _stick.Write(KeyCode.NULL, 0);
        }

        _stick.Dispose();
    }
}
