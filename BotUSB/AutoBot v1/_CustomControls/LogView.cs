using AutoBot_v1._Extension;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace AutoBot_v1._CustomControls
{
    public partial class LogView : UserControl
    {
        private int ID;
        public bool Last { get; set; }

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
            LoadGridSettings();
            LoadColumns();
        }

        private void LogView_Load(object sender, EventArgs e) { }

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
            Grid.ExecuteSafe(() =>
            {
                try
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
                            row.DefaultCellStyle.ForeColor = Color.White;
                            break;
                        case LogType.Information:
                            break;
                        case LogType.Error:
                            row.DefaultCellStyle.BackColor = Color.FromArgb(130, 124, 56);
                            row.DefaultCellStyle.ForeColor = Color.White;
                            break;
                    }

                    Grid.Rows.Add(row);
                    if (Last)
                    {
                        Grid.FirstDisplayedScrollingRowIndex = Grid.RowCount - 1;
                    }
                }
                catch { }
            });
        }

        public void Clear()
        {
            Grid.ExecuteSafe(() =>
            {
                Grid.Rows.Clear();
            });
        }
    }
}
