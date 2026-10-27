using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace BotUSB.UI.Controls;

internal enum ButtonKind
{
    Standard,
    Accent,
    Subtle,
}

/// <summary>A flat, rounded button with an optional icon glyph or color swatch.</summary>
internal sealed class ModernButton : Button
{
    private bool _hover;
    private bool _pressed;
    private Font? _glyphFont;

    public ModernButton()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        UseVisualStyleBackColor = false;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ButtonKind Kind { get; set; } = ButtonKind.Standard;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string? Glyph { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color Swatch { get; set; } = Color.Empty;

    /// <summary>Drop-down style: content on the left and a chevron on the right.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool ShowChevron { get; set; }

    protected override void OnFontChanged(EventArgs e)
    {
        _glyphFont?.Dispose();
        _glyphFont = null;
        base.OnFontChanged(e);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hover = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hover = false;
        _pressed = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs mevent)
    {
        if (mevent.Button == MouseButtons.Left)
        {
            _pressed = true;
            Invalidate();
        }

        base.OnMouseDown(mevent);
    }

    protected override void OnMouseUp(MouseEventArgs mevent)
    {
        _pressed = false;
        Invalidate();
        base.OnMouseUp(mevent);
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        Invalidate();
        base.OnEnabledChanged(e);
    }

    protected override void OnPaint(PaintEventArgs pevent)
    {
        Graphics g = pevent.Graphics;
        g.Clear(Parent?.BackColor ?? Theme.Background);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        (Color fill, Color border, Color text) = GetColors();
        float radius = LogicalToDeviceUnits(4);
        var bounds = new RectangleF(0, 0, Width, Height);

        if (fill.A > 0)
        {
            g.FillRoundedRectangle(fill, bounds, radius);
        }

        if (border.A > 0)
        {
            g.DrawRoundedRectangle(border, bounds, radius);
        }

        if (Focused && ShowFocusCues)
        {
            var focus = bounds;
            focus.Inflate(-LogicalToDeviceUnits(2), -LogicalToDeviceUnits(2));
            g.DrawRoundedRectangle(Theme.Text, focus, radius - 1, LogicalToDeviceUnits(1));
        }

        DrawContent(g, text);
    }

    private void DrawContent(Graphics g, Color textColor)
    {
        const TextFormatFlags flags = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis;

        _glyphFont ??= Theme.CreateGlyphFont(Font);
        // Measure without VerticalCenter/EndEllipsis: with an empty size those flags return a wrong (too small) width.
        const TextFormatFlags measureFlags = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine;
        int gap = LogicalToDeviceUnits(8);

        Size glyphSize = Size.Empty;
        if (!string.IsNullOrEmpty(Glyph))
        {
            glyphSize = TextRenderer.MeasureText(g, Glyph, _glyphFont, Size.Empty, measureFlags);
        }
        else if (!Swatch.IsEmpty)
        {
            int s = LogicalToDeviceUnits(14);
            glyphSize = new Size(s, s);
        }

        Size textSize = string.IsNullOrEmpty(Text) ? Size.Empty : TextRenderer.MeasureText(g, Text, Font, Size.Empty, measureFlags);
        int contentWidth = glyphSize.Width + textSize.Width + (glyphSize.Width > 0 && textSize.Width > 0 ? gap : 0);
        int x = contentWidth > Width ? LogicalToDeviceUnits(4) : (Width - contentWidth) / 2;
        int right = Width - LogicalToDeviceUnits(4);

        if (ShowChevron)
        {
            using var chevronFont = Theme.CreateGlyphFont(Font, 0.75f);
            Size chevronSize = TextRenderer.MeasureText(g, Glyphs.ChevronDown, chevronFont, Size.Empty, measureFlags);
            int chevronX = Width - LogicalToDeviceUnits(12) - chevronSize.Width;
            TextRenderer.DrawText(g, Glyphs.ChevronDown, chevronFont, new Rectangle(chevronX, 0, chevronSize.Width, Height),
                Enabled ? Theme.TextSecondary : Theme.TextDisabled, flags);
            x = LogicalToDeviceUnits(12);
            right = chevronX - LogicalToDeviceUnits(4);
        }

        if (!string.IsNullOrEmpty(Glyph))
        {
            TextRenderer.DrawText(g, Glyph, _glyphFont, new Rectangle(x, 0, glyphSize.Width, Height), textColor, flags);
            x += glyphSize.Width + gap;
        }
        else if (!Swatch.IsEmpty)
        {
            var swatch = new Rectangle(x, (Height - glyphSize.Height) / 2, glyphSize.Width, glyphSize.Height);
            g.FillRoundedRectangle(Swatch, swatch, LogicalToDeviceUnits(3));
            g.DrawRoundedRectangle(Theme.Border, swatch, LogicalToDeviceUnits(3));
            x += glyphSize.Width + gap;
        }

        if (textSize.Width > 0)
        {
            TextRenderer.DrawText(g, Text, Font, new Rectangle(x, 0, Math.Max(0, right - x), Height), textColor, flags);
        }
    }

    private (Color Fill, Color Border, Color Text) GetColors()
    {
        if (!Enabled)
        {
            return Kind == ButtonKind.Accent
                ? (Theme.Blend(Theme.Accent, Theme.Background, 0.6), Color.Empty, Theme.Blend(Theme.OnAccent, Theme.Background, 0.4))
                : (Kind == ButtonKind.Subtle ? Color.Empty : Theme.Surface, Kind == ButtonKind.Subtle ? Color.Empty : Theme.Border, Theme.TextDisabled);
        }

        return Kind switch
        {
            ButtonKind.Accent => (_pressed ? Theme.AccentPressed : _hover ? Theme.AccentHover : Theme.Accent, Color.Empty, Theme.OnAccent),
            ButtonKind.Subtle => (_pressed ? Theme.SurfacePressed : _hover ? Theme.SurfaceHover : Color.Empty, Color.Empty, Theme.Text),
            _ => (_pressed ? Theme.SurfacePressed : _hover ? Theme.SurfaceHover : Theme.Surface, Theme.Border, Theme.Text),
        };
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _glyphFont?.Dispose();
        }

        base.Dispose(disposing);
    }
}
