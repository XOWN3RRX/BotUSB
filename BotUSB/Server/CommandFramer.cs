using System.Text;

namespace BotUSB.Server;

/// <summary>
/// Cuts a TCP character stream into JSON objects. Objects may arrive several per packet (<c>{..}{..}</c>)
/// or split across packets. Braces inside strings are ignored, using the same lenient quote rule as
/// <see cref="LenientJson"/>.
/// </summary>
internal sealed class CommandFramer
{
    public const int MaxObjectLength = 64 * 1024;

    private readonly StringBuilder _current = new();
    private int _depth;
    private bool _inString;
    private bool _escape;
    private bool _maybeEnd;
    private char _quote;

    /// <summary>Feeds characters; complete objects are added to <paramref name="objects"/>.</summary>
    /// <returns>Characters outside any object that were ignored, or that overflowed the size limit.</returns>
    public int Push(ReadOnlySpan<char> chars, List<string> objects)
    {
        int ignored = 0;

        foreach (char c in chars)
        {
            if (_depth == 0)
            {
                if (c == '{')
                {
                    _current.Append(c);
                    _depth = 1;
                }
                else if (!char.IsWhiteSpace(c) && !char.IsControl(c) && c is not ('[' or ']' or ','))
                {
                    // Garbage between objects. A JSON array of objects is fine, and so are the NUL bytes
                    // scripts send when they copy JSON into a fixed buffer (char sends[256]).
                    ignored++;
                }

                continue;
            }

            if (_current.Length >= MaxObjectLength)
            {
                ignored += _current.Length;
                Reset();
                continue;
            }

            _current.Append(c);

            if (_inString)
            {
                if (_maybeEnd)
                {
                    if (char.IsWhiteSpace(c))
                    {
                        continue;
                    }

                    _maybeEnd = false;
                    if (!LenientJson.IsStringTerminator(c))
                    {
                        // The quote was part of the text.
                        if (c == _quote)
                        {
                            _maybeEnd = true;
                        }
                        else if (c == '\\')
                        {
                            _escape = true;
                        }

                        continue;
                    }

                    _inString = false; // The terminator is handled below.
                }
                else
                {
                    if (_escape)
                    {
                        _escape = false;
                    }
                    else if (c == '\\')
                    {
                        _escape = true;
                    }
                    else if (c == _quote)
                    {
                        _maybeEnd = true;
                    }

                    continue;
                }
            }

            switch (c)
            {
                case '"' or '\'':
                    _inString = true;
                    _quote = c;
                    break;
                case '{':
                    _depth++;
                    break;
                case '}':
                    if (--_depth == 0)
                    {
                        objects.Add(_current.ToString());
                        Reset();
                    }

                    break;
            }
        }

        return ignored;
    }

    private void Reset()
    {
        _current.Clear();
        _depth = 0;
        _inString = _escape = _maybeEnd = false;
    }
}
