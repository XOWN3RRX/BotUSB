using BotUSB.Device;
using BotUSB.Server;

namespace BotUSB.Tests;

public class LenientJsonTests
{
    private static ClientCommand Parse(string json)
    {
        Assert.True(LenientJson.TryParseCommand(json, out ClientCommand? command, out string? error), error);
        return command!;
    }

    [Fact]
    public void ParsesScriptStyleSingleQuotes()
    {
        ClientCommand command = Parse("{ 'Keys': [148] }");
        Assert.Equal([148], command.Keys!);
        Assert.Null(command.Message);
        Assert.False(command.Pressed);
    }

    [Fact]
    public void ParsesPressedAsString()
    {
        Assert.True(Parse("{ 'Keys': [111], 'Pressed': 'true'}").Pressed);
        Assert.False(Parse("{ 'Keys': [111], 'Pressed': false}").Pressed);
    }

    [Fact]
    public void ParsesDoubleQuotesAnyCaseAndNumbersAsStrings()
    {
        ClientCommand command = Parse("{\"keys\":[58,59],\"REPEAT\":\"3\"}");
        Assert.Equal([58, 59], command.Keys!);
        Assert.Equal(3, command.Repeat);
    }

    [Fact]
    public void ParsesUnquotedNamesAndKeyNames()
    {
        Assert.Equal([148, 58], Parse("{ Keys: ['_Esc', 'f1'] }").Keys!);
    }

    [Theory]
    [InlineData("{ 'Message': 'it's ok'}", "it's ok")]
    [InlineData("{ 'Message': 'a}b{c'}", "a}b{c")]
    [InlineData("{ 'Message': 'say \"hi\"' }", "say \"hi\"")]
    [InlineData("{ \"Message\": \"tab\\there \\u0041\" }", "tab\there A")]
    [InlineData("{ 'Message': '' }", "")]
    public void ParsesMessages(string json, string expected)
    {
        Assert.Equal(expected, Parse(json).Message);
    }

    [Fact]
    public void IgnoresUnknownProperties()
    {
        Assert.Equal([4], Parse("{ 'Other': { 'x': [1, 2] }, 'Keys': [4], 'More': null }").Keys!);
    }

    [Theory]
    [InlineData("{ 'Keys': [")]
    [InlineData("{ 'Keys': ['nope'] }")]
    [InlineData("{ 'Message': 'open }")]
    [InlineData("{ 'Repeat': 'x' }")]
    [InlineData("not json")]
    public void RejectsInvalidData(string json)
    {
        Assert.False(LenientJson.TryParseCommand(json, out _, out string? error));
        Assert.False(string.IsNullOrEmpty(error));
    }
}

public class CommandFramerTests
{
    private static List<string> Frame(params string[] chunks)
    {
        var framer = new CommandFramer();
        var objects = new List<string>();
        foreach (string chunk in chunks)
        {
            framer.Push(chunk, objects);
        }

        return objects;
    }

    [Fact]
    public void SplitsSeveralObjectsInOnePacket()
    {
        Assert.Equal(["{ 'Keys': [4]}", "{ 'Keys': [5]}"], Frame("{ 'Keys': [4]}{ 'Keys': [5]}"));
    }

    [Fact]
    public void JoinsAnObjectSplitAtEveryPosition()
    {
        const string json = "{ 'Message': 'it's {fine}', 'Keys': [1] }";
        for (int i = 1; i < json.Length; i++)
        {
            Assert.Equal([json], Frame(json[..i], json[i..]));
        }
    }

    [Fact]
    public void JoinsAnObjectSentOneCharacterAtATime()
    {
        const string json = "{ \"Message\": \"a \\\" } b\" }";
        Assert.Equal([json], Frame([.. json.Select(c => c.ToString())]));
    }

    [Fact]
    public void IgnoresGarbageBetweenObjects()
    {
        var framer = new CommandFramer();
        var objects = new List<string>();
        int ignored = framer.Push("xx{ 'Keys': [4]}\r\nyy", objects);
        Assert.Equal(["{ 'Keys': [4]}"], objects);
        Assert.Equal(4, ignored);
    }

    [Fact]
    public void IgnoresNulPaddingFromFixedBuffers()
    {
        var framer = new CommandFramer();
        var objects = new List<string>();
        int ignored = framer.Push("{ 'Keys': [4]}" + new string('\0', 242), objects);
        Assert.Equal(["{ 'Keys': [4]}"], objects);
        Assert.Equal(0, ignored);
    }

    [Fact]
    public void AcceptsAJsonArrayOfObjects()
    {
        Assert.Equal(["{'Keys':[4]}", "{'Keys':[5]}"], Frame("[{'Keys':[4]}, {'Keys':[5]}]"));
    }

    [Fact]
    public void DropsOversizedObjectsAndRecovers()
    {
        var framer = new CommandFramer();
        var objects = new List<string>();
        int ignored = framer.Push("{ 'Message': '" + new string('a', CommandFramer.MaxObjectLength) + "'}", objects);
        Assert.True(ignored > 0);
        Assert.Empty(objects);

        framer.Push("{ 'Keys': [4]}", objects);
        Assert.Equal(["{ 'Keys': [4]}"], objects);
    }
}

