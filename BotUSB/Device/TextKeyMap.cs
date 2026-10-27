using System.Collections.Frozen;

namespace BotUSB.Device;

/// <summary>Maps typed characters to the hold codes that produce them on a US layout.</summary>
internal static class TextKeyMap
{
    private static readonly FrozenDictionary<char, KeyCode[]> s_map = Build();

    public static bool TryGetKeys(char c, out KeyCode[] keys) => s_map.TryGetValue(c, out keys!);

    private static FrozenDictionary<char, KeyCode[]> Build()
    {
        var map = new Dictionary<char, KeyCode[]>
        {
            [' '] = [KeyCode._Space],
            ['-'] = [KeyCode._Minus],
            ['='] = [KeyCode._Equal],
            ['['] = [KeyCode._OpenBracket],
            [']'] = [KeyCode._CloseBracket],
            ['\\'] = [KeyCode._BackSlash1],
            [';'] = [KeyCode._Semicolon],
            ['\''] = [KeyCode._SingleQuote],
            ['`'] = [KeyCode._SingleQuoteMark],
            [','] = [KeyCode._Comma],
            ['/'] = [KeyCode._Slash],
            ['.'] = [KeyCode._Point],
            [':'] = [KeyCode._ShiftL, KeyCode._Semicolon],
            ['"'] = [KeyCode._ShiftL, KeyCode._SingleQuote],
            ['{'] = [KeyCode._ShiftL, KeyCode._OpenBracket],
            ['}'] = [KeyCode._ShiftL, KeyCode._CloseBracket],
            ['?'] = [KeyCode._ShiftL, KeyCode._Slash],
            ['!'] = [KeyCode._ShiftL, KeyCode._1],
            ['@'] = [KeyCode._ShiftL, KeyCode._2],
            ['#'] = [KeyCode._ShiftL, KeyCode._3],
            ['$'] = [KeyCode._ShiftL, KeyCode._4],
            ['%'] = [KeyCode._ShiftL, KeyCode._5],
            ['^'] = [KeyCode._ShiftL, KeyCode._6],
            ['&'] = [KeyCode._ShiftL, KeyCode._7],
            ['*'] = [KeyCode._ShiftL, KeyCode._8],
            ['('] = [KeyCode._ShiftL, KeyCode._9],
            [')'] = [KeyCode._ShiftL, KeyCode._0],
        };

        for (int i = 0; i < 26; i++)
        {
            var letter = (KeyCode)((int)KeyCode._A + i);
            map[(char)('a' + i)] = [letter];
            map[(char)('A' + i)] = [KeyCode._ShiftL, letter];
        }

        map['0'] = [KeyCode._0];
        for (int i = 1; i <= 9; i++)
        {
            map[(char)('0' + i)] = [(KeyCode)((int)KeyCode._1 + i - 1)];
        }

        return map.ToFrozenDictionary();
    }
}
