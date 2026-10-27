using System.Drawing.Drawing2D;
using BotUSB.Device;
using BotUSB.UI;
using BotUSB.UI.Controls;

namespace BotUSB;

/// <summary>Searchable list of every key code. Double-click (or Enter) puts the code into the main window.</summary>
internal sealed class KeyCodesForm : Form
{
    private readonly Label _hint;
    private readonly TextField _search;
    private readonly KeyCodeList _list;

    public KeyCodesForm()
    {
        SuspendLayout();
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(460, 620);
        MinimumSize = new Size(380, 400);
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        Text = "Key codes";
        Icon = Program.AppIcon;
        KeyPreview = true;
        Theme.ApplyTo(this);

        _hint = new Label
        {
            Text = "Tap codes are pressed and released. Hold codes stay down until 0 (NULL). Double-click a code to put it in the main window's test box.",
            AutoSize = false,
            ForeColor = Theme.TextSecondary,
            BackColor = Theme.Background,
        };

        _search = new TextField { Glyph = Glyphs.Search, PlaceholderText = "Search by name or code" };
        _search.TextChanged += (_, _) => ApplyFilter();
        _search.TextBox.KeyDown += Search_KeyDown;

        _list = new KeyCodeList();
        _list.MouseDoubleClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left && _list.IndexFromPoint(e.Location) >= 0)
            {
                ChooseSelected();
            }
        };
        _list.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                ChooseSelected();
                e.SuppressKeyPress = true;
            }
        };

        Controls.AddRange([_hint, _search, _list]);
        ApplyFilter();
        ResumeLayout(false);
    }

    /// <summary>Raised when the user picks a code.</summary>
    public event Action<KeyCode>? KeyChosen;

    private int S(int logical) => LogicalToDeviceUnits(logical);

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        _hint.Font = Theme.CreateCaptionFont(Font);
        _search.TextBox.Focus();
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);

        if (_list is null)
        {
            return; // Called during construction.
        }

        int pad = S(16);
        int width = ClientSize.Width - 2 * pad;
        _search.Bounds = new Rectangle(pad, pad, width, S(34));
        _hint.Bounds = new Rectangle(pad, _search.Bottom + S(6), width, S(36));
        _list.Bounds = new Rectangle(pad, _hint.Bottom + S(4), width, Math.Max(0, ClientSize.Height - pad - _hint.Bottom - S(4)));
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Escape)
        {
            Close();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void ApplyFilter()
    {
        string filter = _search.Text.Trim();
        KeyCode[] matches = [.. KeyCodes.All.Where(k => Matches(k, filter))];

        _list.BeginUpdate();
        _list.Items.Clear();
        foreach (KeyCode key in matches)
        {
            _list.Items.Add(key);
        }

        if (matches.Length > 0)
        {
            _list.SelectedIndex = 0;
        }

        _list.EndUpdate();
    }

    private static bool Matches(KeyCode key, string filter)
    {
        if (filter.Length == 0)
        {
            return true;
        }

        if (int.TryParse(filter, out int number))
        {
            return (int)key == number || ((int)key).ToString().StartsWith(filter, StringComparison.Ordinal);
        }

        return key.ToString().Contains(filter, StringComparison.OrdinalIgnoreCase)
            || KeyCodes.DisplayName(key).Contains(filter, StringComparison.OrdinalIgnoreCase);
    }

    private void ChooseSelected()
    {
        if (_list.SelectedItem is KeyCode key)
        {
            KeyChosen?.Invoke(key);
        }
    }

    private void Search_KeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.Enter:
                ChooseSelected();
                e.SuppressKeyPress = true;
                break;
            case Keys.Down or Keys.Up when _list.Items.Count > 0:
                int next = _list.SelectedIndex + (e.KeyCode == Keys.Down ? 1 : -1);
                _list.SelectedIndex = Math.Clamp(next, 0, _list.Items.Count - 1);
                e.SuppressKeyPress = true;
                break;
        }
    }

    /// <summary>Rows: code badge, key name, and what the code does.</summary>
    private sealed class KeyCodeList : ListBox
    {
        private const int LogicalItemHeight = 44;

        private int _hoverIndex = -1;
        private Font? _captionFont;
        private Font? _codeFont;

        public KeyCodeList()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            DrawMode = DrawMode.OwnerDrawFixed;
            BorderStyle = BorderStyle.None;
            IntegralHeight = false;
            BackColor = Theme.Background;
            ForeColor = Theme.Text;
            ItemHeight = LogicalItemHeight;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ItemHeight = LogicalToDeviceUnits(LogicalItemHeight);
        }

        protected override void OnDpiChangedAfterParent(EventArgs e)
        {
            base.OnDpiChangedAfterParent(e);
            ItemHeight = LogicalToDeviceUnits(LogicalItemHeight);
        }

        protected override void OnFontChanged(EventArgs e)
        {
            _captionFont?.Dispose();
            _codeFont?.Dispose();
            _captionFont = _codeFont = null;
            base.OnFontChanged(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int index = IndexFromPoint(e.Location);
            if (index >= 0 && !GetItemRectangle(index).Contains(e.Location))
            {
                index = -1;
            }

            SetHoverIndex(index);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            SetHoverIndex(-1);
        }

        protected override void OnSelectedIndexChanged(EventArgs e)
        {
            base.OnSelectedIndexChanged(e);
            Invalidate();
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            Graphics g = e.Graphics;
            using (var background = new SolidBrush(BackColor))
            {
                g.FillRectangle(background, e.Bounds);
            }

            if (e.Index < 0 || e.Index >= Items.Count || Items[e.Index] is not KeyCode key)
            {
                return;
            }

            bool selected = (e.State & DrawItemState.Selected) != 0;
            int pad = LogicalToDeviceUnits(2);
            var card = Rectangle.Inflate(e.Bounds, -pad, -pad);

            g.SmoothingMode = SmoothingMode.AntiAlias;
            if (selected || e.Index == _hoverIndex)
            {
                g.FillRoundedRectangle(selected ? Theme.SurfaceSelected : Theme.SurfaceHover, card, LogicalToDeviceUnits(4));
            }

            if (selected)
            {
                int pillHeight = LogicalToDeviceUnits(16);
                var pill = new RectangleF(card.Left, card.Top + (card.Height - pillHeight) / 2f, LogicalToDeviceUnits(3), pillHeight);
                g.FillRoundedRectangle(Theme.Accent, pill, pill.Width / 2f);
            }

            _captionFont ??= Theme.CreateCaptionFont(Font);
            _codeFont ??= Theme.CreateStrongFont(Font);
            const TextFormatFlags flags = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix;

            // Code badge
            int badgeWidth = LogicalToDeviceUnits(44);
            int badgeHeight = LogicalToDeviceUnits(24);
            var badge = new Rectangle(card.Left + LogicalToDeviceUnits(12), card.Top + (card.Height - badgeHeight) / 2, badgeWidth, badgeHeight);
            KeyKind kind = KeyCodes.KindOf(key);
            Color badgeColor = kind switch
            {
                KeyKind.Hold => Theme.Accent,
                KeyKind.MouseMove => LogView.LevelColor(LogLevel.Success),
                KeyKind.Release => LogView.LevelColor(LogLevel.Warning),
                _ => Theme.TextSecondary,
            };
            g.FillRoundedRectangle(Theme.Blend(badgeColor, BackColor, 0.82), badge, LogicalToDeviceUnits(4));
            TextRenderer.DrawText(g, ((int)key).ToString(), _codeFont, badge, badgeColor,
                flags | TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            int x = badge.Right + LogicalToDeviceUnits(12);
            int textWidth = Math.Max(0, card.Right - x - LogicalToDeviceUnits(8));
            int titleHeight = TextRenderer.MeasureText(g, "Ag", Font, Size.Empty, flags).Height;
            int captionHeight = TextRenderer.MeasureText(g, "Ag", _captionFont, Size.Empty, flags).Height;
            int top = card.Top + (card.Height - titleHeight - captionHeight) / 2;

            string name = key.ToString();
            string display = KeyCodes.DisplayName(key);
            TextRenderer.DrawText(g, display == name.TrimStart('_') ? name : $"{name}  ·  {display}", Font, new Rectangle(x, top, textWidth, titleHeight), Theme.Text, flags);
            TextRenderer.DrawText(g, KeyCodes.KindName(kind), _captionFont, new Rectangle(x, top + titleHeight, textWidth, captionHeight), Theme.TextSecondary, flags);
        }

        private void SetHoverIndex(int index)
        {
            if (index == _hoverIndex)
            {
                return;
            }

            if (_hoverIndex >= 0 && _hoverIndex < Items.Count)
            {
                Invalidate(GetItemRectangle(_hoverIndex));
            }

            _hoverIndex = index;
            if (index >= 0)
            {
                Invalidate(GetItemRectangle(index));
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _captionFont?.Dispose();
                _codeFont?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
