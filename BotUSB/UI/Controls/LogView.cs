using System.Drawing.Drawing2D;
using System.Text;

namespace BotUSB.UI.Controls;

/// <summary>Owner-drawn log: time, colored level pill and message. Keeps the newest <see cref="MaxEntries"/> rows.</summary>
internal sealed class LogView : ListBox
{
    public const int MaxEntries = 5000;
    private const int LogicalItemHeight = 28;

    private Font? _captionFont;
    private Font? _pillFont;

    public LogView()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        DrawMode = DrawMode.OwnerDrawFixed;
        BorderStyle = BorderStyle.None;
        IntegralHeight = false;
        SelectionMode = SelectionMode.MultiExtended;
        BackColor = Theme.Surface;
        ForeColor = Theme.Text;
        ItemHeight = LogicalItemHeight;
    }

    public static Color LevelColor(LogLevel level) => (level, Theme.IsDark) switch
    {
        (LogLevel.Success, true) => Color.FromArgb(0x6C, 0xCB, 0x5F),
        (LogLevel.Success, false) => Color.FromArgb(0x0F, 0x7B, 0x0F),
        (LogLevel.Warning, true) => Color.FromArgb(0xFC, 0xE1, 0x00),
        (LogLevel.Warning, false) => Color.FromArgb(0x9D, 0x5D, 0x00),
        (LogLevel.Error, true) => Color.FromArgb(0xFF, 0x99, 0xA4),
        (LogLevel.Error, false) => Color.FromArgb(0xC4, 0x2B, 0x1C),
        (LogLevel.Data, _) => Theme.Accent,
        _ => Theme.TextSecondary,
    };

    private static string LevelName(LogLevel level) => level switch
    {
        LogLevel.Success => "OK",
        LogLevel.Warning => "WARN",
        LogLevel.Error => "ERROR",
        LogLevel.Data => "DATA",
        _ => "INFO",
    };

    /// <summary>Appends entries in one update and drops the oldest rows past the limit.</summary>
    public void Append(List<LogEntry> entries, bool scrollToEnd)
    {
        if (entries.Count == 0)
        {
            return;
        }

        BeginUpdate();
        try
        {
            int overflow = Items.Count + entries.Count - MaxEntries;
            if (overflow > 0)
            {
                // Trim in batches so a busy log doesn't trim on every tick.
                overflow = Math.Min(Items.Count, overflow + MaxEntries / 10);
                for (int i = 0; i < overflow; i++)
                {
                    Items.RemoveAt(0);
                }
            }

            int skip = Math.Max(0, entries.Count - MaxEntries);
            object[] rows = new object[entries.Count - skip];
            for (int i = 0; i < rows.Length; i++)
            {
                rows[i] = entries[skip + i];
            }

            Items.AddRange(rows);

            if (scrollToEnd)
            {
                TopIndex = Math.Max(0, Items.Count - Math.Max(1, ClientSize.Height / ItemHeight));
            }
        }
        finally
        {
            EndUpdate();
        }
    }

    public void CopySelection()
    {
        IEnumerable<object> rows = SelectedItems.Count > 0 ? SelectedItems.Cast<object>() : Items.Cast<object>();
        var text = new StringBuilder();
        foreach (LogEntry entry in rows.OfType<LogEntry>())
        {
            text.Append(entry.Time.ToString("HH:mm:ss.fff")).Append('\t').Append(LevelName(entry.Level)).Append('\t').AppendLine(entry.Message);
        }

        if (text.Length > 0)
        {
            Clipboard.SetText(text.ToString());
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyData == (Keys.Control | Keys.C))
        {
            CopySelection();
            e.Handled = true;
        }
        else if (e.KeyData == (Keys.Control | Keys.A))
        {
            BeginUpdate();
            for (int i = 0; i < Items.Count; i++)
            {
                SetSelected(i, true);
            }

            EndUpdate();
            e.Handled = true;
        }

        base.OnKeyDown(e);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ItemHeight = LogicalToDeviceUnits(LogicalItemHeight);
    }

    protected override void OnDpiChangedAfterParent(EventArgs e)
    {
        base.OnDpiChangedAfterParent(e);
        ItemHeight = LogicalToDeviceUnits(LogicalItemHeight);
    }

    protected override void OnFontChanged(EventArgs e)
    {
        _captionFont?.Dispose();
        _pillFont?.Dispose();
        _captionFont = _pillFont = null;
        base.OnFontChanged(e);
    }

    protected override void OnDrawItem(DrawItemEventArgs e)
    {
        Graphics g = e.Graphics;
        using (var background = new SolidBrush(BackColor))
        {
            g.FillRectangle(background, e.Bounds);
        }

        if (e.Index < 0 || e.Index >= Items.Count || Items[e.Index] is not LogEntry entry)
        {
            return;
        }

        g.SmoothingMode = SmoothingMode.AntiAlias;
        int pad = LogicalToDeviceUnits(2);
        var row = Rectangle.Inflate(e.Bounds, -pad * 2, -1);
        if ((e.State & DrawItemState.Selected) != 0)
        {
            g.FillRoundedRectangle(Theme.SurfaceSelected, row, LogicalToDeviceUnits(4));
        }

        _captionFont ??= Theme.CreateCaptionFont(Font);
        _pillFont ??= Theme.CreateStrongFont(Font, 0.75f);
        const TextFormatFlags flags = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix;

        int x = row.Left + LogicalToDeviceUnits(8);
        int timeWidth = LogicalToDeviceUnits(84);
        TextRenderer.DrawText(g, entry.Time.ToString("HH:mm:ss.fff"), _captionFont, new Rectangle(x, row.Top, timeWidth, row.Height), Theme.TextSecondary, flags);
        x += timeWidth;

        // Level pill: tinted background with the level color as text.
        Color color = LevelColor(entry.Level);
        int pillWidth = LogicalToDeviceUnits(46);
        int pillHeight = LogicalToDeviceUnits(18);
        var pill = new Rectangle(x, row.Top + (row.Height - pillHeight) / 2, pillWidth, pillHeight);
        g.FillRoundedRectangle(Theme.Blend(color, BackColor, 0.82), pill, pillHeight / 2f);
        TextRenderer.DrawText(g, LevelName(entry.Level), _pillFont, pill, color,
            TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);
        x = pill.Right + LogicalToDeviceUnits(10);

        Color text = entry.Level == LogLevel.Error ? color : Theme.Text;
        TextRenderer.DrawText(g, entry.Message, Font, new Rectangle(x, row.Top, Math.Max(0, row.Right - x - LogicalToDeviceUnits(8)), row.Height), text, flags);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _captionFont?.Dispose();
            _pillFont?.Dispose();
        }

        base.Dispose(disposing);
    }
}
