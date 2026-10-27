using System.Collections.Concurrent;

namespace BotUSB;

internal enum LogLevel
{
    Info,
    Success,
    Warning,
    Error,
    Data,
}

internal readonly record struct LogEntry(DateTime Time, LogLevel Level, string Message);

/// <summary>
/// Thread-safe log. Any thread can write without waiting; the main window drains it on a timer, so the key
/// sending thread never blocks on the UI.
/// </summary>
internal static class AppLog
{
    private const int MaxPending = 10_000;

    private static readonly ConcurrentQueue<LogEntry> s_pending = new();
    private static int s_dropped;

    public static void Info(string message) => Write(LogLevel.Info, message);

    public static void Success(string message) => Write(LogLevel.Success, message);

    public static void Warning(string message) => Write(LogLevel.Warning, message);

    public static void Error(string message) => Write(LogLevel.Error, message);

    public static void Data(string message) => Write(LogLevel.Data, message);

    public static void Write(LogLevel level, string message)
    {
        // A flood of messages while the window is hidden must not grow memory without limit.
        if (s_pending.Count >= MaxPending)
        {
            Interlocked.Increment(ref s_dropped);
            return;
        }

        s_pending.Enqueue(new LogEntry(DateTime.Now, level, message));
    }

    /// <summary>Moves pending entries into <paramref name="target"/>.</summary>
    public static void Drain(List<LogEntry> target)
    {
        while (s_pending.TryDequeue(out LogEntry entry))
        {
            target.Add(entry);
        }

        int dropped = Interlocked.Exchange(ref s_dropped, 0);
        if (dropped > 0)
        {
            target.Add(new LogEntry(DateTime.Now, LogLevel.Warning, $"{dropped} log messages were skipped"));
        }
    }
}
