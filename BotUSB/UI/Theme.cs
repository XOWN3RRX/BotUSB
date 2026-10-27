using System.Drawing.Drawing2D;
using System.Drawing.Text;
using Microsoft.Win32;
using BotUSB.Interop;

namespace BotUSB.UI;

/// <summary>Windows 11 inspired colors and fonts that follow the system light/dark setting and accent color.</summary>
internal static class Theme
{
    public static bool IsDark { get; private set; }

    public static Color Background { get; private set; }
    public static Color Surface { get; private set; }
    public static Color SurfaceHover { get; private set; }
    public static Color SurfacePressed { get; private set; }
    public static Color SurfaceSelected { get; private set; }
    public static Color Well { get; private set; }
    public static Color Border { get; private set; }
    public static Color Text { get; private set; }
    public static Color TextSecondary { get; private set; }
    public static Color TextDisabled { get; private set; }
    public static Color Accent { get; private set; }
    public static Color AccentHover { get; private set; }
    public static Color AccentPressed { get; private set; }
    public static Color OnAccent { get; private set; }

    public static string GlyphFontName { get; private set; } = "Segoe MDL2 Assets";
    public static string StrongFontName { get; private set; } = "Segoe UI";

    public static void Initialize()
    {
        IsDark = ReadAppsUseDarkTheme();

        if (IsDark)
        {
            Background = Color.FromArgb(0x20, 0x20, 0x20);
            Surface = Color.FromArgb(0x2B, 0x2B, 0x2B);
            SurfaceHover = Color.FromArgb(0x32, 0x32, 0x32);
            SurfacePressed = Color.FromArgb(0x27, 0x27, 0x27);
            SurfaceSelected = Color.FromArgb(0x38, 0x38, 0x38);
            Well = Color.FromArgb(0x1C, 0x1C, 0x1C);
            Border = Color.FromArgb(0x3D, 0x3D, 0x3D);
            Text = Color.FromArgb(0xFF, 0xFF, 0xFF);
            TextSecondary = Color.FromArgb(0xC5, 0xC5, 0xC5);
            TextDisabled = Color.FromArgb(0x78, 0x78, 0x78);
            OnAccent = Color.Black;
        }
        else
        {
            Background = Color.FromArgb(0xF3, 0xF3, 0xF3);
            Surface = Color.FromArgb(0xFB, 0xFB, 0xFB);
            SurfaceHover = Color.FromArgb(0xF0, 0xF0, 0xF0);
            SurfacePressed = Color.FromArgb(0xE8, 0xE8, 0xE8);
            SurfaceSelected = Color.FromArgb(0xE6, 0xE6, 0xE6);
            Well = Color.FromArgb(0xEB, 0xEB, 0xEB);
            Border = Color.FromArgb(0xE0, 0xE0, 0xE0);
            Text = Color.FromArgb(0x1A, 0x1A, 0x1A);
            TextSecondary = Color.FromArgb(0x5D, 0x5D, 0x5D);
            TextDisabled = Color.FromArgb(0xA0, 0xA0, 0xA0);
            OnAccent = Color.White;
        }

        Accent = ReadAccentColor(IsDark) ?? (IsDark ? Color.FromArgb(0x4C, 0xC2, 0xFF) : Color.FromArgb(0x00, 0x5F, 0xB8));
        AccentHover = Blend(Accent, Background, 0.1);
        AccentPressed = Blend(Accent, Background, 0.2);

        using var fonts = new InstalledFontCollection();
        var names = fonts.Families.Select(f => f.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        GlyphFontName = names.Contains("Segoe Fluent Icons") ? "Segoe Fluent Icons" : "Segoe MDL2 Assets";
        StrongFontName = names.Contains("Segoe UI Semibold") ? "Segoe UI Semibold" : "Segoe UI";

        if (names.Contains("Segoe UI"))
        {
            Application.SetDefaultFont(new Font("Segoe UI", 9f));
        }
    }

    /// <summary>A semibold font relative to <paramref name="baseFont"/>, so it follows DPI changes.</summary>
    public static Font CreateStrongFont(Font baseFont, float scale = 1f) =>
        new(StrongFontName, baseFont.Size * scale, StrongFontName == "Segoe UI" ? FontStyle.Bold : FontStyle.Regular);

    public static Font CreateGlyphFont(Font baseFont, float scale = 1.1f) =>
        new(GlyphFontName, baseFont.Size * scale);

    public static Font CreateCaptionFont(Font baseFont) =>
        new(baseFont.FontFamily, baseFont.Size * 0.92f);

    /// <summary>Applies window colors and a matching title bar.</summary>
    public static void ApplyTo(Form form)
    {
        form.BackColor = Background;
        form.ForeColor = Text;

        if (form.IsHandleCreated)
        {
            NativeMethods.SetDarkTitleBar(form.Handle, IsDark);
        }

        form.HandleCreated += static (s, _) => NativeMethods.SetDarkTitleBar(((Form)s!).Handle, IsDark);
    }

    public static Color Blend(Color from, Color to, double amount) => Color.FromArgb(
        (int)Math.Round(from.R + (to.R - from.R) * amount),
        (int)Math.Round(from.G + (to.G - from.G) * amount),
        (int)Math.Round(from.B + (to.B - from.B) * amount));

    public static GraphicsPath RoundedRectangle(RectangleF bounds, float radius)
    {
        var path = new GraphicsPath();
        float d = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));

        if (d <= 0)
        {
            path.AddRectangle(bounds);
            return path;
        }

        path.AddArc(bounds.Left, bounds.Top, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Top, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    public static void FillRoundedRectangle(this Graphics g, Color color, RectangleF bounds, float radius)
    {
        using var path = RoundedRectangle(bounds, radius);
        using var brush = new SolidBrush(color);
        g.FillPath(brush, path);
    }

    public static void DrawRoundedRectangle(this Graphics g, Color color, RectangleF bounds, float radius, float width = 1f)
    {
        bounds.Inflate(-width / 2f, -width / 2f);
        using var path = RoundedRectangle(bounds, radius);
        using var pen = new Pen(color, width);
        g.DrawPath(pen, path);
    }

    private static bool ReadAppsUseDarkTheme()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        return key?.GetValue("AppsUseLightTheme") is int light && light == 0;
    }

    private static Color? ReadAccentColor(bool dark)
    {
        // AccentPalette holds 8 RGBA shades from lightest to darkest. Windows uses
        // "Light 2" on dark backgrounds and "Dark 1" on light backgrounds.
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Accent");
        if (key?.GetValue("AccentPalette") is byte[] palette && palette.Length >= 32)
        {
            int i = (dark ? 1 : 4) * 4;
            return Color.FromArgb(palette[i], palette[i + 1], palette[i + 2]);
        }

        return null;
    }
}

/// <summary>Code points shared by Segoe Fluent Icons and Segoe MDL2 Assets.</summary>
internal static class Glyphs
{
    public const string Settings = "\uE713";
    public const string Search = "\uE721";
    public const string CheckMark = "\uE73E";
    public const string Keyboard = "\uE765";
    public const string Delete = "\uE74D";
    public const string Send = "\uE724";
    public const string Usb = "\uE88E";
    public const string Network = "\uE968";
    public const string Stopwatch = "\uE916";
    public const string Mouse = "\uE962";
    public const string Health = "\uE95E";
    public const string Cancel = "\uE711";
    public const string ChevronUp = "\uE70E";
    public const string ChevronDown = "\uE70D";
    public const string ChevronLeft = "\uE76B";
    public const string ChevronRight = "\uE76C";
}
