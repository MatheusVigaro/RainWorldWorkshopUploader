using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

public static class Json
{
    public static object Deserialize(string json)
    {
        if (json == null)
        {
            return null;
        }
        return Json.Parser.Parse(json);
    }

    public static string Serialize(object obj)
    {
        return Json.Serializer.Serialize(obj);
    }

    private sealed class Parser : IDisposable
    {
        private Parser(string jsonString)
        {
            this.json = new StringReader(jsonString);
        }

        public static object Parse(string jsonString)
        {
            object result;
            using (Json.Parser parser = new Json.Parser(jsonString))
            {
                result = parser.ParseValue();
            }
            return result;
        }

        public void Dispose()
        {
            this.json.Dispose();
            this.json = null;
        }

        private Dictionary<string, object> ParseObject()
        {
            Dictionary<string, object> dictionary = new Dictionary<string, object>();
            this.json.Read();
            for (; ; )
            {
                Json.Parser.TOKEN nextToken = this.NextToken;
                if (nextToken == Json.Parser.TOKEN.NONE)
                {
                    break;
                }
                if (nextToken == Json.Parser.TOKEN.CURLY_CLOSE)
                {
                    return dictionary;
                }
                if (nextToken != Json.Parser.TOKEN.COMMA)
                {
                    string text = this.ParseString();
                    if (text == null)
                    {
                        goto Block_4;
                    }
                    if (this.NextToken != Json.Parser.TOKEN.COLON)
                    {
                        goto Block_5;
                    }
                    this.json.Read();
                    dictionary[text] = this.ParseValue();
                }
            }
            return null;
        Block_4:
            return null;
        Block_5:
            return null;
        }

        private List<object> ParseArray()
        {
            List<object> list = new List<object>();
            this.json.Read();
            bool flag = true;
            while (flag)
            {
                Json.Parser.TOKEN nextToken = this.NextToken;
                if (nextToken == Json.Parser.TOKEN.NONE)
                {
                    return null;
                }
                if (nextToken != Json.Parser.TOKEN.SQUARED_CLOSE)
                {
                    if (nextToken != Json.Parser.TOKEN.COMMA)
                    {
                        object item = this.ParseByToken(nextToken);
                        list.Add(item);
                    }
                }
                else
                {
                    flag = false;
                }
            }
            return list;
        }

        private object ParseValue()
        {
            Json.Parser.TOKEN nextToken = this.NextToken;
            return this.ParseByToken(nextToken);
        }

        private object ParseByToken(Json.Parser.TOKEN token)
        {
            switch (token)
            {
                case Json.Parser.TOKEN.CURLY_OPEN:
                    return this.ParseObject();
                case Json.Parser.TOKEN.SQUARED_OPEN:
                    return this.ParseArray();
                case Json.Parser.TOKEN.STRING:
                    return this.ParseString();
                case Json.Parser.TOKEN.NUMBER:
                    return this.ParseNumber();
                case Json.Parser.TOKEN.TRUE:
                    return true;
                case Json.Parser.TOKEN.FALSE:
                    return false;
                case Json.Parser.TOKEN.NULL:
                    return null;
            }
            return null;
        }

        private string ParseString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            this.json.Read();
            bool flag = true;
            while (flag)
            {
                if (this.json.Peek() == -1)
                {
                    break;
                }
                char nextChar = this.NextChar;
                if (nextChar != '"')
                {
                    if (nextChar != '\\')
                    {
                        stringBuilder.Append(nextChar);
                    }
                    else if (this.json.Peek() == -1)
                    {
                        flag = false;
                    }
                    else
                    {
                        nextChar = this.NextChar;
                        if (nextChar <= '\\')
                        {
                            if (nextChar == '"' || nextChar == '/' || nextChar == '\\')
                            {
                                stringBuilder.Append(nextChar);
                            }
                        }
                        else if (nextChar <= 'f')
                        {
                            if (nextChar != 'b')
                            {
                                if (nextChar == 'f')
                                {
                                    stringBuilder.Append('\f');
                                }
                            }
                            else
                            {
                                stringBuilder.Append('\b');
                            }
                        }
                        else if (nextChar != 'n')
                        {
                            switch (nextChar)
                            {
                                case 'r':
                                    stringBuilder.Append('\r');
                                    break;
                                case 't':
                                    stringBuilder.Append('\t');
                                    break;
                                case 'u':
                                    {
                                        StringBuilder stringBuilder2 = new StringBuilder();
                                        for (int i = 0; i < 4; i++)
                                        {
                                            stringBuilder2.Append(this.NextChar);
                                        }
                                        stringBuilder.Append((char)Convert.ToInt32(stringBuilder2.ToString(), 16));
                                        break;
                                    }
                            }
                        }
                        else
                        {
                            stringBuilder.Append('\n');
                        }
                    }
                }
                else
                {
                    flag = false;
                }
            }
            return stringBuilder.ToString();
        }

