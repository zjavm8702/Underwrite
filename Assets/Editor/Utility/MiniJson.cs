using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Underwrite.Editor.Utility
{
    /// <summary>
    /// 작은 JSON 읽개. 객체는 Dictionary, 배열은 List, 숫자는 double로 돌려준다.
    ///
    /// JsonUtility는 [[x, y]] 같은 배열의 배열과 키가 정해지지 않은 객체를 읽지 못한다.
    /// Codex가 내는 배치표가 그 모양이다. Newtonsoft는 다른 패키지의 의존성으로만 들어와 있어
    /// 그 패키지가 빠지면 함께 사라지므로 기대지 않는다.
    /// </summary>
    public static class MiniJson
    {
        /// <summary>문자열 전체를 읽는다. 형식이 틀리면 위치를 담은 FormatException을 던진다.</summary>
        public static object Parse(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            var reader = new Reader(text);
            var value = reader.ReadValue();
            reader.SkipWhitespace();

            if (reader.AtEnd == false)
            {
                throw reader.Fail("Unexpected trailing characters.");
            }

            return value;
        }

        /// <summary>
        /// Parse가 돌려주는 모양(Dictionary·List·string·double·bool·null)을 사람이 읽을 수 있게 들여 써서 적는다.
        /// 한글은 그대로 둔다(음성·대사 편집 창이 대사 표를 저장할 때 쓴다).
        /// </summary>
        public static string Serialize(object value)
        {
            var builder = new StringBuilder();
            Write(builder, value, 0);
            builder.Append('\n');
            return builder.ToString();
        }

        private static void Write(StringBuilder builder, object value, int depth)
        {
            switch (value)
            {
                case null:
                {
                    builder.Append("null");
                    return;
                }
                case string text:
                {
                    WriteString(builder, text);
                    return;
                }
                case bool flag:
                {
                    builder.Append(flag == true ? "true" : "false");
                    return;
                }
                case double number:
                {
                    builder.Append(number.ToString("R", CultureInfo.InvariantCulture));
                    return;
                }
                case Dictionary<string, object> map:
                {
                    builder.Append('{');
                    var first = true;

                    foreach (var pair in map)
                    {
                        builder.Append(first == true ? "\n" : ",\n");
                        builder.Append(' ', (depth + 1) * 2);
                        WriteString(builder, pair.Key);
                        builder.Append(": ");
                        Write(builder, pair.Value, depth + 1);
                        first = false;
                    }

                    builder.Append('\n').Append(' ', depth * 2).Append('}');
                    return;
                }
                case List<object> list:
                {
                    // 짧은 문자열 배열(대사 두 벌)은 한 줄로 — 표가 길어지지 않게.
                    builder.Append('[');

                    for (var i = 0; i < list.Count; i++)
                    {
                        if (i > 0)
                        {
                            builder.Append(", ");
                        }

                        Write(builder, list[i], depth + 1);
                    }

                    builder.Append(']');
                    return;
                }
                default:
                {
                    throw new ArgumentException($"Cannot serialize {value.GetType().Name}.");
                }
            }
        }

        private static void WriteString(StringBuilder builder, string text)
        {
            builder.Append('"');

            foreach (var c in text)
            {
                switch (c)
                {
                    case '"':
                    {
                        builder.Append("\\\"");
                        break;
                    }
                    case '\\':
                    {
                        builder.Append("\\\\");
                        break;
                    }
                    case '\n':
                    {
                        builder.Append("\\n");
                        break;
                    }
                    case '\r':
                    {
                        builder.Append("\\r");
                        break;
                    }
                    case '\t':
                    {
                        builder.Append("\\t");
                        break;
                    }
                    default:
                    {
                        builder.Append(c);
                        break;
                    }
                }
            }

            builder.Append('"');
        }

        private sealed class Reader
        {
            private readonly string Text;
            private int Index;

            public Reader(string text)
            {
                Text = text;
            }

            public bool AtEnd
            {
                get
                {
                    return Index >= Text.Length;
                }
            }

            public FormatException Fail(string message)
            {
                return new FormatException($"{message} (at {Index})");
            }

            public void SkipWhitespace()
            {
                while (AtEnd == false && char.IsWhiteSpace(Text[Index]) == true)
                {
                    Index++;
                }
            }

            public object ReadValue()
            {
                SkipWhitespace();

                if (AtEnd == true)
                {
                    throw Fail("Unexpected end of JSON.");
                }

                var c = Text[Index];

                switch (c)
                {
                    case '{':
                    {
                        return ReadObject();
                    }
                    case '[':
                    {
                        return ReadArray();
                    }
                    case '"':
                    {
                        return ReadString();
                    }
                    case 't':
                    {
                        Expect("true");
                        return true;
                    }
                    case 'f':
                    {
                        Expect("false");
                        return false;
                    }
                    case 'n':
                    {
                        Expect("null");
                        return null;
                    }
                    default:
                    {
                        return ReadNumber();
                    }
                }
            }

            private void Expect(string word)
            {
                if (string.CompareOrdinal(Text, Index, word, 0, word.Length) != 0)
                {
                    throw Fail($"Expected '{word}'.");
                }

                Index += word.Length;
            }

            private Dictionary<string, object> ReadObject()
            {
                var result = new Dictionary<string, object>();
                Index++;
                SkipWhitespace();

                if (AtEnd == false && Text[Index] == '}')
                {
                    Index++;
                    return result;
                }

                while (true)
                {
                    SkipWhitespace();

                    if (AtEnd == true || Text[Index] != '"')
                    {
                        throw Fail("Expected a property name.");
                    }

                    var key = ReadString();
                    SkipWhitespace();

                    if (AtEnd == true || Text[Index] != ':')
                    {
                        throw Fail("Expected ':'.");
                    }

                    Index++;
                    result[key] = ReadValue();
                    SkipWhitespace();

                    if (AtEnd == true)
                    {
                        throw Fail("Unterminated object.");
                    }

                    if (Text[Index] == ',')
                    {
                        Index++;
                        continue;
                    }

                    if (Text[Index] == '}')
                    {
                        Index++;
                        return result;
                    }

                    throw Fail("Expected ',' or '}'.");
                }
            }

            private List<object> ReadArray()
            {
                var result = new List<object>();
                Index++;
                SkipWhitespace();

                if (AtEnd == false && Text[Index] == ']')
                {
                    Index++;
                    return result;
                }

                while (true)
                {
                    result.Add(ReadValue());
                    SkipWhitespace();

                    if (AtEnd == true)
                    {
                        throw Fail("Unterminated array.");
                    }

                    if (Text[Index] == ',')
                    {
                        Index++;
                        continue;
                    }

                    if (Text[Index] == ']')
                    {
                        Index++;
                        return result;
                    }

                    throw Fail("Expected ',' or ']'.");
                }
            }

            private string ReadString()
            {
                var builder = new StringBuilder();
                Index++;

                while (AtEnd == false)
                {
                    var c = Text[Index++];

                    if (c == '"')
                    {
                        return builder.ToString();
                    }

                    if (c != '\\')
                    {
                        builder.Append(c);
                        continue;
                    }

                    if (AtEnd == true)
                    {
                        break;
                    }

                    var escaped = Text[Index++];

                    switch (escaped)
                    {
                        case 'b':
                        {
                            builder.Append('\b');
                            break;
                        }
                        case 'f':
                        {
                            builder.Append('\f');
                            break;
                        }
                        case 'n':
                        {
                            builder.Append('\n');
                            break;
                        }
                        case 'r':
                        {
                            builder.Append('\r');
                            break;
                        }
                        case 't':
                        {
                            builder.Append('\t');
                            break;
                        }
                        case 'u':
                        {
                            if (Index + 4 > Text.Length)
                            {
                                throw Fail("Truncated unicode escape.");
                            }

                            var code = Text.Substring(Index, 4);
                            builder.Append((char)int.Parse(code, NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                            Index += 4;
                            break;
                        }
                        default:
                        {
                            // \" \\ \/ 는 그 글자 그대로다.
                            builder.Append(escaped);
                            break;
                        }
                    }
                }

                throw Fail("Unterminated string.");
            }

            private double ReadNumber()
            {
                var start = Index;

                while (AtEnd == false && "+-0123456789.eE".IndexOf(Text[Index]) >= 0)
                {
                    Index++;
                }

                if (Index == start)
                {
                    throw Fail("Unexpected character.");
                }

                var token = Text.Substring(start, Index - start);
                return double.Parse(token, NumberStyles.Float, CultureInfo.InvariantCulture);
            }
        }
    }
}
