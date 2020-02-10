using AutoBot_v1._Bot._Keys;
using System;
using System.Windows.Forms;

namespace AutoBot_v1
{
    public partial class Info : Form
    {
        public Info()
        {
            InitializeComponent();
        }

        private void Info_Load(object sender, EventArgs e)
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

            Array keys = Enum.GetValues(typeof(KeyBotEnum));

            foreach (KeyBotEnum item in keys)
            {
                Grid.Rows.Add(item.ToString(), (int)item);
            }
        }
    }
}
