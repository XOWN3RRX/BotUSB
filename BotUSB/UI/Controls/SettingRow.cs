using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace BotUSB.UI.Controls;

/// <summary>A settings card: title and description on the left, a control on the right.</summary>
internal sealed class SettingRow : Control
{
    private Font? _captionFont;

    public SettingRow(string title, string description, Control action)
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Theme.Surface;
        Text = title;
        Description = description;
        Action = action;
        Controls.Add(action);
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Description { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Control Action { get; }

    protected override void OnFontChanged(EventArgs e)
    {
        _captionFont?.Dispose();
        _captionFont = null;
        base.OnFontChanged(e);
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        int pad = LogicalToDeviceUnits(16);
        Action.Location = new Point(Width - pad - Action.Width, (Height - Action.Height) / 2);
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.Clear(Parent?.BackColor ?? Theme.Background);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        float radius = LogicalToDeviceUnits(6);
        var bounds = new RectangleF(0, 0, Width, Height);
        g.FillRoundedRectangle(Theme.Surface, bounds, radius);
        g.DrawRoundedRectangle(Theme.Border, bounds, radius);

        _captionFont ??= Theme.CreateCaptionFont(Font);
        const TextFormatFlags flags = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix;
        int pad = LogicalToDeviceUnits(16);
        int width = Math.Max(0, Action.Left - pad * 2);
        int titleHeight = TextRenderer.MeasureText(g, "Ag", Font, Size.Empty, flags).Height;
        int captionHeight = TextRenderer.MeasureText(g, "Ag", _captionFont, Size.Empty, flags).Height;
        int top = (Height - titleHeight - captionHeight) / 2;

        TextRenderer.DrawText(g, Text, Font, new Rectangle(pad, top, width, titleHeight), Enabled ? Theme.Text : Theme.TextDisabled, flags);
        TextRenderer.DrawText(g, Description, _captionFont, new Rectangle(pad, top + titleHeight, width, captionHeight), Enabled ? Theme.TextSecondary : Theme.TextDisabled, flags);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _captionFont?.Dispose();
        }

        base.Dispose(disposing);
    }
}
