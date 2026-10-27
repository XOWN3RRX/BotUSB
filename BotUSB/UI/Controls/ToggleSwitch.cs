using System.Drawing.Drawing2D;

namespace BotUSB.UI.Controls;

/// <summary>A Windows 11 style on/off switch.</summary>
internal sealed class ToggleSwitch : CheckBox
{
    private bool _hover;

    public ToggleSwitch()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        AutoSize = false;
        Size = new Size(40, 20);
        Cursor = Cursors.Hand;
    }

    protected override void OnMouseEnter(EventArgs eventargs)
    {
        _hover = true;
        Invalidate();
        base.OnMouseEnter(eventargs);
    }

    protected override void OnMouseLeave(EventArgs eventargs)
    {
        _hover = false;
        Invalidate();
        base.OnMouseLeave(eventargs);
    }

    protected override void OnPaint(PaintEventArgs pevent)
    {
        Graphics g = pevent.Graphics;
        g.Clear(Parent?.BackColor ?? Theme.Surface);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var track = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
        float radius = track.Height / 2f;
        float knobSize = track.Height * (_hover ? 0.6f : 0.5f);
        float knobMargin = (track.Height - knobSize) / 2f;

        if (Checked)
        {
            g.FillRoundedRectangle(_hover ? Theme.AccentHover : Theme.Accent, track, radius);
        }
        else
        {
            g.FillRoundedRectangle(_hover ? Theme.SurfaceHover : Theme.Surface, track, radius);
            g.DrawRoundedRectangle(Theme.TextSecondary, track, radius);
        }

        float knobX = Checked ? track.Right - knobMargin - knobSize : track.Left + knobMargin;
        using var brush = new SolidBrush(Checked ? Theme.OnAccent : Theme.TextSecondary);
        g.FillEllipse(brush, knobX, track.Top + knobMargin, knobSize, knobSize);

        if (Focused && ShowFocusCues)
        {
            using var pen = new Pen(Theme.Text) { DashStyle = DashStyle.Dot };
            g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        }
    }
}
