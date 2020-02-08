using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace AutoBot_v1._Bot._TCPListener
{
    public class TCPThread
    {
        public event EventHandler OnChangeStatus;
        private TcpListener _server;
        private Thread _runningThread;

        private string _localAddress = "127.0.0.1";
        private int _port = 13000;

        public Action<string> OnTriggerServerAction { get; set; }

        public TCPThread()
        {
            _runningThread = new Thread(StartServer);
        }

        public TCPThread(string localAddress, int port)
        {
            this._localAddress = localAddress;
            this._port = port;

            _runningThread = new Thread(StartServer);
        }

        public void Run()
        {
            _runningThread.Start();
        }

        private void StartServer()
        {
            try
            {
                IPAddress localAddr = IPAddress.Parse(_localAddress);

                _server = new TcpListener(localAddr, _port);

                _server.Start();

                Byte[] bytes = new Byte[256];
                String data = null;

                while (true)
                {
                    OnChangeStatus?.Invoke("Server wait connection...", EventArgs.Empty);

                    TcpClient client = _server.AcceptTcpClient();

                    OnChangeStatus?.Invoke("Client Connected", EventArgs.Empty);

                    NetworkStream stream = client.GetStream();

                    data = null;
                    int i;
                    try
                    {
                        while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                        {
                            data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);

                            OnTriggerServerAction?.Invoke(data);
                        }
                    }
                    catch
                    {
                        OnChangeStatus?.Invoke("Server connection closed...", EventArgs.Empty);
                    }

                    client.Close();
                }
            }
            catch (SocketException e)
            {
                OnChangeStatus?.Invoke(string.Format("SocketException : {0}", e), EventArgs.Empty);
            }
            finally
            {
                try
                {
                    _server.Stop();
                }
                catch (Exception ex)
                {
                    OnChangeStatus?.Invoke(string.Format("FATAL EXCEPTION : {0}", ex), EventArgs.Empty);
                }
            }
        }

        public void StopServer()
        {
            try
            {
                _server.Stop();
                _runningThread.Abort();
            }
            catch { }
        }
    }
}
