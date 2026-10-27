using BotUSB.Device;

namespace BotUSB.Server;

/// <summary>One JSON object sent by a script, e.g. <c>{ 'Keys': [148] }</c>.</summary>
internal sealed record ClientCommand(int[]? Keys, string? Message, bool Pressed, int Repeat)
{
    public override string ToString()
    {
        if (Message is not null)
        {
            return $"Message: \"{Message}\"";
        }

        string keys = Keys is null ? "none" : string.Join(" + ", Keys.Select(k => KeyCodes.IsValid(k) ? $"{(KeyCode)k} ({k})" : k.ToString()));
        return $"Keys: {keys}{(Pressed ? ", pressed" : "")}{(Repeat != 0 ? $", repeat {Repeat}" : "")}";
    }

    /// <summary>
    /// Turns the request into a device command with the same rules as the original app:
    /// Message, then Repeat (one key), then press-and-hold or tap (one key), then 2 or 3 key combos.
    /// </summary>
    public BotCommand? ToBotCommand(out string? error)
    {
        error = null;

        if (Message is not null)
        {
            return BotCommand.Type(Message);
        }

        if (Keys is not { Length: > 0 })
        {
            error = "No Keys or Message";
            return null;
        }

        foreach (int key in Keys)
        {
            if (!KeyCodes.IsValid(key))
            {
                error = $"Unknown key code {key} (valid: 0-{KeyCodes.Max})";
                return null;
            }
        }

        KeyCode[] keys = Array.ConvertAll(Keys, k => (KeyCode)k);

        if (Repeat > 0 && keys.Length == 1)
        {
            // The original app capped the argument byte at 254.
            return BotCommand.WithArgument(keys[0], (byte)(Repeat > byte.MaxValue ? byte.MaxValue - 1 : Repeat));
        }

        return keys.Length switch
        {
            1 => Pressed ? BotCommand.Hold(keys[0]) : BotCommand.Tap(keys[0]),
            2 or 3 => BotCommand.Combo(keys),
            _ => Fail(out error, $"{keys.Length} keys at once is not supported (1 to 3)"),
        };

        static BotCommand? Fail(out string? error, string message)
        {
            error = message;
            return null;
        }
    }
}
