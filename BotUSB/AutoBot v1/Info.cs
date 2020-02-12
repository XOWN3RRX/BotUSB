using AutoBot_v1._Bot._Keys;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace AutoBot_v1
{
    public partial class Info : Form
    {
        private Dictionary<string, int> dict;
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
            dict = new Dictionary<string, int>();

            foreach (KeyBotEnum item in keys)
            {
                Grid.Rows.Add(item.ToString(), (int)item);
                dict.Add(item.ToString(), (int)item);
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            Grid.Rows.Clear();

            if (textBox1.Text.Length == 0)
            {
                foreach (var item in dict)
                {
                    Grid.Rows.Add(item.Key, item.Value);
                }
            }
            else
            {
                string text = textBox1.Text.ToLower();
                IEnumerable<KeyValuePair<string, int>> result = dict.Where(x => x.Key.ToLower().Contains(text) || x.Value.ToString().Contains(text));

                foreach (var item in result)
                {
                    Grid.Rows.Add(item.Key, item.Value);
                }
            }
        }
    }
}