        private object ParseNumber()
        {
            string nextWord = this.NextWord;
            if (nextWord.IndexOf('.') == -1)
            {
                long result;
                long.TryParse(nextWord, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
                return result;
            }
            double result2;
            double.TryParse(nextWord, NumberStyles.Any, CultureInfo.InvariantCulture, out result2);
            return result2;
        }

        private void EatWhitespace()
        {
            while (" \t\n\r".IndexOf(this.PeekChar) != -1)
            {
                this.json.Read();
                if (this.json.Peek() == -1)
                {
                    break;
                }
            }
        }

        // (get) Token: 0x06003197 RID: 12695 RVA: 0x003B49EB File Offset: 0x003B2BEB
        private char PeekChar
        {
            get
            {
                return Convert.ToChar(this.json.Peek());
            }
        }

        // (get) Token: 0x06003198 RID: 12696 RVA: 0x003B49FD File Offset: 0x003B2BFD
        private char NextChar
        {
            get
            {
                return Convert.ToChar(this.json.Read());
            }
        }

        // (get) Token: 0x06003199 RID: 12697 RVA: 0x003B4A10 File Offset: 0x003B2C10
        private string NextWord
        {
            get
            {
                StringBuilder stringBuilder = new StringBuilder();
                while (" \t\n\r{}[],:\"".IndexOf(this.PeekChar) == -1)
                {
                    stringBuilder.Append(this.NextChar);
                    if (this.json.Peek() == -1)
                    {
                        break;
                    }
                }
                return stringBuilder.ToString();
            }
        }

        // (get) Token: 0x0600319A RID: 12698 RVA: 0x003B4A5C File Offset: 0x003B2C5C
        private Json.Parser.TOKEN NextToken
        {
            get
            {
                this.EatWhitespace();
                if (this.json.Peek() == -1)
                {
                    return Json.Parser.TOKEN.NONE;
                }
                char peekChar = this.PeekChar;
                if (peekChar <= '[')
                {
                    switch (peekChar)
                    {
                        case '"':
                            return Json.Parser.TOKEN.STRING;
                        case '#':
                        case '$':
                        case '%':
                        case '&':
                        case '\'':
                        case '(':
                        case ')':
                        case '*':
                        case '+':
                        case '.':
                        case '/':
                            break;
                        case ',':
                            this.json.Read();
                            return Json.Parser.TOKEN.COMMA;
                        case '-':
                        case '0':
                        case '1':
                        case '2':
                        case '3':
                        case '4':
                        case '5':
                        case '6':
                        case '7':
                        case '8':
                        case '9':
                            return Json.Parser.TOKEN.NUMBER;
                        case ':':
                            return Json.Parser.TOKEN.COLON;
                        default:
                            if (peekChar == '[')
                            {
                                return Json.Parser.TOKEN.SQUARED_OPEN;
                            }
                            break;
                    }
                }
                else
                {
                    if (peekChar == ']')
                    {
                        this.json.Read();
                        return Json.Parser.TOKEN.SQUARED_CLOSE;
                    }
                    if (peekChar == '{')
                    {
                        return Json.Parser.TOKEN.CURLY_OPEN;
                    }
                    if (peekChar == '}')
                    {
                        this.json.Read();
                        return Json.Parser.TOKEN.CURLY_CLOSE;
                    }
                }
                string nextWord = this.NextWord;
                if (nextWord != null)
                {
                    if (nextWord == "false")
                    {
                        return Json.Parser.TOKEN.FALSE;
                    }
                    if (nextWord == "true")
                    {
                        return Json.Parser.TOKEN.TRUE;
                    }
                    if (nextWord == "null")
                    {
                        return Json.Parser.TOKEN.NULL;
                    }
                }
                return Json.Parser.TOKEN.NONE;
            }
        }

