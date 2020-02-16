using System;
using System.Linq;
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

        public void Press(KeyBotEnum key)
        {
            if (this.CONNECTED)
            {
                _bufferOut[2] = Convert.ToByte((int)key);
                HIDDLLInterface.hidWriteEx(5638, 6536, ref _bufferOut[0]);
            }
        }

        public void PressAndRelease(KeyBotEnum key)
        {
            if (this.CONNECTED)
            {
                _bufferOut[2] = Convert.ToByte((int)key);
                HIDDLLInterface.hidWriteEx(5638, 6536, ref _bufferOut[0]);
                _bufferOut[2] = Convert.ToByte((int)KeyBotEnum.NULL);
                HIDDLLInterface.hidWriteEx(5638, 6536, ref _bufferOut[0]);
            }
        }

        public void ReleaseAll()
        {
            if (this.CONNECTED)
            {
                _bufferOut[2] = Convert.ToByte((int)KeyBotEnum.NULL);
                HIDDLLInterface.hidWriteEx(5638, 6536, ref _bufferOut[0]);
            }
        }

        public void PressAndRelease(KeyBotEnum key1, KeyBotEnum key2)
        {
            if (this.CONNECTED)
            {
                _bufferOut[2] = Convert.ToByte((int)key1);
                HIDDLLInterface.hidWriteEx(5638, 6536, ref _bufferOut[0]);
                _bufferOut[2] = Convert.ToByte((int)key2);
                HIDDLLInterface.hidWriteEx(5638, 6536, ref _bufferOut[0]);
                _bufferOut[2] = Convert.ToByte(KeyBotEnum.NULL);
                HIDDLLInterface.hidWriteEx(5638, 6536, ref _bufferOut[0]);
            }
        }

        public void PressMessage(string message)
        {
            if (message.Length == 0)
            {
                return;
            }

            if (this.CONNECTED)
            {
                foreach (char item in message)
                {
                    Keyword key = KeywordParser.Keys.Where(x => x.Character == item).FirstOrDefault();

                    if (key != null)
                    {
                        if (key.Keys.Length == 1)
                        {
                            Bot.Instance.PressAndRelease(key.Keys[0]);
                        }
                        else if (key.Keys.Length == 2)
                        {
                            Bot.Instance.PressAndRelease(key.Keys[0], key.Keys[1]);
                        }
                    }
                }
            }
        }
    }
}
