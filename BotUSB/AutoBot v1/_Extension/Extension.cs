using System;
using System.Windows.Forms;

namespace AutoBot_v1._Extension
{
    public static class Extension
    {
        public static void ExecuteSafe(this Control control, Action action)
        {
            if (control.InvokeRequired)
            {
                control.Invoke((MethodInvoker)delegate
                {
                    action();
                });
            }
            else
            {
                action();
            }
        }
    }
}
