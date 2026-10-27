namespace BotUSB.Device;

internal enum BotAction
{
    /// <summary>Writes the key, then NULL for hold codes (tap codes release themselves).</summary>
    Tap,

    /// <summary>Writes the key only (hold codes stay down).</summary>
    Hold,

    /// <summary>Writes the key with an argument byte (the distance for mouse moves).</summary>
    WithArgument,

    /// <summary>Writes each key in order, then NULL.</summary>
    Combo,

    /// <summary>Types text using <see cref="TextKeyMap"/>.</summary>
    Type,

    ReleaseAll,

    /// <summary>Checks that the stick accepts a command and reports the latency.</summary>
    DriverTest,

    /// <summary>Sends nothing; wakes the worker to look for the stick after a device change.</summary>
    Rescan,
}

internal sealed record BotCommand(BotAction Action, KeyCode[] Keys, byte Argument = 0, string? Text = null)
{
    public static BotCommand Tap(KeyCode key) => new(BotAction.Tap, [key]);

    public static BotCommand Hold(KeyCode key) => new(BotAction.Hold, [key]);

    public static BotCommand WithArgument(KeyCode key, byte argument) => new(BotAction.WithArgument, [key], argument);

    public static BotCommand Combo(params KeyCode[] keys) => new(BotAction.Combo, keys);

    public static BotCommand Type(string text) => new(BotAction.Type, [], Text: text);

    public static readonly BotCommand ReleaseAll = new(BotAction.ReleaseAll, []);

    public static readonly BotCommand DriverTest = new(BotAction.DriverTest, []);

    public static readonly BotCommand Rescan = new(BotAction.Rescan, []);
}
