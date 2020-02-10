using System;
using System.Drawing;
using System.Windows.Forms;
using AutoBot_v1._Bot;
using AutoBot_v1._Bot._JSON;
using AutoBot_v1._Bot._Keys;
using AutoBot_v1._Bot._TCPListener;
using AutoBot_v1._CustomControls;
using AutoBot_v1._Extension;
using Newtonsoft.Json;

namespace AutoBot_v1
{
    public partial class MainForm : Form
    {
        private TCPThread tcp;
        private LogView logView;

        private ClientData clientData;

        public MainForm()
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

            logView.Log("IP : " + tcp.LocalAddress, LogView.LogType.Information);
            logView.Log("Port : " + tcp.Port, LogView.LogType.Information);

            tcp.Run();
        }

        private void Tcp_OnChangeStatus(object sender, EventArgs e)
        {
            try
            {
                lblServerStatus.TextSafe(sender?.ToString());
                logView.Log(sender?.ToString(), LogView.LogType.Information);
            }
            catch
            {
                lblServerStatus.TextSafe("TCP SERVER EXCEPTION");
                logView.Log("TCP SERVER EXCEPTION", LogView.LogType.Exception);
            }
        }

        private void AutobotConnected()
        {
            this.lblStatus.TextSafe("USB Connected");
            this.lblStatus.ForeColor = Color.Green;

            logView.Log("USB Connected", LogView.LogType.Information);
        }

        private void AutobotDisconnected()
        {
            this.lblStatus.TextSafe("USB Disconnected");
            this.lblStatus.ForeColor = Color.Red;

            logView.Log("USB Disconnected", LogView.LogType.Information);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Bot.Instance.PressAndRelease(KeyBotEnum._AltL, KeyBotEnum.Tab);
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            HIDDLLInterface.DisconnectFromHID();
            tcp.StopServer();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Bot.Instance.Press(KeyBotEnum.CAPS);
        }

        private void ClientAction(string data)
        {
            try
            {
                try
                {
                    clientData = null;
                    clientData = JsonConvert.DeserializeObject<ClientData>(data);

                    if (clientData.Message != null)
                    {
                        Bot.Instance.PressMessage(clientData.Message);
                    }
                    else if (clientData.Keys.Length == 1)
                    {
                        Bot.Instance.PressAndRelease(clientData.Keys[0]);
                    }
                    else if (clientData.Keys.Length == 2)
                    {
                        Bot.Instance.PressAndRelease(clientData.Keys[0], clientData.Keys[1]);
                    }
                    else
                    {
                        logView.Log("UNDEFINED DATA", LogView.LogType.Error);
                    }
                }
                catch(Exception ex)
                {
                    logView.Log("Client send wrong data", LogView.LogType.Exception);
                    logView.Log(ex.Message, LogView.LogType.Exception);
                    logView.Log(data, LogView.LogType.Information);
                    return;
                }
            }
            catch
            {
                this.lblServerStatus.TextSafe("Client send wrong data");
                logView.Log("Client send wrong data", LogView.LogType.Error);
            }
        }

        private void btnInfo_Click(object sender, EventArgs e)
        {
            Info inf = new Info();
            inf.Show();
        }
    }
}
