namespace Area52.Services.Implementation.QueryParser.Nodes;

internal class PropertyNode : IAstNode
{
    private readonly string name;

    public PropertyNode(string name)
    {
        this.name = name;
    }

    public void ToRql(RqlQueryBuilderContext context)
    {
        context.Append(this.name);
    }

    public override string ToString()
    {
        return this.name;
    }
}
