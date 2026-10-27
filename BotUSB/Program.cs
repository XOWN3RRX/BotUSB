using BotUSB.UI;

namespace BotUSB;

internal static class Program
{
    private const string InstanceMutexName = @"Local\BotUSB.SingleInstance";
    private const string ShowEventName = @"Local\BotUSB.Show";

    private static Icon? s_appIcon;

    public static Icon AppIcon => s_appIcon ??= LoadAppIcon();

    [STAThread]
    private static void Main()
    {
        using var mutex = new Mutex(initiallyOwned: true, InstanceMutexName, out bool isFirstInstance);
        using var showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowEventName);

        if (!isFirstInstance)
        {
            // Only one app can own the stick and the port: bring the running one to the front instead.
            Interop.NativeMethods.AllowSetForegroundWindow(Interop.NativeMethods.ASFW_ANY);
            showEvent.Set();
            return;
        }

        ApplicationConfiguration.Initialize();
        Theme.Initialize();

        // A bug in a UI handler must not stop scripts: log it instead of showing the crash dialog.
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => AppLog.Error($"Unexpected error: {e.Exception}");

#pragma warning disable WFO5001 // Dark mode for common controls (scroll bars, text boxes, dialogs).
        Application.SetColorMode(Theme.IsDark ? SystemColorMode.Dark : SystemColorMode.Classic);
#pragma warning restore WFO5001

        var mainForm = new MainForm();

        // Wait for other launches on a pool thread; the registration ends with the process.
        ThreadPool.RegisterWaitForSingleObject(showEvent, (_, _) =>
        {
            if (mainForm.IsHandleCreated && !mainForm.IsDisposed)
            {
                mainForm.BeginInvoke(mainForm.RestoreFromTray);
            }
        }, null, Timeout.Infinite, executeOnlyOnce: false);

        Application.Run(mainForm);
    }

    private static Icon LoadAppIcon()
    {
        using Stream? stream = typeof(Program).Assembly.GetManifestResourceStream("BotUSB.AppIcon.ico");
        return stream is null ? SystemIcons.Application : new Icon(stream);
    }
}
