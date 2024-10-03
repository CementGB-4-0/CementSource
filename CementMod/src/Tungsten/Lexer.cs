﻿using System.Collections.Generic;
using System;
using Il2CppSystem.Data;

namespace Tungsten;

public enum TokenType
{
    // starts with a lowercase letter
    Identifier,
    // starts with a capital letter
    ClassIdentifier,
    Integer,
    Float,
    String,

    KeywordIf,
    KeywordElse,
    KeywordFor,
    KeywordWhile,
    KeywordReturn,
    KeywordFunc,
    KeywordVar,
    KeywordFalse,
    KeywordTrue,
    KeywordNull,
    KeywordNew,
    KeywordBreak,
    KeywordContinue,
    KeywordIn,

    OpenCurlyBracket,
    ClosedCurlyBracket,
    OpenSquareBracket,
    ClosedSquareBracket,
    OpenParenthesis,
    ClosedParenthesis,

    Equals,
    Greater,
    Less,
    LessEquals,
    GreaterEquals,
    DoubleEquals,
    BangEquals,

    Plus,
    Minus,
    Asterisk,
    ForwardSlash,
    Bang,

    SemiColon,
    Colon,
    Comma,

    End,
    Error
}

public struct Token
{
    public TokenType type;
    public string lexeme;

    public int lineIdx;
    public int charIdx;

    public Token(TokenType type, string lexeme="", int lineIdx = -1, int charIdx = -1)
    {
        this.type = type;
        this.lexeme = lexeme;
        this.lineIdx = lineIdx;
        this.charIdx = charIdx;
    }
}

public class Lexer
{
    private string _code;
    private int i = -1;

    private int lineIdx = 0;
    private int charIdx = -1;
    private char Current => _code[i];

    private Dictionary<string, TokenType> _keywords = new() {
        { "if", TokenType.KeywordIf },
        { "else", TokenType.KeywordElse },
        { "for", TokenType.KeywordFor },
        { "while", TokenType.KeywordWhile },
        { "return", TokenType.KeywordReturn },
        { "func", TokenType.KeywordFunc },
        { "var", TokenType.KeywordVar },
        { "true", TokenType.KeywordTrue },
        { "false", TokenType.KeywordFalse },
        { "null", TokenType.KeywordNull },
        { "new", TokenType.KeywordNew },
        { "break", TokenType.KeywordBreak },
        { "continue", TokenType.KeywordContinue },
        { "in", TokenType.KeywordIn }
    };

    private void Advance()
    {
        ++i;

        if (IsEnd)
        {
            errorManager.AppendAsLine();
            ++charIdx;
            return;
        }

        if (Current == '\n')
        {
            charIdx = -1;
            ++lineIdx;
            errorManager.AppendAsLine();
        }
        else
        {
            errorManager.AddChar(Current);
        }
        ++charIdx;
    }

    private ErrorManager errorManager;

    public Lexer(string code, ErrorManager errorManager=null)
    {
        _code = code;
        this.errorManager = errorManager == null ? new ErrorManager() : errorManager;
        Advance();
    }

    private void RemoveWhitespace()
    {
        while (!IsEnd && char.IsWhiteSpace(_code[i]))
            Advance();
    }

    private bool Match(char c)
    {
        if (Current == c)
        {
            Advance();
            return true;
        }

        return false;
    }

    private bool IsEnd => i >= _code.Length;
    
