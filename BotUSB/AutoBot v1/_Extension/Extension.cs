using System.Windows.Forms;

namespace AutoBot_v1._Extension
{
    public static class Extension
    {
        public static void TextSafe(this Control control, string text)
        {
            if (control == null)
            {
                return;
            }

            if (control.InvokeRequired)
            {
                control.Invoke((MethodInvoker)delegate
                {
                    control.Text = text;
                });
            }
            else
            {
                control.Text = text;
            }
        }
    }
}
