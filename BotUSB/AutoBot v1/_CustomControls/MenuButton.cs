using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AutoBot_v1._CustomControls
{
    public class MenuButton : Button
    {
        private ContextMenuStrip _menu;

        [DefaultValue(null), Browsable(true),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public ContextMenuStrip Menu
        {
            get
            {
                return _menu;
            }
            set
            {
                _menu = value;

                if (_menu != null)
                {
                    _menu.ItemClicked += _menu_ItemClicked;
                }
            }
        }

        private void _menu_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            if (e.ClickedItem != null)
            {
                this.Text = e.ClickedItem.Text;
            }
        }

        [DefaultValue(20), Browsable(true),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int SplitWidth { get; set; }

        public MenuButton()
        {
            SplitWidth = 20;
        }

        protected override void OnMouseDown(MouseEventArgs mevent)
        {
            Rectangle splitRect = new Rectangle(this.Width - this.SplitWidth, 0, this.SplitWidth, this.Height);

            if (Menu != null && mevent.Button == MouseButtons.Left && splitRect.Contains(mevent.Location))
            {
                Menu.Show(this, 0, this.Height);
            }
            else
            {
                base.OnMouseDown(mevent);
            }
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            base.OnPaint(pevent);

            if (this.Menu != null && this.SplitWidth > 0)
            {
                int arrowX = ClientRectangle.Width - 14;
                int arrowY = ClientRectangle.Height / 2 - 1;

                var arrowBrush = Enabled ? SystemBrushes.ControlText : SystemBrushes.ButtonShadow;
                var arrows = new[]
                {
                    new Point(arrowX, arrowY), new Point(arrowX + 7, arrowY), new Point(arrowX + 3, arrowY + 4)
                };
                pevent.Graphics.FillPolygon(arrowBrush, arrows);

                int lineX = ClientRectangle.Width - this.SplitWidth;
                int lineYFrom = arrowY - 4;
                int lineYTo = arrowY + 8;

                using (var separatorPen = new Pen(Brushes.DarkGray)
                {
                    DashStyle = DashStyle.Dot
                })
                {
                    pevent.Graphics.DrawLine(separatorPen, lineX, lineYFrom, lineX, lineYTo);
                }
            }
        }
    }
}
