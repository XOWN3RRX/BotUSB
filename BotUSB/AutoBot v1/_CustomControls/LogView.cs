using System;
using System.Drawing;
using System.Windows.Forms;

namespace AutoBot_v1._CustomControls
{
    public partial class LogView : UserControl
    {
        private int ID;
        public enum LogType
        {
            Exception,
            Information,
            Error
        }
        public LogView()
        {
            InitializeComponent();
            ID = 0;
        }

        private void LogView_Load(object sender, EventArgs e)
        {
            LoadGridSettings();
            LoadColumns();
        }

        private void LoadGridSettings()
        {
            Grid.AllowUserToAddRows = false;
            Grid.AllowUserToDeleteRows = false;
            Grid.AllowUserToOrderColumns = false;
            Grid.AllowUserToResizeColumns = false;
            Grid.AllowUserToResizeRows = false;
            Grid.ReadOnly = true;
            Grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            Grid.MultiSelect = false;
            Grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            Grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            Grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            Grid.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;
        }

        private void LoadColumns()
        {
            Grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ColumnID",
                HeaderText = "ID",

            });
            Grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ColumnType",
                HeaderText = "Type"
            });
            Grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ColumnDate",
                HeaderText = "Date",
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Format = "HH:mm:ss dd-MM-yyyy"
                }
            });
            Grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ColumnMessage",
                HeaderText = "Message"
            });

            Grid.Columns[0].Width = 40;
            Grid.Columns[1].Width = 100;
            Grid.Columns[2].Width = 150;
            Grid.Columns[3].Width = 210;
        }

        public void Log(string message, LogType type)
        {
            DataGridViewRow row = new DataGridViewRow();
            row.Cells.Add(new DataGridViewTextBoxCell
            {
                Value = (++ID).ToString()
            });

            string typeString = type.ToString();
            row.Cells.Add(new DataGridViewTextBoxCell
            {
                Value = typeString
            });

            row.Cells.Add(new DataGridViewTextBoxCell
            {
                Value = DateTime.Now
            });

            row.Cells.Add(new DataGridViewTextBoxCell
            {
                Value = message
            });

            switch (type)
            {
                case LogType.Exception:
                    row.DefaultCellStyle.BackColor = Color.FromArgb(117, 48, 48);
                    break;
                case LogType.Information:
                    break;
                case LogType.Error:
                    row.DefaultCellStyle.BackColor = Color.FromArgb(130, 124, 56);
                    break;
            }

            Grid.Rows.Add(row);
        }
    }
}