public class DispatchTests
{
    private static BotCommand Dispatch(ClientCommand command)
    {
        BotCommand? result = command.ToBotCommand(out string? error);
        Assert.True(result is not null, error);
        return result!;
    }

    [Fact]
    public void MessageWinsOverKeys()
    {
        BotCommand command = Dispatch(new ClientCommand([4], "hi", false, 0));
        Assert.Equal(BotAction.Type, command.Action);
        Assert.Equal("hi", command.Text);
    }

    [Fact]
    public void SingleKeyTapsOrHolds()
    {
        Assert.Equal(BotAction.Tap, Dispatch(new ClientCommand([58], null, false, 0)).Action);
        Assert.Equal(BotAction.Hold, Dispatch(new ClientCommand([111], null, true, 0)).Action);
    }

    [Theory]
    [InlineData(10, 10)]
    [InlineData(255, 255)]
    [InlineData(300, 254)]
    public void RepeatIsSentAsTheArgumentByte(int repeat, byte expected)
    {
        BotCommand command = Dispatch(new ClientCommand([207], null, false, repeat));
        Assert.Equal(BotAction.WithArgument, command.Action);
        Assert.Equal(expected, command.Argument);
    }

    [Fact]
    public void TwoOrThreeKeysAreCombos()
    {
        BotCommand command = Dispatch(new ClientCommand([100, 102, 183], null, false, 0));
        Assert.Equal(BotAction.Combo, command.Action);
        Assert.Equal([KeyCode._CtrlL, KeyCode._AltL, KeyCode._Delete], command.Keys);
    }

    [Theory]
    [InlineData(new int[0])]
    [InlineData(new[] { 1, 2, 3, 4 })]
    [InlineData(new[] { 211 })]
    [InlineData(new[] { -1 })]
    public void RejectsUnsupportedKeys(int[] keys)
    {
        Assert.Null(new ClientCommand(keys, null, false, 0).ToBotCommand(out string? error));
        Assert.NotNull(error);
    }

    [Fact]
    public void RejectsEmptyCommand()
    {
        Assert.Null(new ClientCommand(null, null, true, 0).ToBotCommand(out string? error));
        Assert.NotNull(error);
    }
}

public class KeyCodeTests
{
    [Fact]
    public void ProtocolNumbersMatchTheOriginalApp()
    {
        Assert.Equal(58, (int)KeyCode.F1);
        Assert.Equal(100, (int)KeyCode._CtrlL);
        Assert.Equal(111, (int)KeyCode._A);
        Assert.Equal(148, (int)KeyCode._Esc);
        Assert.Equal(165, (int)KeyCode._F1);
        Assert.Equal(207, (int)KeyCode._MouseXPlus);
        Assert.Equal(210, KeyCodes.Max);
        Assert.Equal(211, KeyCodes.All.Length);
    }

    [Theory]
    [InlineData("148", 148)]
    [InlineData(" _esc ", 148)]
    [InlineData("MouseLeft", 1)]
    public void ParsesNumbersAndNames(string text, int expected)
    {
        Assert.True(KeyCodes.TryParse(text, out KeyCode code));
        Assert.Equal(expected, (int)code);
    }

    [Theory]
    [InlineData("211")]
    [InlineData("-1")]
    [InlineData("Nope")]
    public void RejectsUnknownCodes(string text)
    {
        Assert.False(KeyCodes.TryParse(text, out _));
    }

    [Theory]
    [InlineData("NULL", "Release")]
    [InlineData("F1", "Tap")]
    [InlineData("NumpadPoint", "Tap")]
    [InlineData("_CtrlL", "Hold")]
    [InlineData("_NumpadPoint", "Hold")]
    [InlineData("_MouseYMinus", "MouseMove")]
    public void KnowsWhatEachCodeDoes(string code, string kind)
    {
        Assert.Equal(Enum.Parse<KeyKind>(kind), KeyCodes.KindOf(Enum.Parse<KeyCode>(code)));
    }

    [Theory]
    [InlineData("NULL", true)]
    [InlineData("MouseLeft", true)]
    [InlineData("F1", true)]
    [InlineData("NumpadPoint", true)]
    [InlineData("_CtrlL", false)]
    [InlineData("_Esc", false)]
    [InlineData("_MouseXPlus", false)]
    public void OnlyTapCodesSkipTheRelease(string code, bool releasesItself)
    {
        Assert.Equal(releasesItself, KeyCodes.ReleasesItself(Enum.Parse<KeyCode>(code)));
    }

    [Theory]
    [InlineData('a', "_A")]
    [InlineData('A', "_ShiftL, _A")]
    [InlineData('1', "_1")]
    [InlineData('0', "_0")]
    [InlineData(')', "_ShiftL, _0")]
    [InlineData(' ', "_Space")]
    public void MapsTypedCharacters(char c, string expected)
    {
        Assert.True(TextKeyMap.TryGetKeys(c, out KeyCode[] keys));
        Assert.Equal(expected, string.Join(", ", keys));
    }
}
