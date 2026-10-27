using System.Globalization;
using System.Text;
using BotUSB.Device;

namespace BotUSB.Server;

/// <summary>
/// Parses the small JSON objects scripts send. It accepts what Json.NET accepted in the original app:
/// single or double quotes, unquoted property names, any letter case, and numbers or booleans written as
/// strings. Key codes may also be names ("_Esc").
/// </summary>
/// <remarks>
/// Scripts build JSON by string concatenation, so a message like <c>'it's'</c> arrives unescaped. A quote
/// therefore only closes a string when the next non-space character is <c>, } ] :</c> or the end, and
/// <see cref="CommandFramer"/> applies the same rule.
/// </remarks>
internal static class LenientJson
{
    public static bool IsStringTerminator(char c) => c is ',' or '}' or ']' or ':';

    public static bool TryParseCommand(ReadOnlySpan<char> json, out ClientCommand? command, out string? error)
    {
        var parser = new Parser(json);
        try
        {
            command = parser.ParseCommand();
            error = null;
            return true;
        }
        catch (FormatException ex)
        {
            command = null;
            error = ex.Message;
            return false;
        }
    }

    private ref struct Parser(ReadOnlySpan<char> text)
    {
        private readonly ReadOnlySpan<char> _text = text;
        private int _pos;

        public ClientCommand ParseCommand()
        {
            int[]? keys = null;
            string? message = null;
            bool pressed = false;
            int repeat = 0;

            SkipWhitespace();
            Expect('{');

            while (true)
            {
                SkipWhitespace();
                if (TryConsume('}'))
                {
                    break;
                }

                string name = ParsePropertyName();
                SkipWhitespace();
                Expect(':');
                SkipWhitespace();

                switch (name.ToLowerInvariant())
                {
                    case "keys":
                        keys = ParseKeys();
                        break;
                    case "message":
                        message = ParseValue() switch
                        {
                            null => null,
                            string s => s,
                            object other => Convert.ToString(other, CultureInfo.InvariantCulture),
                        };
                        break;
                    case "pressed":
                        pressed = ToBool(ParseValue(), "Pressed");
                        break;
                    case "repeat":
                        repeat = ToInt(ParseValue(), "Repeat");
                        break;
                    default:
                        _ = ParseValue(); // Unknown properties are ignored, as before.
                        break;
                }

                SkipWhitespace();
                if (TryConsume(','))
                {
                    continue;
                }

                Expect('}');
                break;
            }

            SkipWhitespace();
            if (_pos < _text.Length)
            {
                throw Error("Unexpected text after the object");
            }

            return new ClientCommand(keys, message, pressed, repeat);
        }

        private int[]? ParseKeys()
        {
            object? value = ParseValue();
            List<object?> items = value as List<object?> ?? [value];
            if (value is null)
            {
                return null;
            }

            var keys = new int[items.Count];
            for (int i = 0; i < items.Count; i++)
            {
                keys[i] = items[i] switch
                {
                    string s when KeyCodes.TryParse(s, out KeyCode code) => (int)code,
                    string s when int.TryParse(s, out int number) => number,
                    string s => throw Error($"Unknown key \"{s}\""),
                    object other => ToInt(other, "Keys"),
                    null => throw Error("A key is null"),
                };
            }

            return keys;
        }

        private object? ParseValue()
        {
            SkipWhitespace();
            if (_pos >= _text.Length)
            {
                throw Error("Unexpected end of data");
            }

            char c = _text[_pos];
            switch (c)
            {
                case '"' or '\'':
                    return ParseString();
                case '[':
                    return ParseArray();
                case '{':
                    SkipObject();
                    return null;
                case '-' or '+' or (>= '0' and <= '9'):
                    return ParseNumber();
            }

            string word = ParseWord();
            return word switch
            {
                "true" or "True" => true,
                "false" or "False" => false,
                "null" or "undefined" => null,
                _ => word,
            };
        }

        private List<object?> ParseArray()
        {
            Expect('[');
            var items = new List<object?>();
            SkipWhitespace();
            if (TryConsume(']'))
            {
                return items;
            }

            while (true)
            {
                items.Add(ParseValue());
                SkipWhitespace();
                if (TryConsume(','))
                {
                    SkipWhitespace();
                    if (TryConsume(']'))
                    {
                        return items; // Trailing comma.
                    }

                    continue;
                }

                Expect(']');
                return items;
            }
        }

        private void SkipObject()
        {
            Expect('{');
            int depth = 1;
            while (_pos < _text.Length && depth > 0)
            {
                char c = _text[_pos];
                if (c is '"' or '\'')
                {
                    ParseString();
                    continue;
                }

                depth += c == '{' ? 1 : c == '}' ? -1 : 0;
                _pos++;
            }
        }

        private string ParsePropertyName()
        {
            char c = Peek();
            string name = c is '"' or '\'' ? ParseString() : ParseWord();
            if (name.Length == 0)
            {
                throw Error("Expected a property name");
            }

            return name;
        }

        private string ParseString()
        {
            char quote = _text[_pos++];
            var sb = new StringBuilder();

            while (_pos < _text.Length)
            {
                char c = _text[_pos++];
                if (c == quote)
                {
                    int next = _pos;
                    while (next < _text.Length && char.IsWhiteSpace(_text[next]))
                    {
                        next++;
                    }

                    if (next >= _text.Length || LenientJson.IsStringTerminator(_text[next]))
                    {
                        return sb.ToString();
                    }

                    sb.Append(c); // An unescaped quote inside the text.
                    continue;
                }

                if (c != '\\')
                {
                    sb.Append(c);
                    continue;
                }

                if (_pos >= _text.Length)
                {
                    break;
                }

                char escaped = _text[_pos++];
                switch (escaped)
                {
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'u' when _pos + 4 <= _text.Length
                        && ushort.TryParse(_text.Slice(_pos, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ushort code):
                        sb.Append((char)code);
                        _pos += 4;
                        break;
                    default: sb.Append(escaped); break;
                }
            }

            throw Error("Unterminated string");
        }

        private object ParseNumber()
        {
            int start = _pos;
            while (_pos < _text.Length && (char.IsAsciiDigit(_text[_pos]) || _text[_pos] is '-' or '+' or '.' or 'e' or 'E'))
            {
                _pos++;
            }

            ReadOnlySpan<char> number = _text[start.._pos];
            if (long.TryParse(number, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long integer))
            {
                return integer;
            }

            if (double.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out double real))
            {
                return real;
            }

            throw Error($"Invalid number \"{number}\"");
        }

        private string ParseWord()
        {
            int start = _pos;
            while (_pos < _text.Length && (char.IsLetterOrDigit(_text[_pos]) || _text[_pos] is '_' or '$'))
            {
                _pos++;
            }

            if (_pos == start)
            {
                throw Error($"Unexpected character '{Peek()}'");
            }

            return _text[start.._pos].ToString();
        }

        private static bool ToBool(object? value, string name) => value switch
        {
            bool b => b,
            long n => n != 0,
            double d => d != 0,
            null => false,
            string s when bool.TryParse(s.Trim(), out bool b) => b,
            string s when long.TryParse(s.Trim(), out long n) => n != 0,
            _ => throw Error($"{name} must be true or false"),
        };

        private static int ToInt(object? value, string name) => value switch
        {
            long n and >= int.MinValue and <= int.MaxValue => (int)n,
            double d when d == Math.Truncate(d) && d is >= int.MinValue and <= int.MaxValue => (int)d,
            bool b => b ? 1 : 0,
            null => 0,
            string s when int.TryParse(s.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) => n,
            _ => throw Error($"{name} must be a whole number"),
        };

        private void SkipWhitespace()
        {
            while (_pos < _text.Length && char.IsWhiteSpace(_text[_pos]))
            {
                _pos++;
            }
        }

        private readonly char Peek() => _pos < _text.Length ? _text[_pos] : '\0';

        private bool TryConsume(char c)
        {
            if (_pos < _text.Length && _text[_pos] == c)
            {
                _pos++;
                return true;
            }

            return false;
        }

        private void Expect(char c)
        {
            if (!TryConsume(c))
            {
                throw Error(_pos < _text.Length ? $"Expected '{c}' but found '{_text[_pos]}'" : $"Expected '{c}'");
            }
        }

        private static FormatException Error(string message) => new(message);
    }
}
