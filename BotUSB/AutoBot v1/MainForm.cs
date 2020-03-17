using System;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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

        private MenuButton btnMenuDropDown;

        private object locker = new object();
        private bool tested = false;

        private const string CUSTOM_BUTTON = "Custom";

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
                tested = false;
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

            CreateDropDownButton();
            CreateDropDownButtonCombinations();

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

            Bot.Instance.OnPingTrigger += Instance_OnPingTrigger;

            tcp.Run();

            botQueue = new BotQueue();
            botQueue.OnErrorOccured += BotQueue_OnErrorOccured;
        }

        private void CreateDropDownButtonCombinations()
        {
            ContextMenuStrip contextMenu = new ContextMenuStrip();

            btnCombination.Text = "ALT+TAB";
            btnCombination.Tag = new Keyword((char)0, KeyBotEnum._AltL, KeyBotEnum._Tab);
            AddItemsContextMenu("ALT+TAB", new Keyword((char)0, KeyBotEnum._AltL, KeyBotEnum._Tab), contextMenu, ContextMenuCombinations_Click);
            AddItemsContextMenu("Win+D", new Keyword((char)0, KeyBotEnum._WinL, KeyBotEnum._D), contextMenu, ContextMenuCombinations_Click);
            AddItemsContextMenu("Win+Tab", new Keyword((char)0, KeyBotEnum._WinL, KeyBotEnum._Tab), contextMenu, ContextMenuCombinations_Click);
            AddItemsContextMenu("Win+R", new Keyword((char)0, KeyBotEnum._WinL, KeyBotEnum._R), contextMenu, ContextMenuCombinations_Click);
            AddItemsContextMenu("Ctrl+Esc", new Keyword((char)0, KeyBotEnum._CtrlL, KeyBotEnum._Esc), contextMenu, ContextMenuCombinations_Click);
            AddItemsContextMenu("Ctrl+Alt+Del", new Keyword((char)0, KeyBotEnum._CtrlL, KeyBotEnum._AltL, KeyBotEnum._Delete), contextMenu, ContextMenuCombinations_Click);

            btnCombination.Menu = contextMenu;
        }

        private void CreateDropDownButton()
        {
            btnMenuDropDown = new MenuButton();
            btnMenuDropDown.Location = new Point(89, 19);
            btnMenuDropDown.Size = new Size(75, 23);
            btnMenuDropDown.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            btnMenuDropDown.Text = "CAPS";
            btnMenuDropDown.TabIndex = 3;
            btnMenuDropDown.UseVisualStyleBackColor = true;
            btnMenuDropDown.TextAlign = ContentAlignment.MiddleLeft;

            btnMenuDropDown.Click += new System.EventHandler(MenuButtonClick);

            ContextMenuStrip contextMenu = new ContextMenuStrip();

            Array keys = Enum.GetValues(typeof(KeyBotEnum));

            btnMenuDropDown.Tag = (int)KeyBotEnum._CAPS;
            AddItemsContextMenu("CAPS", (int)KeyBotEnum._CAPS, contextMenu, ContextMenu_Click);
            AddItemsContextMenu("Win", (int)KeyBotEnum._WinL, contextMenu, ContextMenu_Click);
            AddItemsContextMenu("PrintScreen", (int)KeyBotEnum._PrtSrc, contextMenu, ContextMenu_Click);
            AddItemsContextMenu("Escape", (int)KeyBotEnum._Esc, contextMenu, ContextMenu_Click);

            AddItemsContextMenu(CUSTOM_BUTTON, 0, contextMenu, ContextMenu_Click);

            btnMenuDropDown.Menu = contextMenu;

            groupBox3.Controls.Add(btnMenuDropDown);
        }

        private void AddItemsContextMenu(string name, object key, ContextMenuStrip parent, EventHandler eventTrigger)
        {
            ToolStripItem result = parent.Items.Add(name);
            result.Tag = key;
            result.Click += eventTrigger;
        }

        private void ContextMenuCombinations_Click(object sender, EventArgs e)
        {
            ToolStripItem result = (sender as ToolStripItem);
            btnCombination.Tag = result.Tag;
        }

        private void ContextMenu_Click(object sender, EventArgs e)
        {
            ToolStripItem result = (sender as ToolStripItem);
            btnMenuDropDown.Tag = result.Tag;

            if (result.Text.Equals(CUSTOM_BUTTON))
            {
                customTextBox.Visible = true;
            }
            else
            {
                customTextBox.Visible = false;
            }
        }

        private void MenuButtonClick(object sender, EventArgs eventArgs)
        {
            if ((sender as Button).Text.Equals(CUSTOM_BUTTON))
            {
                int result;
                int.TryParse(customTextBox.Text, out result);

                KeyBotEnum min = Enum.GetValues(typeof(KeyBotEnum)).Cast<KeyBotEnum>().Min();
                KeyBotEnum max = Enum.GetValues(typeof(KeyBotEnum)).Cast<KeyBotEnum>().Max();

                if (result >= (int)min && result <= (int)max)
                {
                    Bot.Instance.PressAndRelease((KeyBotEnum)result);
                }
            }
            else
            {
                Bot.Instance.PressAndRelease((KeyBotEnum)(sender as Button).Tag);
            }
        }

        private void Instance_OnPingTrigger(int ping)
        {
            lblPingStatus.ExecuteSafe(() =>
            {
                lblPingStatus.Text = ping.ToString() + " ms";
            });
        }

        private void BotQueue_OnErrorOccured(string message, LogView.LogType logType)
        {
            logView.Log(message?.ToString(), logType);
        }

        private void Tcp_OnChangeStatus(object sender, EventArgs e)
        {
            try
            {
                lblServerStatus.ExecuteSafe(() =>
                {
                    lblServerStatus.Text = sender?.ToString();
                });
                logView.Log(sender?.ToString(), LogView.LogType.Information);
            }
            catch
            {
                lblServerStatus.ExecuteSafe(() =>
                {
                    lblServerStatus.Text = "TCP SERVER EXCEPTION";
                });
                logView.Log("TCP SERVER EXCEPTION", LogView.LogType.Exception);
            }
        }

        private void AutobotConnected()
        {
            this.lblStatus.ExecuteSafe(() =>
            {
                lblStatus.Text = "USB Connected, usb driver test...";
                lblStatus.ForeColor = Color.Gray;
            });

            logView.Log("USB Connected", LogView.LogType.Information);

            TestDriver();
        }

        private void TestDriver()
        {
            try
            {
                Task.Run(() =>
                {
                    lock (locker)
                    {
                        if (!tested)
                        {
                            Thread.Sleep(2000);

                            DateTime dt = DateTime.Now;
                            Bot.Instance.PressAndRelease(KeyBotEnum.NULL);
                            double elapsed = DateTime.Now.Subtract(dt).Milliseconds;

                            if (elapsed < 10)
                            {
                                this.lblStatus.ExecuteSafe(() =>
                                {
                                    lblStatus.Text = "USB Connected, usb driver fail";
                                    lblStatus.ForeColor = Color.Red;
                                });
                            }
                            else
                            {
                                this.lblStatus.ExecuteSafe(() =>
                                {
                                    lblStatus.Text = "USB Connected, usb driver passed";
                                    lblStatus.ForeColor = Color.Green;
                                });
                            }

                            tested = true;
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                logView.Log("TestDriver Exception", LogView.LogType.Exception);
                logView.Log(ex.Message, LogView.LogType.Exception);
            }
        }

        private void AutobotDisconnected()
        {
            this.lblStatus.ExecuteSafe(() =>
            {
                lblStatus.Text = "USB Disconnected";
                lblStatus.ForeColor = Color.Red;
            });

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
                this.lblServerStatus.ExecuteSafe(() =>
                {
                    lblServerStatus.Text = "Client send wrong data";
                });
                logView.Log("Client send wrong data", LogView.LogType.Error);
            }
        }

        private void btnInfo_Click(object sender, EventArgs e)
        {
            Info inf = new Info(this);
            inf.Show();
        }

        public void IncomingKey(string key)
        {
            try
            {
                customTextBox.ExecuteSafe(() =>
                {
                    customTextBox.Text = key;
                });
            }
            catch (Exception ex) { }
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

        private void testToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Bot.Instance.CONNECTED)
            {
                this.lblStatus.ExecuteSafe(() =>
                {
                    lblStatus.Text = "USB Connected, usb driver test...";
                    this.lblStatus.ForeColor = Color.Gray;
                });

                tested = false;
                TestDriver();
            }
            else
            {
                MessageBox.Show("USB is not connected...");
            }
        }

        private void btnCombination_Click(object sender, EventArgs e)
        {
            if (btnCombination.Tag != null)
            {
                if (btnCombination.Tag is Keyword keys)
                {
                    Bot.Instance.PressAndRelease(keys);
                }
            }
        }
    }
}
