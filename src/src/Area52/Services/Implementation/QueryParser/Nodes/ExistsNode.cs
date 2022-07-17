namespace Area52.Services.Implementation.QueryParser.Nodes;

internal class ExistsNode : IAstNode
{
    private readonly IAstNode node;

    public IAstNode Nested
    {
        get => this.node;
    }

    public ExistsNode(IAstNode node)
    {
        this.node = node;
    }
    public void ToRql(RqlQueryBuilderContext context)
    {
        context.Append("exists(");
        this.node.ToRql(context);
        context.Append(")");
    }

    public override string ToString()
    {
        return string.Concat("exists ", this.node.ToString());
    }
}
