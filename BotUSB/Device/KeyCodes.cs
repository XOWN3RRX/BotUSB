using System.Collections.Frozen;

namespace BotUSB.Device;

internal enum KeyKind
{
    Release,
    Tap,
    Hold,
    MouseMove,
}

/// <summary>Lookup helpers and human-readable names for <see cref="KeyCode"/>.</summary>
internal static class KeyCodes
{
    public const int Max = (int)KeyCode._MouseYMinus;

    public static readonly KeyCode[] All = Enum.GetValues<KeyCode>();

    private static readonly FrozenDictionary<string, KeyCode> s_byName =
        All.ToFrozenDictionary(k => k.ToString(), StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenDictionary<string, string> s_displayNames = new Dictionary<string, string>
    {
        ["MouseLeft"] = "Left mouse button",
        ["MouseRight"] = "Right mouse button",
        ["MouseMiddle"] = "Middle mouse button",
        ["MouseLeftRight"] = "Left + right mouse buttons",
        ["MouseXPlus"] = "Mouse right",
        ["MouseXMinus"] = "Mouse left",
        ["MouseYPlus"] = "Mouse down",
        ["MouseYMinus"] = "Mouse up",
        ["Enter1"] = "Enter",
        ["Enter2"] = "Numpad Enter",
        ["NumpadEnter"] = "Numpad Enter",
        ["Esc"] = "Escape",
        ["BackSpace"] = "Backspace",
        ["Minus1"] = "-",
        ["Minus"] = "-",
        ["Equal"] = "=",
        ["OpenBracket"] = "[",
        ["CloseBracket"] = "]",
        ["BackSlash1"] = "\\",
        ["BackSlash2"] = "\\ (non-US)",
        ["Semicolon"] = ";",
        ["SingleQuote"] = "'",
        ["SingleQuoteMark"] = "`",
        ["Comma"] = ",",
        ["Point"] = ".",
        ["Slash"] = "/",
        ["CAPS"] = "Caps Lock",
        ["PrtSrc"] = "Print Screen",
        ["ScrollLock"] = "Scroll Lock",
        ["PauseBreak"] = "Pause/Break",
        ["PageUp"] = "Page Up",
        ["PageDown"] = "Page Down",
        ["RightArrow"] = "Right arrow",
        ["LeftArrow"] = "Left arrow",
        ["DownArrow"] = "Down arrow",
        ["UpArrow"] = "Up arrow",
        ["NumLock"] = "Num Lock",
        ["NumpadDivide"] = "Numpad /",
        ["NumpadMultiply"] = "Numpad *",
        ["NumpadMinus2"] = "Numpad -",
        ["NumpadAdd"] = "Numpad +",
        ["NumpadPoint"] = "Numpad .",
        ["CtrlL"] = "Left Ctrl",
        ["ShiftL"] = "Left Shift",
        ["AltL"] = "Left Alt",
        ["WinL"] = "Left Windows",
        ["CtrlR"] = "Right Ctrl",
        ["ShiftR"] = "Right Shift",
        ["AltR"] = "Right Alt",
        ["WinR"] = "Right Windows",
    }.ToFrozenDictionary();

    public static bool IsValid(int code) => code is >= 0 and <= Max;

    public static KeyKind KindOf(KeyCode code) => code switch
    {
        KeyCode.NULL => KeyKind.Release,
        <= KeyCode.NumpadPoint => KeyKind.Tap,
        <= KeyCode._NumpadPoint => KeyKind.Hold,
        _ => KeyKind.MouseMove,
    };

    /// <summary>
    /// True when one write is a complete tap: tap codes are released by the stick itself, and NULL is the
    /// release. Hold codes need a NULL afterwards.
    /// </summary>
    public static bool ReleasesItself(KeyCode code) => KindOf(code) is KeyKind.Tap or KeyKind.Release;

    /// <summary>Parses a code number ("148") or name ("_Esc", case-insensitive).</summary>
    public static bool TryParse(ReadOnlySpan<char> text, out KeyCode code)
    {
        text = text.Trim();
        if (int.TryParse(text, out int number))
        {
            code = (KeyCode)number;
            return IsValid(number);
        }

        return s_byName.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(text, out code);
    }

    /// <summary>A friendly key name, e.g. "Left Ctrl" for <see cref="KeyCode._CtrlL"/>.</summary>
    public static string DisplayName(KeyCode code)
    {
        if (code == KeyCode.NULL)
        {
            return "Release all";
        }

        string name = code.ToString().TrimStart('_');
        if (s_displayNames.TryGetValue(name, out string? display))
        {
            return display;
        }

        if (name.StartsWith("Number", StringComparison.Ordinal))
        {
            return name["Number".Length..];
        }

        if (name.StartsWith("Numpad", StringComparison.Ordinal))
        {
            return "Numpad " + name["Numpad".Length..];
        }

        return name;
    }

    public static string KindName(KeyKind kind) => kind switch
    {
        KeyKind.Release => "Releases every held key and button",
        KeyKind.Tap => "Tap: pressed and released",
        KeyKind.Hold => "Hold: stays down until 0 (NULL)",
        _ => "Moves the mouse once by Repeat (1-255)",
    };
}
