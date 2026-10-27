using BotUSB.Device;
using BotUSB.Interop;
using BotUSB.Server;
using BotUSB.UI;
using BotUSB.UI.Controls;

namespace BotUSB;

internal sealed class MainForm : Form
{
    private static readonly (string Name, KeyCode[] Keys)[] s_combos =
    [
        ("Alt+Tab", [KeyCode._AltL, KeyCode._Tab]),
        ("Win+D", [KeyCode._WinL, KeyCode._D]),
        ("Win+Tab", [KeyCode._WinL, KeyCode._Tab]),
        ("Win+R", [KeyCode._WinL, KeyCode._R]),
        ("Ctrl+Esc", [KeyCode._CtrlL, KeyCode._Esc]),
        ("Ctrl+Alt+Del", [KeyCode._CtrlL, KeyCode._AltL, KeyCode._Delete]),
    ];

    private static readonly (string Name, KeyCode Key)[] s_singleKeys =
    [
        ("Caps Lock", KeyCode._CAPS),
        ("Windows", KeyCode._WinL),
        ("Print Screen", KeyCode._PrtSrc),
        ("Escape", KeyCode._Esc),
    ];

    private readonly BotEngine _engine;
    private readonly CommandServer _server;
    private readonly List<LogEntry> _pendingLog = [];

    private readonly Label _logTitle;
    private readonly Label _autoScrollLabel;
    private readonly ToggleSwitch _autoScrollToggle;
    private readonly Label _receivedLabel;
    private readonly ToggleSwitch _receivedToggle;
    private readonly ModernButton _clearButton;
    private readonly Card _logCard;
    private readonly LogView _log;

    private readonly Label _title;
    private readonly ModernButton _keyCodesButton;
    private readonly ModernButton _settingsButton;
    private readonly StatusCard _status;

    private readonly Label _keysHeading;
    private readonly ModernButton _comboButton;
    private readonly ModernButton _comboSend;
    private readonly ContextMenuStrip _comboMenu;
    private readonly ModernButton _keyButton;
    private readonly TextField _codeField;
    private readonly ModernButton _keySend;
    private readonly ContextMenuStrip _keyMenu;

    private readonly Label _mouseHeading;
    private readonly ModernButton[] _clickButtons;
    private readonly ModernButton[] _moveButtons;
    private readonly TextField _stepField;

    private readonly ModernButton _releaseButton;
    private readonly ModernButton _testButton;

    private readonly ToolTip _toolTip;
    private readonly System.Windows.Forms.Timer _timer;
    private readonly NotifyIcon _trayIcon;
    private readonly ContextMenuStrip _trayMenu;

    private KeyCode[] _combo = s_combos[0].Keys;
    private KeyCode? _presetKey = s_singleKeys[0].Key;
    private int _serverPort;
    private bool _serverLan;
    private long _ignoreClicksUntil;
    private bool _exiting;
    private bool _trayHintShown;
    private SettingsForm? _settingsForm;
    private KeyCodesForm? _keyCodesForm;

