﻿namespace AutoBot_v1._Bot._Keys
{
    public class KeywordParser
    {
        public static readonly Keyword[] Keys = new Keyword[]
        {
            new Keyword(' ', _Keys.KeyBotEnum._Space),
            new Keyword('a', _Keys.KeyBotEnum._A),
            new Keyword('b', _Keys.KeyBotEnum._B),
            new Keyword('c', _Keys.KeyBotEnum._C),
            new Keyword('d', _Keys.KeyBotEnum._D),
            new Keyword('e', _Keys.KeyBotEnum._E),
            new Keyword('f', _Keys.KeyBotEnum._F),
            new Keyword('g', _Keys.KeyBotEnum._G),
            new Keyword('h', _Keys.KeyBotEnum._H),
            new Keyword('i', _Keys.KeyBotEnum._I),
            new Keyword('j', _Keys.KeyBotEnum._J),
            new Keyword('k', _Keys.KeyBotEnum._K),
            new Keyword('l', _Keys.KeyBotEnum._L),
            new Keyword('m', _Keys.KeyBotEnum._M),
            new Keyword('n', _Keys.KeyBotEnum._N),
            new Keyword('o', _Keys.KeyBotEnum._O),
            new Keyword('p', _Keys.KeyBotEnum._P),
            new Keyword('q', _Keys.KeyBotEnum._Q),
            new Keyword('r', _Keys.KeyBotEnum._R),
            new Keyword('s', _Keys.KeyBotEnum._S),
            new Keyword('t', _Keys.KeyBotEnum._T),
            new Keyword('u', _Keys.KeyBotEnum._U),
            new Keyword('v', _Keys.KeyBotEnum._V),
            new Keyword('w', _Keys.KeyBotEnum._W),
            new Keyword('x', _Keys.KeyBotEnum._X),
            new Keyword('y', _Keys.KeyBotEnum._Y),
            new Keyword('z', _Keys.KeyBotEnum._Z),
            new Keyword('A', _Keys.KeyBotEnum._ShiftL, _Keys.KeyBotEnum._A),
            new Keyword('B', _Keys.KeyBotEnum._ShiftL, _Keys.KeyBotEnum._B),
            new Keyword('C', _Keys.KeyBotEnum._ShiftL, _Keys.KeyBotEnum._C),
            new Keyword('D', _Keys.KeyBotEnum._ShiftL, _Keys.KeyBotEnum._D),
            new Keyword('E', _Keys.KeyBotEnum._ShiftL, _Keys.KeyBotEnum._E),
            new Keyword('F', _Keys.KeyBotEnum._ShiftL, _Keys.KeyBotEnum._F),
            new Keyword('G', _Keys.KeyBotEnum._ShiftL, _Keys.KeyBotEnum._G),
            new Keyword('H', _Keys.KeyBotEnum._ShiftL, _Keys.KeyBotEnum._H),
            new Keyword('I', _Keys.KeyBotEnum._ShiftL, _Keys.KeyBotEnum._I),
            new Keyword('J', _Keys.KeyBotEnum._ShiftL, _Keys.KeyBotEnum._J),
            new Keyword('K', _Keys.KeyBotEnum._ShiftL, _Keys.KeyBotEnum._K),
            new Keyword('L', _Keys.KeyBotEnum._ShiftL, _Keys.KeyBotEnum._L),
            new Keyword('M', _Keys.KeyBotEnum._ShiftL, _Keys.KeyBotEnum._M),
            new Keyword('N', _Keys.KeyBotEnum._ShiftL, _Keys.KeyBotEnum._N),
            new Keyword('O', _Keys.KeyBotEnum._ShiftL, _Keys.KeyBotEnum._O),
            new Keyword('P', _Keys.KeyBotEnum._ShiftL, _Keys.KeyBotEnum._P),
            new Keyword('Q', _Keys.KeyBotEnum._ShiftL, _Keys.KeyBotEnum._Q),
            new Keyword('R', _Keys.KeyBotEnum._ShiftL, _Keys.KeyBotEnum._R),
            new Keyword('S', _Keys.KeyBotEnum._ShiftL, _Keys.KeyBotEnum._S),
            new Keyword('T', _Keys.KeyBotEnum._ShiftL, _Keys.KeyBotEnum._T),
            new Keyword('U', _Keys.KeyBotEnum._ShiftL, _Keys.KeyBotEnum._U),
            new Keyword('V', _Keys.KeyBotEnum._ShiftL, _Keys.KeyBotEnum._V),
            new Keyword('W', _Keys.KeyBotEnum._ShiftL, _Keys.KeyBotEnum._W),
            new Keyword('X', _Keys.KeyBotEnum._ShiftL, _Keys.KeyBotEnum._X),
            new Keyword('Y', _Keys.KeyBotEnum._ShiftL, _Keys.KeyBotEnum._Y),
            new Keyword('Z', _Keys.KeyBotEnum._ShiftL, _Keys.KeyBotEnum._Z),
            new Keyword('0', _Keys.KeyBotEnum._0),
            new Keyword('1', _Keys.KeyBotEnum._1),
            new Keyword('2', _Keys.KeyBotEnum._2),
            new Keyword('3', _Keys.KeyBotEnum._3),
            new Keyword('4', _Keys.KeyBotEnum._4),
            new Keyword('5', _Keys.KeyBotEnum._5),
            new Keyword('6', _Keys.KeyBotEnum._6),
            new Keyword('7', _Keys.KeyBotEnum._7),
            new Keyword('8', _Keys.KeyBotEnum._8),
            new Keyword('9', _Keys.KeyBotEnum._9),
            new Keyword('-', _Keys.KeyBotEnum._Minus),
            new Keyword('=', _Keys.KeyBotEnum._Equal),
            new Keyword('[', _Keys.KeyBotEnum._OpenBracket),
            new Keyword(']', _Keys.KeyBotEnum._CloseBracket),
            new Keyword('\\', _Keys.KeyBotEnum._BackSlash1),
            new Keyword(';', _Keys.KeyBotEnum._Semicolon),
            new Keyword('\'', _Keys.KeyBotEnum._SingleQuote),
            new Keyword('`', _Keys.KeyBotEnum._SingleQuoteMark),
            new Keyword(',', _Keys.KeyBotEnum._Comma),
            new Keyword('/', _Keys.KeyBotEnum._Slash),
            new Keyword('.', _Keys.KeyBotEnum._Point),
            new Keyword(':', _Keys.KeyBotEnum._ShiftL, _Keys.KeyBotEnum._Semicolon),
            new Keyword('\"', _Keys.KeyBotEnum._ShiftL, _Keys.KeyBotEnum._SingleQuote),
            new Keyword('{', _Keys.KeyBotEnum._ShiftL, _Keys.KeyBotEnum._OpenBracket),
            new Keyword('}', _Keys.KeyBotEnum._ShiftL, _Keys.KeyBotEnum._CloseBracket),
            new Keyword('?', _Keys.KeyBotEnum._ShiftL, _Keys.KeyBotEnum._Slash),
            new Keyword('!', _Keys.KeyBotEnum._ShiftL, _Keys.KeyBotEnum._1),
            new Keyword('@', _Keys.KeyBotEnum._ShiftL, _Keys.KeyBotEnum._2),
            new Keyword('#', _Keys.KeyBotEnum._ShiftL, _Keys.KeyBotEnum._3),
            new Keyword('$', _Keys.KeyBotEnum._ShiftL, _Keys.KeyBotEnum._4),
            new Keyword('%', _Keys.KeyBotEnum._ShiftL, _Keys.KeyBotEnum._5),
            new Keyword('^', _Keys.KeyBotEnum._ShiftL, _Keys.KeyBotEnum._6),
            new Keyword('&', _Keys.KeyBotEnum._ShiftL, _Keys.KeyBotEnum._7),
            new Keyword('*', _Keys.KeyBotEnum._ShiftL, _Keys.KeyBotEnum._8),
            new Keyword('(', _Keys.KeyBotEnum._ShiftL, _Keys.KeyBotEnum._9),
            new Keyword(')', _Keys.KeyBotEnum._ShiftL, _Keys.KeyBotEnum._0)
        };
    }
}