        private const string WHITE_SPACE = " \t\n\r";

        private const string WORD_BREAK = " \t\n\r{}[],:\"";

        private StringReader json;

        private enum TOKEN
        {
            NONE,
            CURLY_OPEN,
            CURLY_CLOSE,
            SQUARED_OPEN,
            SQUARED_CLOSE,
            COLON,
            COMMA,
            STRING,
            NUMBER,
            TRUE,
            FALSE,
            NULL
        }
    }

    private sealed class Serializer
    {
        private Serializer()
        {
            this.builder = new StringBuilder();
        }

        public static string Serialize(object obj)
        {
            Json.Serializer serializer = new Json.Serializer();
            serializer.SerializeValue(obj);
            return serializer.builder.ToString();
        }

        private void SerializeValue(object value)
        {
            if (value == null)
            {
                this.builder.Append("null");
                return;
            }
            string str;
            if ((str = (value as string)) != null)
            {
                this.SerializeString(str);
                return;
            }
            if (value is bool)
            {
                this.builder.Append(value.ToString().ToLower());
                return;
            }
            IList anArray;
            if ((anArray = (value as IList)) != null)
            {
                this.SerializeArray(anArray);
                return;
            }
            IDictionary obj;
            if ((obj = (value as IDictionary)) != null)
            {
                this.SerializeObject(obj);
                return;
            }
            if (value is char)
            {
                this.SerializeString(value.ToString());
                return;
            }
            this.SerializeOther(value);
        }

        private void SerializeObject(IDictionary obj)
        {
            bool flag = true;
            this.builder.Append('{');
            foreach (object key in obj.Keys)
            {
                if (!flag)
                {
                    this.builder.Append(',');
                }
                this.SerializeString(key.ToString());
                this.builder.Append(':');
                this.SerializeValue(obj[key]);
                flag = false;
            }
            this.builder.Append('}');
        }

        private void SerializeArray(IList anArray)
        {
            this.builder.Append('[');
            bool flag = true;
            foreach (object item in anArray)
            {
                if (!flag)
                {
                    this.builder.Append(',');
                }
                this.SerializeValue(item);
                flag = false;
            }
            this.builder.Append(']');
        }

        private void SerializeString(string str)
        {
            this.builder.Append('"');
            char[] array = str.ToCharArray();
            int i = 0;
            while (i < array.Length)
            {
                char c = array[i];
                switch (c)
                {
                    case '\b':
                        this.builder.Append("\\b");
                        break;
                    case '\t':
                        this.builder.Append("\\t");
                        break;
                    case '\n':
                        this.builder.Append("\\n");
                        break;
                    case '\v':
                        goto IL_DD;
                    case '\f':
                        this.builder.Append("\\f");
                        break;
                    case '\r':
                        this.builder.Append("\\r");
                        break;
                    default:
                        if (c != '"')
                        {
                            if (c != '\\')
                            {
                                goto IL_DD;
                            }
                            this.builder.Append("\\\\");
                        }
                        else
                        {
                            this.builder.Append("\\\"");
                        }
                        break;
                }
            IL_123:
                i++;
                continue;
            IL_DD:
                int num = Convert.ToInt32(c);
                if (num >= 32 && num <= 126)
                {
                    this.builder.Append(c);
                    goto IL_123;
                }
                this.builder.Append("\\u" + Convert.ToString(num, 16).PadLeft(4, '0'));
                goto IL_123;
            }
            this.builder.Append('"');
        }

        private void SerializeOther(object value)
        {
            if (value is int || value is uint || value is long || value is sbyte || value is byte || value is short || value is ushort || value is ulong)
            {
                this.builder.Append(value.ToString());
                return;
            }
            if (value is float || value is double || value is decimal)
            {
                this.builder.Append(string.Format("{0:0.000}", value));
                return;
            }
            this.SerializeString(value.ToString());
        }

        private StringBuilder builder;
    }
}
