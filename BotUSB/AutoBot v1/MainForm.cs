using System;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using AutoBot_v1._Bot;
using AutoBot_v1._Bot._JSON;
using AutoBot_v1._Bot._Keys;
using AutoBot_v1._Bot._TCPListener;
using AutoBot_v1._CustomControls;
using AutoBot_v1._Extension;
using AutoBot_v1.Properties;
using Newtonsoft.Json;

namespace AutoBot_v1
{
    public partial class MainForm : Form
    {
        private TCPThread tcp;
        private LogView logView;
        private BotQueue botQueue;

        private ClientData clientData;
        private ClientData[] clientDataMany;

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

            clientDataMany = new ClientData[] { };

            trayToolStripMenuItem.Checked = Settings.Default.Settings_Tray;
            topToolStripMenuItem.Checked = Settings.Default.Settings_Top;
            lastToolStripMenuItem.Checked = Settings.Default.Settings_Last;
            this.TopMost = Settings.Default.Settings_Top;

            tcp = new TCPThread();
            tcp.OnChangeStatus += Tcp_OnChangeStatus;
            tcp.OnTriggerServerAction = ClientAction;

            logView.Log("IP : " + tcp.LocalAddress, LogView.LogType.Information);
            logView.Log("Port : " + tcp.Port, LogView.LogType.Information);
            logView.Last = Settings.Default.Settings_Last;

            tcp.Run();

            botQueue = new BotQueue();
            botQueue.OnErrorOccured += BotQueue_OnErrorOccured;
        }

        private void BotQueue_OnErrorOccured(string message, LogView.LogType logType)
        {
            logView.Log(message?.ToString(), logType);
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
            botQueue.Stop();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Bot.Instance.PressAndRelease(KeyBotEnum._CAPS);
        }

        private void ClienActionMultipleObjects(string data)
        {
            try
            {
                if (data.Length > 0)
                {
                    logView.Log("Trying parsing multiple data...", LogView.LogType.Information);

                    data = "[" + data + "]";

                    data = data.Replace("}", "},");
                    int index = data.LastIndexOf(',');
                    data = data.Remove(index, 1);

                    clientDataMany = JsonConvert.DeserializeObject<ClientData[]>(data);

                    if (clientDataMany.Length > 0)
                    {
                        foreach (ClientData item in clientDataMany)
                        {
                            botQueue.Queue.Enqueue(item);
                        }
                    }

                    logView.Log("Success parsed multiple objects...", LogView.LogType.Information);
                }
                else
                {
                    logView.Log("Nothing send from client in ClienActionMultipleObjects", LogView.LogType.Error);
                }
            }
            catch (Exception ex)
            {
                logView.Log("Client multiple objects, not parsed...", LogView.LogType.Exception);
                logView.Log(ex.Message, LogView.LogType.Exception);
            }
        }

        private void ClientAction(string data)
        {
            try
            {
                try
                {
                    clientData = null;
                    clientData = JsonConvert.DeserializeObject<ClientData>(data);

                    if (clientData != null)
                    {
                        botQueue.Queue.Enqueue(clientData);
                    }
                }
                catch (Exception ex)
                {
                    int? count = data?.Where(x => x.Equals('{')).Count();
                    if (count != null && count > 1)
                    {
                        ClienActionMultipleObjects(data);
                    }
                    else
                    {
                        logView.Log("Client send wrong data", LogView.LogType.Exception);
                        logView.Log(ex.Message, LogView.LogType.Exception);
                        logView.Log(data, LogView.LogType.Information);
                        return;
                    }

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

        private void trayToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem item = (sender as ToolStripMenuItem);

            item.Checked = !item.Checked;

            Settings.Default.Settings_Tray = item.Checked;
            Settings.Default.Save();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (Settings.Default.Settings_Tray)
            {
                e.Cancel = true;
                this.WindowState = FormWindowState.Minimized;
                this.notifyIcon1.Visible = true;
                this.ShowInTaskbar = false;
            }
        }

        private void notifyIcon1_DoubleClick(object sender, EventArgs e)
        {
            ShowApp();
        }

        private void ShowApp()
        {
            this.WindowState = FormWindowState.Normal;
            this.notifyIcon1.Visible = false;
            this.ShowInTaskbar = true;
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CloseApp();
        }

        private void CloseApp()
        {
            Settings.Default.Settings_Tray = false;
            this.Close();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowApp();
        }

        private void closeToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            CloseApp();
        }

        private void topToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.TopMost = !this.TopMost;
            this.topToolStripMenuItem.Checked = this.TopMost;

            Settings.Default.Settings_Top = this.TopMost;
            Settings.Default.Save();
        }

        private void lastToolStripMenuItem_Click(object sender, EventArgs e)
        {
            logView.Last = !logView.Last;
            lastToolStripMenuItem.Checked = logView.Last;

            Settings.Default.Settings_Last = logView.Last;
            Settings.Default.Save();
        }

        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            logView.Clear();
        }
    }
}
