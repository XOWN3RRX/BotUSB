using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing.Drawing2D;

namespace BotUSB.UI.Controls;

/// <summary>A rounded text box with an optional leading glyph (search, code and port fields).</summary>
internal sealed class TextField : Control
{
    private Font? _glyphFont;

    public TextField()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Theme.Surface;

        TextBox = new TextBox
        {
            BorderStyle = BorderStyle.None,
            BackColor = Theme.Surface,
            ForeColor = Theme.Text,
        };

        TextBox.GotFocus += (_, _) => Invalidate();
        TextBox.LostFocus += (_, _) => Invalidate();
        TextBox.TextChanged += (_, e) => OnTextChanged(e);
        Controls.Add(TextBox);
        Cursor = Cursors.IBeam;
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TextBox TextBox { get; }

    /// <summary>Icon drawn on the left, or null for none.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string? Glyph { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string PlaceholderText
    {
        get => TextBox.PlaceholderText;
        set => TextBox.PlaceholderText = value;
    }

    [AllowNull]
    public override string Text
    {
        get => TextBox.Text;
        set => TextBox.Text = value;
    }

    protected override void OnFontChanged(EventArgs e)
    {
        _glyphFont?.Dispose();
        _glyphFont = null;
        base.OnFontChanged(e);
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);
        TextBox.ForeColor = Enabled ? Theme.Text : Theme.TextDisabled;
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        TextBox.Focus();
        base.OnMouseDown(e);
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        int left = LogicalToDeviceUnits(Glyph is null ? 10 : 34);
        int right = LogicalToDeviceUnits(10);
        TextBox.Bounds = new Rectangle(left, (Height - TextBox.Height) / 2, Math.Max(0, Width - left - right), TextBox.Height);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.Clear(Parent?.BackColor ?? Theme.Background);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        float radius = LogicalToDeviceUnits(4);
        var bounds = new RectangleF(0, 0, Width, Height);
        g.FillRoundedRectangle(Theme.Surface, bounds, radius);
        g.DrawRoundedRectangle(Theme.Border, bounds, radius);

        if (TextBox.Focused)
        {
            // Fluent style: accent underline when focused.
            float thickness = LogicalToDeviceUnits(2);
            using var clip = Theme.RoundedRectangle(bounds, radius);
            g.SetClip(clip);
            using var brush = new SolidBrush(Theme.Accent);
            g.FillRectangle(brush, 0, Height - thickness, Width, thickness);
            g.ResetClip();
        }

        if (Glyph is not null)
        {
            _glyphFont ??= Theme.CreateGlyphFont(Font, 1f);
            TextRenderer.DrawText(g, Glyph, _glyphFont,
                new Rectangle(LogicalToDeviceUnits(10), 0, LogicalToDeviceUnits(20), Height),
                Enabled ? Theme.TextSecondary : Theme.TextDisabled, TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }
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
