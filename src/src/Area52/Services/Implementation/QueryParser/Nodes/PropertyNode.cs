namespace Area52.Services.Implementation.QueryParser.Nodes;

internal class PropertyNode : IAstNode
{
    private bool needEscape;

    public string Name
    {
        get;
    }

    public PropertyNode(string name)
    {
        this.Name = name;
        this.needEscape = this.CanNeedEscape(name);
    }

    public void ToRql(RqlQueryBuilderContext context)
    {
        if (this.needEscape)
        {
            context.Append('\'');
            context.Append(this.Name);
            context.Append('\'');
        }
        else
        {
            context.Append(this.Name);
        }
    }

    public override string ToString()
    {
        if (this.needEscape)
        {
            return string.Concat("[", this.Name, "]");
        }
        else
        {
            return this.Name;
        }
    }

    private bool CanNeedEscape(string value)
    {
        bool isNumbersOnly = true;
        bool contaisWhitespace = false;

        for (int i = 0; i < value.Length; i++)
        {
            isNumbersOnly &= char.IsDigit(value, i);
            contaisWhitespace |= char.IsWhiteSpace(value, i);
        }

        return isNumbersOnly || contaisWhitespace;
    }
}
