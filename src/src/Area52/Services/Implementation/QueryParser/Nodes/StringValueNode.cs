using System.Text;

namespace Area52.Services.Implementation.QueryParser.Nodes;

internal class StringValueNode : IValueNode
{
    public string Value
    {
        get;
    }

    public StringValueNode(string value)
    {
        this.Value = value;
    }

    public void ToRql(RqlQueryBuilderContext context)
    {
        string paramName = context.AddParameterWithValue(this.Value);
        context.Append('$');
        context.Append(paramName);
    }

    public override string ToString()
    {
        return ToStringLiteral(this.Value);
    }

    public static StringValueNode FromLiteral(string literal)
    {
        StringBuilder sb = new StringBuilder(literal.Length);
        ReadOnlySpan<char> internLiteral = literal.AsSpan()[1..^1];
        for (int i = 0; i < internLiteral.Length; i++)
        {
            char c = internLiteral[i];
            if (c == '\\')
            {
                i++;
                char next = internLiteral[i];
                char appendChar = next switch
                {
                    '\\' => '\\',
                    '0' => '\0',
                    'a' => '\a',
                    'b' => '\b',
                    'f' => '\f',
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    'v' => '\v',
                    '\'' when literal[0] == '\'' => '\'',
                    '"' when literal[0] == '"' => '"',
                    'u' => throw new NotImplementedException("Unicode char encoding is not implemented."),
                    _ => throw new ApplicationException($"Invalid chacter in string on position {i}")
                };


                sb.Append(appendChar);
            }
            else
            {
                sb.Append(c);
            }
        }

        return new StringValueNode(sb.ToString());
    }

    public static string ToStringLiteral(string rawValue)
    {
        StringBuilder literal = new StringBuilder(rawValue.Length + 2);
        literal.Append("\"");
        foreach (var c in rawValue)
        {
            switch (c)
            {
                case '\"': literal.Append("\\\""); break;
                case '\\': literal.Append(@"\\"); break;
                case '\0': literal.Append(@"\0"); break;
                case '\a': literal.Append(@"\a"); break;
                case '\b': literal.Append(@"\b"); break;
                case '\f': literal.Append(@"\f"); break;
                case '\n': literal.Append(@"\n"); break;
                case '\r': literal.Append(@"\r"); break;
                case '\t': literal.Append(@"\t"); break;
                case '\v': literal.Append(@"\v"); break;
                default:
                    // ASCII printable character
                    if (c >= 0x20 && c <= 0x7e)
                    {
                        literal.Append(c);
                        // As UTF16 escaped character
                    }
                    else
                    {
                        literal.Append(@"\u");
                        literal.Append(((int)c).ToString("x4"));
                    }
                    break;
            }
        }

        literal.Append("\"");

        return literal.ToString();
    }
}
