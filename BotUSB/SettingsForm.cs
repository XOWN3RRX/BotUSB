using BotUSB.UI;
using BotUSB.UI.Controls;

namespace BotUSB;

/// <summary>App settings. Changes are saved immediately.</summary>
internal sealed class SettingsForm : Form
{
    private readonly (Label Heading, SettingRow[] Rows)[] _groups;
    private readonly TextField _portField;
    private readonly ModernButton _doneButton;

    public SettingsForm()
    {
        AppSettings settings = AppSettings.Current;

        SuspendLayout();
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(520, 620);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Settings";
        Icon = Program.AppIcon;
        Theme.ApplyTo(this);

        _portField = new TextField { Text = settings.Port.ToString(), Size = new Size(110, 32) };
        _portField.TextBox.MaxLength = 5;
        _portField.TextBox.AccessibleName = "Port";
        _portField.TextBox.TextAlign = HorizontalAlignment.Center;
        _portField.TextBox.Leave += (_, _) => ApplyPort();
        _portField.TextBox.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                ApplyPort();
                e.SuppressKeyPress = true;
            }
        };

        _groups =
        [
            (Heading("General"),
            [
                new SettingRow("Keep running in the tray", "Closing or minimizing hides BotUSB to the notification area",
                    Toggle(settings.KeepInTray, v => settings.KeepInTray = v)),
                new SettingRow("Always on top", "Keep the BotUSB window above other windows",
                    Toggle(settings.AlwaysOnTop, v => settings.AlwaysOnTop = v)),
            ]),
            (Heading("Script server"),
            [
                new SettingRow("Port", $"TCP port scripts connect to (default {AppSettings.DefaultPort})", _portField),
                new SettingRow("Allow other computers", "Anyone on your network can then send keys",
                    Toggle(settings.AllowLan, v => settings.AllowLan = v)),
            ]),
            (Heading("Log"),
            [
                new SettingRow("Auto-scroll", "Scroll to new messages",
                    Toggle(settings.AutoScrollLog, v => settings.AutoScrollLog = v)),
                new SettingRow("Show received data", "Log every command received from scripts",
                    Toggle(settings.ShowReceivedData, v => settings.ShowReceivedData = v)),
            ]),
        ];

        _doneButton = new ModernButton { Kind = ButtonKind.Accent, Text = "Done", DialogResult = DialogResult.OK };
        AcceptButton = _doneButton;
        CancelButton = _doneButton;

        foreach ((Label heading, SettingRow[] rows) in _groups)
        {
            Controls.Add(heading);
            Controls.AddRange(rows);
        }

        Controls.Add(_doneButton);
        ResumeLayout(false);

        ToggleSwitch Toggle(bool value, Action<bool> apply)
        {
            var toggle = new ToggleSwitch { Checked = value };
            toggle.CheckedChanged += (_, _) =>
            {
                apply(toggle.Checked);
                settings.Save();
            };
            return toggle;
        }
    }

    private int S(int logical) => LogicalToDeviceUnits(logical);

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        foreach ((Label heading, _) in _groups)
        {
            heading.Font = Theme.CreateStrongFont(Font, 1.2f);
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        ApplyPort();
        base.OnFormClosing(e);
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);

        if (_doneButton is null)
        {
            return; // Called during construction.
        }

        int pad = S(20);
        int width = ClientSize.Width - 2 * pad;
        int y = pad - S(4);

        foreach ((Label heading, SettingRow[] rows) in _groups)
        {
            heading.Bounds = new Rectangle(pad, y, width, S(32));
            y = heading.Bottom + S(4);
            foreach (SettingRow row in rows)
            {
                row.Bounds = new Rectangle(pad, y, width, S(62));
                y = row.Bottom + S(4);
            }

            y += S(14);
        }

        _doneButton.Bounds = new Rectangle(ClientSize.Width - pad - S(110), ClientSize.Height - pad - S(34), S(110), S(34));
    }

    private void ApplyPort()
    {
        AppSettings settings = AppSettings.Current;
        if (int.TryParse(_portField.Text, out int port) && port is >= 1 and <= 65535)
        {
            if (port != settings.Port)
            {
                settings.Port = port;
                settings.Save();
            }
        }
        else
        {
            _portField.Text = settings.Port.ToString();
        }
    }

    private static Label Heading(string text) => new()
    {
        Text = text,
        AutoSize = false,
        TextAlign = ContentAlignment.BottomLeft,
        ForeColor = Theme.Text,
        BackColor = Theme.Background,
    };
}
