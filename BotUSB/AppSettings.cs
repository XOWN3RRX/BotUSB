using System.Text.Json;
using System.Text.Json.Serialization;

namespace BotUSB;

/// <summary>App settings, stored as JSON in %AppData%\BotUSB.</summary>
internal sealed class AppSettings
{
    public const int DefaultPort = 13000;

    private static readonly string s_filePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BotUSB", "settings.json");

    public static AppSettings Current { get; } = Load();

    /// <summary>Raised on the UI thread after the settings were saved.</summary>
    public static event EventHandler? Changed;

    /// <summary>Closing or minimizing the main window hides it to the notification area.</summary>
    public bool KeepInTray { get; set; }

    public bool AlwaysOnTop { get; set; }

    /// <summary>Scroll the log to new messages.</summary>
    public bool AutoScrollLog { get; set; } = true;

    /// <summary>Log every command received from scripts.</summary>
    public bool ShowReceivedData { get; set; }

    public int Port { get; set; } = DefaultPort;

    /// <summary>Accept scripts from other computers, not only from this one.</summary>
    public bool AllowLan { get; set; }

    /// <summary>Distance for the mouse test buttons (the Repeat byte, 1-255).</summary>
    public int MouseStep { get; set; } = 20;

    public void Save()
    {
        Normalize();

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(s_filePath)!);
            File.WriteAllText(s_filePath, JsonSerializer.Serialize(this, AppSettingsJsonContext.Default.AppSettings));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Settings still apply for this session.
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void Normalize()
    {
        if (Port is < 1 or > 65535)
        {
            Port = DefaultPort;
        }

        MouseStep = Math.Clamp(MouseStep, 1, 255);
    }

    private static AppSettings Load()
    {
        try
        {
            if (File.Exists(s_filePath))
            {
                var settings = JsonSerializer.Deserialize(File.ReadAllText(s_filePath), AppSettingsJsonContext.Default.AppSettings);
                if (settings is not null)
                {
                    settings.Normalize();
                    return settings;
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            // Corrupt or unreadable file: start with defaults.
        }

        return new AppSettings();
    }
}

[JsonSerializable(typeof(AppSettings))]
[JsonSourceGenerationOptions(WriteIndented = true)]
internal sealed partial class AppSettingsJsonContext : JsonSerializerContext;
