namespace Area52.Services.Implementation.QueryParser.Nodes;

internal class GtNode : BinaryOpNode
{
    public GtNode(IAstNode left, IAstNode right) : base(left, right)
    {
    }

    public override void ToRql(RqlQueryBuilderContext context)
    {
        this.Left.ToRql(context);
        context.Append(" > ");
        this.Right.ToRql(context);
    }

    public override string ToString()
    {
        return this.ToString(">");
    }
}
