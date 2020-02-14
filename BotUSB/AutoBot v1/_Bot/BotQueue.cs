using AutoBot_v1._Bot._JSON;
using AutoBot_v1._CustomControls;
using System.Collections.Concurrent;
using System.Threading;

namespace AutoBot_v1._Bot
{
    public class BotQueue
    {
        private Thread thread;
        private static object locker = new object();
        private ClientData data;

        public ConcurrentQueue<ClientData> Queue;

        public delegate void OnErrorOccuredDelegate(string message, LogView.LogType logType);
        public event OnErrorOccuredDelegate OnErrorOccured;

        private bool finish = false;

        public bool Finish
        {
            get
            {
                {
                    return finish;
                }
            }
            set
            {
                lock(locker)
                {
                    this.finish = value;
                }
            }
        }

        public BotQueue()
        {
            Queue = new ConcurrentQueue<ClientData>();
            thread = new Thread(Start);
            thread.Start();
        }

        public void Start()
        {
            while(!Finish)
            {
                if(Queue.Count > 0)
                {
                    if(Queue.TryDequeue(out data))
                    {
                        if (data.Message != null)
                        {
                            Bot.Instance.PressMessage(data.Message);
                        }
                        else if (data.Keys.Length == 1)
                        {
                            Bot.Instance.PressAndRelease(data.Keys[0]);
                        }
                        else if (data.Keys.Length == 2)
                        {
                            Bot.Instance.PressAndRelease(data.Keys[0], data.Keys[1]);
                        }
                        else
                        {
                            OnErrorOccured?.Invoke("UNDEFINED DATA", LogView.LogType.Error);
                        }
                    }
                }

                Thread.Sleep(10);
            }
        }

        public void Stop()
        {
            this.Finish = true;
        }
    }
}
