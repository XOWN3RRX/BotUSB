using System;
using System.Threading;
using AutoBot_v1._Bot._Keys;

namespace AutoBot_v1._Bot
{
    public class Bot
    {
        private static Bot _instance;
        private static object _locker = new object();

        private byte[] _bufferOut;

        public static Bot Instance
        {
            get
            {
                lock (_locker)
                {
                    if (_instance == null)
                    {
                        _instance = new Bot();
                    }

                    return _instance;
                }
            }
        }

        public bool CONNECTED { get; set; } = false;

        private Bot()
        {
            _bufferOut = new byte[66];
            _bufferOut[3] = 0;
            _bufferOut[1] = 0;
            _bufferOut[0] = 3;
        }

        /// <summary>
        /// BufferOut[2] = 0; // Keycode, tasta
        /// <returns></returns>
        /// Alt 1
        /// 3, 0, 43, 0
        /// 3, 0, 102, 0
        public void Press(BotKeyEnum key)
        {
            if (this.CONNECTED)
            {
                _bufferOut[2] = Convert.ToByte((int)key);
                HIDDLLInterface.hidWriteEx(5638, 6536, ref _bufferOut[0]);
            }
        }

        public void PressAndRelease(BotKeyEnum key)
        {
            if (this.CONNECTED)
            {
                _bufferOut[2] = Convert.ToByte((int)key);
                HIDDLLInterface.hidWriteEx(5638, 6536, ref _bufferOut[0]);
                Thread.Sleep(10);
                _bufferOut[2] = Convert.ToByte((int)BotKeyEnum.NULL);
                HIDDLLInterface.hidWriteEx(5638, 6536, ref _bufferOut[0]);
            }
        }

        public void ReleaseAll()
        {
            if (this.CONNECTED)
            {
                _bufferOut[2] = Convert.ToByte((int)BotKeyEnum.NULL);
                HIDDLLInterface.hidWriteEx(5638, 6536, ref _bufferOut[0]);
            }
        }

        public void PressAndRelease(BotKeyEnum key1, BotKeyEnum key2)
        {
            if (this.CONNECTED)
            {
                _bufferOut[2] = Convert.ToByte((int)key1);
                HIDDLLInterface.hidWriteEx(5638, 6536, ref _bufferOut[0]);
                Thread.Sleep(10);
                _bufferOut[2] = Convert.ToByte((int)key2);
                HIDDLLInterface.hidWriteEx(5638, 6536, ref _bufferOut[0]);
                Thread.Sleep(10);
                _bufferOut[2] = Convert.ToByte(BotKeyEnum.NULL);
                HIDDLLInterface.hidWriteEx(5638, 6536, ref _bufferOut[0]);
            }
        }
    }
}
