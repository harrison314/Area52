namespace Area52.Services.Implementation.QueryParser.Nodes;

internal class ArrayNode : IAstNode
{
    private List<IAstNode> nodes;
    public ArrayNode()
    {
        this.nodes = new List<IAstNode>();
    }

    public ArrayNode Add(IAstNode node)
    {
        this.nodes.Add(node);
        return this;
    }

    public void ToRql(RqlQueryBuilderContext context)
    {
        bool isFirst = true;
        foreach (IAstNode node in this.nodes)
        {
            if (isFirst)
            {
                isFirst = false;
            }
            else
            {
                context.Append(", ");
            }

            node.ToRql(context);
        }
    }

    public override string ToString()
    {
        return string.Concat("{", string.Join(", ", this.nodes), "}");
    }
}
