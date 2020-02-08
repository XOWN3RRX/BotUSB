using System;
using System.Drawing;
using System.Windows.Forms;
using AutoBot_v1._Bot;
using AutoBot_v1._Bot._Keys;
using AutoBot_v1._Bot._TCPListener;
using AutoBot_v1._CustomControls;

namespace AutoBot_v1
{
    public partial class Form1 : Form
    {
        private TCPThread tcp;
        private LogView logView;

        public Form1()
        {
            InitializeComponent();
        }

        #region Unmanaged functions
        public void OnPlugged(int pHandle)
        {
            if (HIDDLLInterface.hidGetVendorID(pHandle) == 5638 & HIDDLLInterface.hidGetProductID(pHandle) == 6536)
            {
                AutobotConnected();
                Bot.Instance.CONNECTED = true;
            }
        }

        public void OnUnplugged(int pHandle)
        {
            if (HIDDLLInterface.hidGetVendorID(pHandle) == 5638 & HIDDLLInterface.hidGetProductID(pHandle) == 6536)
            {
                HIDDLLInterface.hidSetReadNotify(HIDDLLInterface.hidGetHandle(5638, 6536), false);
                AutobotDisconnected();
                Bot.Instance.CONNECTED = false;
            }
        }

        public void OnChanged()
        {
            int num = HIDDLLInterface.hidGetHandle(5638, 6536);
            HIDDLLInterface.hidSetReadNotify(HIDDLLInterface.hidGetHandle(5638, 6536), true);
        }
        #endregion

        private void Form1_Load(object sender, EventArgs e)
        {
            logView = new LogView();
            logView.Dock = DockStyle.Fill;
            logPanel.Controls.Add(logView);
            logView.Log("Application running", LogView.LogType.Information);

            AutobotDisconnected();
            Form form = this;
            HIDDLLInterface.ConnectToHID(ref form);

            tcp = new TCPThread();
            tcp.OnChangeStatus += Tcp_OnChangeStatus;
            tcp.OnTriggerServerAction = ClientAction;
            tcp.Run();
        }

        private void Tcp_OnChangeStatus(object sender, EventArgs e)
        {
            try
            {
                lblServerStatus.Invoke((MethodInvoker)delegate
                {
                    lblServerStatus.Text = sender?.ToString();
                });

                logView.Invoke((MethodInvoker)delegate
                {
                    logView.Log(sender?.ToString(), LogView.LogType.Information);
                });
            }
            catch
            {
                lblServerStatus.Text = "TCP SERVER EXCEPTION";
                logView.Log("TCP SERVER EXCEPTION", LogView.LogType.Exception);
            }
        }

        private void AutobotConnected()
        {
            this.lblStatus.Text = "USB Connected";
            this.lblStatus.ForeColor = Color.Green;

            logView.Log("USB Connected", LogView.LogType.Information);
        }

        private void AutobotDisconnected()
        {
            this.lblStatus.Text = "USB Disconnected";
            this.lblStatus.ForeColor = Color.Red;

            logView.Log("USB Disconnected", LogView.LogType.Information);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Bot.Instance.PressAndRelease(BotKeyEnum._AltL, BotKeyEnum.Tab);
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            HIDDLLInterface.DisconnectFromHID();
            tcp.StopServer();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Bot.Instance.Press(BotKeyEnum.CAPS);
        }

        private void ClientAction(string data)
        {
            try
            {
                int keyCode = -1;
                int.TryParse(data, out keyCode);

                int index = Array.IndexOf(Enum.GetValues(typeof(BotKeyEnum)), (BotKeyEnum)keyCode);

                if (index == -1)
                {
                    this.logView.Invoke((MethodInvoker)delegate
                    {
                        logView.Log("Client send wrong data", LogView.LogType.Error);
                    });
                    return;
                }

                Bot.Instance.PressAndRelease((BotKeyEnum)keyCode);
            }
            catch
            {
                this.lblServerStatus.Invoke((MethodInvoker)delegate
                {
                    lblServerStatus.Text = "Client send wrong data";
                });

                this.logView.Invoke((MethodInvoker)delegate
                {
                    logView.Log("Client send wrong data", LogView.LogType.Error);
                });
            }
        }
    }
}
