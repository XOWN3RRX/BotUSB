using AutoBot_v1._Bot._Keys;
using System.Linq;

namespace AutoBot_v1._Bot._JSON
{
    public class ClientData
    {
        public KeyBotEnum[] Keys { get; set; }
        public string Message { get; set; }
        public bool Pressed { get; set; } = false;
        public int Repeat { get; set; } = 0;

        public override string ToString()
        {
            if (Keys.Length > 0)
            {
                return Keys.Aggregate("Keys :", (current, next) => current + " " + next);
            }
            return "ClientData";
        }
    }
}
