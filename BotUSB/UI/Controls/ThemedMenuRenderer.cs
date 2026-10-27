using System.Drawing.Drawing2D;

namespace BotUSB.UI.Controls;

/// <summary>Renders context menus with the app theme (rounded highlight, glyph check marks).</summary>
internal sealed class ThemedMenuRenderer() : ToolStripProfessionalRenderer(new ThemedColorTable())
{
    public static void Apply(ToolStrip strip)
    {
        strip.Renderer = new ThemedMenuRenderer();
        strip.BackColor = Theme.Surface;
        strip.ForeColor = Theme.Text;
        strip.Padding = new Padding(4);
    }

    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        using var brush = new SolidBrush(Theme.Surface);
        e.Graphics.FillRectangle(brush, e.AffectedBounds);
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        using var pen = new Pen(Theme.Border);
        e.Graphics.DrawRectangle(pen, 0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
    }

    protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
    {
        // No separate margin color.
    }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        if (!e.Item.Selected || !e.Item.Enabled)
        {
            return;
        }

        var bounds = new Rectangle(2, 0, e.Item.Width - 4, e.Item.Height);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.FillRoundedRectangle(Theme.SurfaceHover, bounds, 4);
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = e.Item.Enabled ? Theme.Text : Theme.TextDisabled;
        base.OnRenderItemText(e);
    }

    protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
    {
        e.ArrowColor = e.Item?.Enabled == false ? Theme.TextDisabled : Theme.TextSecondary;
        base.OnRenderArrow(e);
    }

    protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
    {
        using var font = new Font(Theme.GlyphFontName, e.Item.Font.Size * 0.9f);
        Color color = e.Item.Enabled ? Theme.Accent : Theme.TextDisabled;
        TextRenderer.DrawText(e.Graphics, Glyphs.CheckMark, font, e.ImageRectangle, color,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        int y = e.Item.Height / 2;
        using var pen = new Pen(Theme.Border);
        e.Graphics.DrawLine(pen, 4, y, e.Item.Width - 4, y);
    }

    private sealed class ThemedColorTable : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground => Theme.Surface;
        public override Color MenuBorder => Theme.Border;
        public override Color MenuItemBorder => Color.Transparent;
        public override Color MenuItemSelected => Theme.SurfaceHover;
        public override Color ImageMarginGradientBegin => Theme.Surface;
        public override Color ImageMarginGradientMiddle => Theme.Surface;
        public override Color ImageMarginGradientEnd => Theme.Surface;
        public override Color SeparatorDark => Theme.Border;
        public override Color SeparatorLight => Theme.Border;
    }
}
