namespace AutoBot_v1._Bot._Keys
{
    public class Keyword
    {
        public char Character { get; private set; }
        public KeyBotEnum[] Keys;

        private Keyword() { }

        public Keyword(char chr, KeyBotEnum key1)
        {
            this.Character = chr;
            this.Keys = new KeyBotEnum[] { key1 };
        }

        public Keyword(char chr, KeyBotEnum key1, KeyBotEnum key2)
        {
            this.Character = chr;
            this.Keys = new KeyBotEnum[] { key1, key2 };
        }
    }
}
