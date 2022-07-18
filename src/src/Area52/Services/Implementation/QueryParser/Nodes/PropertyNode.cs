namespace Area52.Services.Implementation.QueryParser.Nodes;

internal class PropertyNode : IAstNode
{
    public string Name
    {
        get;
    }

    public PropertyNode(string name)
    {
        this.Name = name;
    }

    public void ToRql(RqlQueryBuilderContext context)
    {
        context.Append(this.Name);
    }

    public override string ToString()
    {
        return this.Name;
    }
}
