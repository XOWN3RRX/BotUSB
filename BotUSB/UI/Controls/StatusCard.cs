using System.Drawing.Drawing2D;

namespace BotUSB.UI.Controls;

/// <summary>A card with status rows: glyph, label, value and a state dot.</summary>
internal sealed class StatusCard : Control
{
    private const int LogicalRowHeight = 34;

    private readonly Row[] _rows;
    private Font? _glyphFont;
    private Font? _captionFont;

    public StatusCard(params (string Glyph, string Label)[] rows)
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        _rows = [.. rows.Select(r => new Row(r.Glyph, r.Label))];
    }

    public int PreferredHeight => LogicalToDeviceUnits(LogicalRowHeight * _rows.Length + 12);

    /// <summary>Updates a row; repaints only when something changed (called from a timer).</summary>
    public void SetRow(int index, string value, Color? dot)
    {
        Row row = _rows[index];
        if (row.Value == value && row.Dot == dot)
        {
            return;
        }

        row.Value = value;
        row.Dot = dot;
        Invalidate();
    }

    protected override void OnFontChanged(EventArgs e)
    {
        _glyphFont?.Dispose();
        _captionFont?.Dispose();
        _glyphFont = _captionFont = null;
        base.OnFontChanged(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.Clear(Parent?.BackColor ?? Theme.Background);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var bounds = new RectangleF(0, 0, Width, Height);
        g.FillRoundedRectangle(Theme.Surface, bounds, LogicalToDeviceUnits(8));
        g.DrawRoundedRectangle(Theme.Border, bounds, LogicalToDeviceUnits(8));

        _glyphFont ??= Theme.CreateGlyphFont(Font, 1.1f);
        _captionFont ??= Theme.CreateCaptionFont(Font);
        const TextFormatFlags flags = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix;

        int pad = LogicalToDeviceUnits(14);
        int rowHeight = LogicalToDeviceUnits(LogicalRowHeight);
        int y = LogicalToDeviceUnits(6);
        int labelWidth = LogicalToDeviceUnits(64);

        foreach (Row row in _rows)
        {
            int x = pad;
            TextRenderer.DrawText(g, row.Glyph, _glyphFont, new Rectangle(x, y, LogicalToDeviceUnits(20), rowHeight), Theme.TextSecondary, flags);
            x += LogicalToDeviceUnits(28);
            TextRenderer.DrawText(g, row.Label, _captionFont, new Rectangle(x, y, labelWidth, rowHeight), Theme.TextSecondary, flags);
            x += labelWidth;

            int right = Width - pad;
            if (row.Dot is Color dot)
            {
                int size = LogicalToDeviceUnits(8);
                using var brush = new SolidBrush(dot);
                g.FillEllipse(brush, right - size, y + (rowHeight - size) / 2f, size, size);
                right -= size + LogicalToDeviceUnits(8);
            }

            TextRenderer.DrawText(g, row.Value, Font, new Rectangle(x, y, Math.Max(0, right - x), rowHeight), Theme.Text, flags | TextFormatFlags.Right);
            y += rowHeight;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _glyphFont?.Dispose();
            _captionFont?.Dispose();
        }

        base.Dispose(disposing);
    }

    private sealed class Row(string glyph, string label)
    {
        public string Glyph { get; } = glyph;

        public string Label { get; } = label;

        public string Value { get; set; } = string.Empty;

        public Color? Dot { get; set; }
    }
}
