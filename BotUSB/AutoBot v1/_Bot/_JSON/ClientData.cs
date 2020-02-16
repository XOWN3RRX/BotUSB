using AutoBot_v1._Bot._Keys;

namespace AutoBot_v1._Bot._JSON
{
    public class ClientData
    {
        public KeyBotEnum[] Keys { get; set; }
        public string Message { get; set; }
        public bool Pressed { get; set; } = false;
    }
}