    public MainForm()
    {
        AppLog.Info($"BotUSB {Application.ProductVersion.Split('+')[0]} started");
        _engine = new BotEngine();
        _server = new CommandServer(_engine);
        AppSettings settings = AppSettings.Current;

        SuspendLayout();

        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(1000, 660);
        MinimumSize = new Size(840, 640);
        StartPosition = FormStartPosition.CenterScreen;
        Text = "BotUSB";
        Icon = Program.AppIcon;
        TopMost = settings.AlwaysOnTop;
        Theme.ApplyTo(this);

        _toolTip = new ToolTip();

        // Log (left)
        _logTitle = Heading("Log");
        _autoScrollLabel = Caption("Auto-scroll");
        _autoScrollToggle = new ToggleSwitch { Checked = settings.AutoScrollLog, AccessibleName = "Auto-scroll" };
        _autoScrollToggle.CheckedChanged += (_, _) => SaveSetting(s => s.AutoScrollLog = _autoScrollToggle.Checked);
        _receivedLabel = Caption("Received data");
        _receivedToggle = new ToggleSwitch { Checked = settings.ShowReceivedData, AccessibleName = "Show received data" };
        _receivedToggle.CheckedChanged += (_, _) => SaveSetting(s => s.ShowReceivedData = _receivedToggle.Checked);
        _toolTip.SetToolTip(_receivedToggle, "Log every command received from scripts");
        _clearButton = new ModernButton { Kind = ButtonKind.Subtle, Glyph = Glyphs.Delete, TabStop = false, AccessibleName = "Clear log" };
        _clearButton.Click += (_, _) => _log!.Items.Clear();
        _toolTip.SetToolTip(_clearButton, "Clear the log");

        _log = new LogView();
        _logCard = new Card();
        _logCard.Controls.Add(_log);
        _logCard.Layout += (_, _) =>
        {
            int inset = _logCard.LogicalToDeviceUnits(6);
            _log.Bounds = new Rectangle(inset, inset, Math.Max(0, _logCard.Width - 2 * inset), Math.Max(0, _logCard.Height - 2 * inset));
        };

        // Sidebar (right)
        _title = Heading("BotUSB");
        _keyCodesButton = new ModernButton { Kind = ButtonKind.Subtle, Glyph = Glyphs.Keyboard, TabStop = false, AccessibleName = "Key codes" };
        _keyCodesButton.Click += (_, _) => ShowKeyCodes();
        _toolTip.SetToolTip(_keyCodesButton, "Key codes");
        _settingsButton = new ModernButton { Kind = ButtonKind.Subtle, Glyph = Glyphs.Settings, TabStop = false, AccessibleName = "Settings" };
        _settingsButton.Click += (_, _) => ShowSettings();
        _toolTip.SetToolTip(_settingsButton, "Settings");

        _status = new StatusCard((Glyphs.Usb, "Stick"), (Glyphs.Network, "Server"), (Glyphs.Stopwatch, "Speed"));

        _keysHeading = Subheading("Test keys");

        _comboMenu = new ContextMenuStrip();
        _comboButton = new ModernButton { ShowChevron = true, Text = s_combos[0].Name };
        _comboButton.Click += (_, _) => _comboMenu.Show(_comboButton, new Point(0, _comboButton.Height));
        foreach ((string name, KeyCode[] keys) in s_combos)
        {
            _comboMenu.Items.Add(new ToolStripMenuItem(name, null, (_, _) =>
            {
                _combo = keys;
                _comboButton.Text = name;
                _comboButton.Invalidate();
            }));
        }

        _comboSend = new ModernButton { Glyph = Glyphs.Send, AccessibleName = "Send combination" };
        _comboSend.Click += (_, _) => _engine.Enqueue(BotCommand.Combo(_combo));
        _toolTip.SetToolTip(_comboSend, "Send the key combination");

        _keyMenu = new ContextMenuStrip();
        _keyButton = new ModernButton { ShowChevron = true, Text = s_singleKeys[0].Name };
        _keyButton.Click += (_, _) => _keyMenu.Show(_keyButton, new Point(0, _keyButton.Height));
        foreach ((string name, KeyCode key) in s_singleKeys)
        {
            _keyMenu.Items.Add(new ToolStripMenuItem(name, null, (_, _) => SelectPresetKey(name, key)));
        }

        _keyMenu.Items.Add(new ToolStripSeparator());
        _keyMenu.Items.Add(new ToolStripMenuItem("Custom code...", null, (_, _) => SelectCustomKey(null)));

        _codeField = new TextField { Glyph = Glyphs.Keyboard, PlaceholderText = "Code or name" };
        _codeField.TextBox.AccessibleName = "Key code";
        _codeField.TextBox.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                SendSingleKey();
                e.SuppressKeyPress = true;
            }
        };
        _toolTip.SetToolTip(_codeField.TextBox, $"A key code (0-{KeyCodes.Max}) or name such as _Esc");

        _keySend = new ModernButton { Glyph = Glyphs.Send, AccessibleName = "Send key" };
        _keySend.Click += (_, _) => SendSingleKey();
        _toolTip.SetToolTip(_keySend, "Tap the key");

        _mouseHeading = Subheading("Test mouse");
        _clickButtons =
        [
            MouseClickButton("Left", KeyCode.MouseLeft),
            MouseClickButton("Middle", KeyCode.MouseMiddle),
            MouseClickButton("Right", KeyCode.MouseRight),
        ];

        _moveButtons =
        [
            MouseMoveButton(Glyphs.ChevronLeft, KeyCode._MouseXMinus, "Move left"),
            MouseMoveButton(Glyphs.ChevronUp, KeyCode._MouseYMinus, "Move up"),
            MouseMoveButton(Glyphs.ChevronDown, KeyCode._MouseYPlus, "Move down"),
            MouseMoveButton(Glyphs.ChevronRight, KeyCode._MouseXPlus, "Move right"),
        ];

        _stepField = new TextField { Text = settings.MouseStep.ToString(), PlaceholderText = "Step" };
        _stepField.TextBox.MaxLength = 3;
        _stepField.TextBox.AccessibleName = "Mouse step";
        _stepField.TextBox.TextAlign = HorizontalAlignment.Center;
        _stepField.TextBox.Leave += (_, _) => _stepField.Text = ReadMouseStep().ToString();
        _toolTip.SetToolTip(_stepField.TextBox, "Distance per move (1-255), sent as Repeat");

        _releaseButton = new ModernButton { Glyph = Glyphs.Cancel, Text = "Release all" };
        _releaseButton.Click += (_, _) => _engine.Enqueue(BotCommand.ReleaseAll);
        _toolTip.SetToolTip(_releaseButton, "Release every held key and mouse button (code 0)");

        _testButton = new ModernButton { Kind = ButtonKind.Accent, Glyph = Glyphs.Health, Text = "Test driver" };
        _testButton.Click += (_, _) => _engine.Enqueue(BotCommand.DriverTest);
        _toolTip.SetToolTip(_testButton, "Check that the stick accepts commands");

        Controls.AddRange([_logTitle, _autoScrollLabel, _autoScrollToggle, _receivedLabel, _receivedToggle, _clearButton, _logCard,
            _title, _keyCodesButton, _settingsButton, _status, _keysHeading, _comboButton, _comboSend, _keyButton, _codeField, _keySend,
            _mouseHeading, .. _clickButtons, .. _moveButtons, _stepField, _releaseButton, _testButton]);

        ThemedMenuRenderer.Apply(_comboMenu);
        ThemedMenuRenderer.Apply(_keyMenu);
        SelectPresetKey(s_singleKeys[0].Name, s_singleKeys[0].Key);

        _timer = new System.Windows.Forms.Timer { Interval = 50 };
        _timer.Tick += Timer_Tick;

        _trayMenu = new ContextMenuStrip();
        ThemedMenuRenderer.Apply(_trayMenu);
        var openItem = new ToolStripMenuItem("Open BotUSB", null, (_, _) => RestoreFromTray());
        openItem.Font = new Font(openItem.Font, FontStyle.Bold);
        _trayMenu.Items.AddRange(
        [
            openItem,
            new ToolStripMenuItem("Settings", null, (_, _) => ShowSettings()),
            new ToolStripMenuItem("Release all keys", null, (_, _) => _engine.Enqueue(BotCommand.ReleaseAll)),
            new ToolStripSeparator(),
            new ToolStripMenuItem("Exit", null, (_, _) => ExitApplication()),
        ]);

        _trayIcon = new NotifyIcon
        {
            Icon = new Icon(Program.AppIcon, SystemInformation.SmallIconSize),
            Text = "BotUSB",
            ContextMenuStrip = _trayMenu,
            Visible = settings.KeepInTray,
        };
        _trayIcon.MouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                RestoreFromTray();
            }
        };
        _trayIcon.BalloonTipClicked += (_, _) => RestoreFromTray();
        AppSettings.Changed += Settings_Changed;

        ResumeLayout(false);
    }

    private int S(int logical) => LogicalToDeviceUnits(logical);

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        _logTitle.Font = Theme.CreateStrongFont(Font, 1.45f);
        _title.Font = Theme.CreateStrongFont(Font, 1.45f);
        _keysHeading.Font = Theme.CreateStrongFont(Font, 1.05f);
        _mouseHeading.Font = Theme.CreateStrongFont(Font, 1.05f);
        _autoScrollLabel.Font = Theme.CreateCaptionFont(Font);
        _receivedLabel.Font = Theme.CreateCaptionFont(Font);

        StartServer();
        _timer.Start();
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);

        if (_testButton is null)
        {
            return; // Called during construction.
        }

        Size client = ClientSize;
        int pad = S(16);
        int gap = S(16);
        int sidebarWidth = S(340);
        int sidebarX = client.Width - pad - sidebarWidth;
        int iconButton = S(34);
        int rowHeight = S(34);

        // Log header: title on the left, toggles and Clear on the right.
        int logRight = sidebarX - gap;
        _clearButton.Bounds = new Rectangle(logRight - iconButton, pad, iconButton, iconButton);
        int toggleY = pad + (iconButton - S(20)) / 2;
        _receivedToggle.Bounds = new Rectangle(_clearButton.Left - S(12) - S(40), toggleY, S(40), S(20));
        _receivedLabel.Bounds = new Rectangle(_receivedToggle.Left - S(8) - S(90), pad, S(90), iconButton);
        _autoScrollToggle.Bounds = new Rectangle(_receivedLabel.Left - S(20) - S(40), toggleY, S(40), S(20));
        _autoScrollLabel.Bounds = new Rectangle(_autoScrollToggle.Left - S(8) - S(72), pad, S(72), iconButton);
        _logTitle.Bounds = new Rectangle(pad, pad, Math.Max(0, _autoScrollLabel.Left - pad), iconButton);
        _logCard.Bounds = new Rectangle(pad, _logTitle.Bottom + S(10), Math.Max(0, logRight - pad), Math.Max(0, client.Height - pad - _logTitle.Bottom - S(10)));

        // Sidebar
        _settingsButton.Bounds = new Rectangle(sidebarX + sidebarWidth - iconButton, pad, iconButton, iconButton);
        _keyCodesButton.Bounds = new Rectangle(_settingsButton.Left - S(4) - iconButton, pad, iconButton, iconButton);
        _title.Bounds = new Rectangle(sidebarX, pad, sidebarWidth - 2 * iconButton - S(4), iconButton);
        _status.Bounds = new Rectangle(sidebarX, _title.Bottom + S(10), sidebarWidth, _status.PreferredHeight);

        int y = _status.Bottom + S(18);
        _keysHeading.Bounds = new Rectangle(sidebarX, y, sidebarWidth, S(24));
        y = _keysHeading.Bottom + S(6);
        _comboSend.Bounds = new Rectangle(sidebarX + sidebarWidth - iconButton - S(10), y, iconButton + S(10), rowHeight);
        _comboButton.Bounds = new Rectangle(sidebarX, y, _comboSend.Left - S(8) - sidebarX, rowHeight);

        y += rowHeight + S(8);
        _keySend.Bounds = new Rectangle(_comboSend.Left, y, _comboSend.Width, rowHeight);
        _keyButton.Bounds = new Rectangle(sidebarX, y, S(140), rowHeight);
        _codeField.Bounds = new Rectangle(_keyButton.Right + S(8), y, _keySend.Left - S(8) - _keyButton.Right - S(8), rowHeight);

        y += rowHeight + S(18);
        _mouseHeading.Bounds = new Rectangle(sidebarX, y, sidebarWidth, S(24));
        y = _mouseHeading.Bottom + S(6);
        LayoutRow(_clickButtons, sidebarX, y, sidebarWidth, rowHeight);

        y += rowHeight + S(8);
        int stepWidth = S(64);
        _stepField.Bounds = new Rectangle(sidebarX + sidebarWidth - stepWidth, y, stepWidth, rowHeight);
        LayoutRow(_moveButtons, sidebarX, y, sidebarWidth - stepWidth - S(8), rowHeight);

        int bottomY = client.Height - pad - S(38);
        int half = (sidebarWidth - S(8)) / 2;
        _releaseButton.Bounds = new Rectangle(sidebarX, bottomY, half, S(38));
        _testButton.Bounds = new Rectangle(sidebarX + sidebarWidth - half, bottomY, half, S(38));
    }

    private void LayoutRow(Control[] controls, int x, int y, int width, int height)
    {
        int spacing = S(8);
        int each = (width - spacing * (controls.Length - 1)) / controls.Length;
        int end = x + width;
        for (int i = 0; i < controls.Length; i++)
        {
            int right = i == controls.Length - 1 ? end : x + each;
            controls[i].Bounds = new Rectangle(x, y, right - x, height);
            x = right + spacing;
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        bool userClosing = e.CloseReason is CloseReason.UserClosing or CloseReason.None;

        if (userClosing && !_exiting && AppSettings.Current.KeepInTray)
        {
            // Keep serving scripts from the notification area.
            e.Cancel = true;
            HideToTray();
            return;
        }

        base.OnFormClosing(e);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (WindowState == FormWindowState.Minimized && AppSettings.Current.KeepInTray)
        {
            HideToTray();
        }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        AppSettings.Changed -= Settings_Changed;
        _timer.Stop();
        _trayIcon.Visible = false;
        _server.Dispose();
        _engine.Dispose();
        base.OnFormClosed(e);
    }

    private void StartServer()
    {
        AppSettings settings = AppSettings.Current;
        _serverPort = settings.Port;
        _serverLan = settings.AllowLan;
        _server.Start(settings.Port, settings.AllowLan);
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        _pendingLog.Clear();
        AppLog.Drain(_pendingLog);
        if (_pendingLog.Count > 0 && Visible)
        {
            _log.Append(_pendingLog, AppSettings.Current.AutoScrollLog);
        }
        else if (_pendingLog.Count > 0)
        {
            // Hidden in the tray: still keep the history.
            _log.Append(_pendingLog, scrollToEnd: false);
        }

        UpdateStatus();
    }

    private void UpdateStatus()
    {
        Color ok = LogView.LevelColor(LogLevel.Success);
        Color bad = LogView.LevelColor(LogLevel.Error);

        _status.SetRow(0, _engine.IsConnected ? _engine.DeviceName ?? "Connected" : "Not connected", _engine.IsConnected ? ok : bad);

        if (_server.Endpoint is { } endpoint)
        {
            int clients = _server.ClientCount;
            string address = _serverLan ? $"port {endpoint.Port}" : $"127.0.0.1:{endpoint.Port}";
            _status.SetRow(1, clients switch
            {
                0 => address,
                1 => $"{address}  ·  1 client",
                _ => $"{address}  ·  {clients} clients",
            }, ok);
        }
        else
        {
            _status.SetRow(1, _server.Error is null ? "Stopped" : "Port in use", bad);
        }

        string speed = _engine.WriteCount == 0
            ? "No commands yet"
            : $"{_engine.LastWriteTime.TotalMilliseconds:0.00} ms  ·  avg {_engine.AverageWriteTime.TotalMilliseconds:0.00} ms";
        int queued = _engine.QueueLength;
        _status.SetRow(2, queued > 1 ? $"{speed}  ·  {queued} queued" : speed, null);
    }

    private void SelectPresetKey(string name, KeyCode key)
    {
        _presetKey = key;
        _keyButton.Text = name;
        _keyButton.Invalidate();
        _codeField.Text = $"{key} ({(int)key})";
        _codeField.Enabled = false;
    }

    /// <summary>Switches the single-key test to a custom code, optionally filled in (from the key codes window).</summary>
    public void SelectCustomKey(KeyCode? key)
    {
        _presetKey = null;
        _keyButton.Text = "Custom";
        _keyButton.Invalidate();
        _codeField.Enabled = true;
        _codeField.Text = key is { } k ? ((int)k).ToString() : string.Empty;
        _codeField.TextBox.Focus();
        _codeField.TextBox.SelectAll();
    }

    private void SendSingleKey()
    {
        if (_presetKey is { } preset)
        {
            _engine.Enqueue(BotCommand.Tap(preset));
            return;
        }

        if (KeyCodes.TryParse(_codeField.Text, out KeyCode key))
        {
            _engine.Enqueue(KeyCodes.KindOf(key) == KeyKind.MouseMove
                ? BotCommand.WithArgument(key, (byte)ReadMouseStep())
                : BotCommand.Tap(key));
        }
        else
        {
            AppLog.Warning($"\"{_codeField.Text.Trim()}\" is not a key code (0-{KeyCodes.Max}) or key name");
        }
    }

    private ModernButton MouseClickButton(string text, KeyCode key)
    {
        var button = new ModernButton { Glyph = Glyphs.Mouse, Text = text };
        button.Click += (_, _) =>
        {
            // The click lands where the cursor is, which may be this very button: ignore that echo.
            if (Environment.TickCount64 < _ignoreClicksUntil)
            {
                return;
            }

            _ignoreClicksUntil = Environment.TickCount64 + 500;
            _engine.Enqueue(BotCommand.Tap(key));
        };
        _toolTip.SetToolTip(button, $"Click the {text.ToLowerInvariant()} mouse button at the cursor (code {(int)key})");
        return button;
    }

    private ModernButton MouseMoveButton(string glyph, KeyCode key, string tip)
    {
        var button = new ModernButton { Glyph = glyph, AccessibleName = tip };
        button.Click += (_, _) => _engine.Enqueue(BotCommand.WithArgument(key, (byte)ReadMouseStep()));
        _toolTip.SetToolTip(button, $"{tip} (code {(int)key})");
        return button;
    }

    private int ReadMouseStep()
    {
        int step = int.TryParse(_stepField.Text, out int value) ? Math.Clamp(value, 1, 255) : AppSettings.Current.MouseStep;
        if (step != AppSettings.Current.MouseStep)
        {
            SaveSetting(s => s.MouseStep = step);
        }

        return step;
    }

    private static void SaveSetting(Action<AppSettings> change)
    {
        change(AppSettings.Current);
        AppSettings.Current.Save();
    }

    private void ShowKeyCodes()
    {
        if (_keyCodesForm is { IsDisposed: false })
        {
            _keyCodesForm.Activate();
            return;
        }

        _keyCodesForm = new KeyCodesForm();
        _keyCodesForm.KeyChosen += key =>
        {
            RestoreFromTray();
            SelectCustomKey(key);
        };
        _keyCodesForm.Show(this);
    }

    private void ShowSettings()
    {
        if (_settingsForm is not null)
        {
            _settingsForm.Activate();
            return;
        }

        using var settings = new SettingsForm();
        _settingsForm = settings;
        try
        {
            if (Visible)
            {
                settings.ShowDialog(this);
            }
            else
            {
                // Opened from the tray while the main window is hidden.
                settings.StartPosition = FormStartPosition.CenterScreen;
                settings.ShowInTaskbar = true;
                settings.ShowDialog();
            }
        }
        finally
        {
            _settingsForm = null;
        }
    }

    private void HideToTray()
    {
        if (!AppSettings.Current.KeepInTray || _exiting)
        {
            return;
        }

        Hide();

        if (!_trayHintShown)
        {
            _trayHintShown = true;
            _trayIcon.ShowBalloonTip(3000, "BotUSB is still running", "Scripts can keep sending keys. Click the tray icon to open BotUSB again.", ToolTipIcon.None);
        }
    }

    public void RestoreFromTray()
    {
        Show();
        if (WindowState == FormWindowState.Minimized)
        {
            WindowState = FormWindowState.Normal;
        }

        Activate();
        NativeMethods.SetForegroundWindow(Handle);
    }

    private void ExitApplication()
    {
        _exiting = true;
        Close();
    }

    private void Settings_Changed(object? sender, EventArgs e)
    {
        AppSettings settings = AppSettings.Current;
        _trayIcon.Visible = settings.KeepInTray;
        TopMost = settings.AlwaysOnTop;

        if (_autoScrollToggle.Checked != settings.AutoScrollLog)
        {
            _autoScrollToggle.Checked = settings.AutoScrollLog;
        }

        if (_receivedToggle.Checked != settings.ShowReceivedData)
        {
            _receivedToggle.Checked = settings.ShowReceivedData;
        }

        if (settings.Port != _serverPort || settings.AllowLan != _serverLan)
        {
            StartServer();
        }

        // Never leave the app running without any visible way back.
        if (!settings.KeepInTray && !Visible)
        {
            RestoreFromTray();
        }
    }

    private static Label Heading(string text) => new()
    {
        Text = text,
        AutoSize = false,
        TextAlign = ContentAlignment.MiddleLeft,
        ForeColor = Theme.Text,
        BackColor = Theme.Background,
    };

    private static Label Subheading(string text) => Heading(text);

    private static Label Caption(string text) => new()
    {
        Text = text,
        AutoSize = false,
        TextAlign = ContentAlignment.MiddleRight,
        ForeColor = Theme.TextSecondary,
        BackColor = Theme.Background,
    };

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Dispose();
            _toolTip.Dispose();
            _trayIcon.Dispose();
            _trayMenu.Dispose();
            _comboMenu.Dispose();
            _keyMenu.Dispose();
        }

        base.Dispose(disposing);
    }
}