    private bool IsIdentifier(char c)
    {
        return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c == '_') || (c =='.') || IsDigit(c);
    }

    private bool IsDigit(char c)
    {
        return c >= '0' && c <= '9';
    }

    public Token Next()
    {
        RemoveWhitespace();
        if (IsEnd) return Token(TokenType.End);

        Token tok;
        bool gotTok = true;
        switch (Current)
        {
            case '+':
                tok = Token(TokenType.Plus);
                break;
            case '-':
                tok = Token(TokenType.Minus);
                break;
            case '*':
                tok = Token(TokenType.Asterisk);
                break;
            case '/':
                tok = Token(TokenType.ForwardSlash);
                break;
            case '(':
                tok = Token(TokenType.OpenParenthesis);
                break;
            case ')':
                tok = Token(TokenType.ClosedParenthesis);
                break;
            case '[':
                tok = Token(TokenType.OpenSquareBracket);
                break;
            case ']':
                tok = Token(TokenType.ClosedSquareBracket);
                break;
            case '{':
                tok = Token(TokenType.OpenCurlyBracket);
                break;
            case '}':
                tok = Token(TokenType.ClosedCurlyBracket);
                break;
            case '=':
                tok = Token(TokenType.Equals);
                Advance();
                if (Match('='))
                {
                    tok = Token(TokenType.DoubleEquals);
                    Advance();
                    return tok;
                }
                return tok;
            case '>':
                tok = Token(TokenType.Greater);
                Advance();
                if (Match('='))
                {
                    tok = Token(TokenType.GreaterEquals);
                    Advance();
                    return tok;
                }
                return tok;
            case '<':
                tok = Token(TokenType.Less);
                Advance();
                if (Match('='))
                {
                    tok = Token(TokenType.LessEquals);
                    Advance();
                    return tok;
                }
                return tok;
            case '!':
                tok = Token(TokenType.Bang);
                Advance();
                if (Match('='))
                {
                    tok = Token(TokenType.BangEquals);
                    Advance();
                    return tok;
                }
                return tok;
            case ',':
                tok = Token(TokenType.Comma);
                break;
            case ';':
                tok = Token(TokenType.SemiColon);
                break;
            case ':':
                tok = Token(TokenType.Colon);
                break;
            case '"':
                return LexString();
            default:
                if (IsDigit(Current))
                    return LexNum();
                if (IsIdentifier(Current))
                    return LexId();

                tok = Token(TokenType.Error);
                Error($"Unexpected character '{Current}'");
                break;
        }

        Advance();
        return tok;
    }

    private Token LexNum()
    {
        int line = this.lineIdx;
        int charIdx = this.charIdx;

        string num = "";
        while (!IsEnd && IsDigit(Current))
        {
            num += Current;
            Advance();
        }

        bool isInt = true;
        if (!IsEnd && Current == '.')
        {
            isInt = false;
            Advance();
            num += '.';
            bool raiseError = true;
            while (!IsEnd && IsDigit(Current))
            {
                num += Current;
                Advance();
                raiseError = false;
            }

            if (raiseError)
            {
;               Error("There must be at least one digit after the decimal point.");
            }
        }

        return Token(isInt ? TokenType.Integer : TokenType.Float, num, line, charIdx);
    }

    private Token LexString()
    {
        int line = this.lineIdx;
        int charIdx = this.charIdx;

        Advance(); // advance past "
        if (IsEnd)
        {
            Error("Expected contents of string after '\"', but got nothing.");
            return Token(TokenType.String, "", line, charIdx);
        }

        string s = "";
        while (Current != '"')
        {
            if (Current == '\n')
            {
                Error("Expected '\"' to close string; strings can't span several lines." + 
                    "If you want to include a newline in your string use '\\n'");
                break;
            }
            if (Current == '\\')
            {
                Advance();
                if (IsEnd)
                {
                    Error("Expected character after '\\'.");
                    return Token(TokenType.String, s, line, charIdx);
                }
                switch (Current)
                {
                    case '\\':
                        s += '\\';
                        break;
                    case 'n':
                        s += '\n';
                        break;
                    case 'r':
                        s += '\r';
                        break;
                    case '"':
                        s += '"';
                        break;
                    case 't':
                        s += '\t';
                        break;
                    default:
                        Error("Invalid backspaced character. If you wanted to type a backslash use '\\\\'.");
                        break;
                }
            }
            else
            {
                s += Current;
            }
            Advance();
            if (IsEnd)
            {
                Error("Expected '\"' at the end of the string.");
                break;
            }
        }
        if (Current == '"') Advance();
        return Token(TokenType.String, s, line, charIdx);
    }

    private Token LexId()
    {
        int line = this.lineIdx;
        int charIdx = this.charIdx;

        string id = "";
        while (!IsEnd && IsIdentifier(Current))
        {
            id += Current;
            Advance();
        }

        if (_keywords.ContainsKey(id))
            return Token(_keywords[id], lineIdx: line, charIdx: charIdx);

        return Token(char.IsUpper(id[0]) ? TokenType.ClassIdentifier : TokenType.Identifier, id, line, charIdx);
    }

    private Token Token(TokenType type, string lexeme = "", int lineIdx = -1, int charIdx = -1)
    {
        return new Token(type, lexeme, lineIdx == -1 ? this.lineIdx : lineIdx, charIdx == -1 ? this.charIdx : charIdx);
    }

    private void Error(string message)
    {
        errorManager.RaiseSyntaxError(lineIdx, charIdx, message);
    }
}
