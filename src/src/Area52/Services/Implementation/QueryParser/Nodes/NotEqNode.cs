namespace Area52.Services.Implementation.QueryParser.Nodes;

internal class NotEqNode : BinaryOpNode
{
    public NotEqNode(IAstNode left, IAstNode right) : base(left, right)
    {
    }

    public override void ToRql(RqlQueryBuilderContext context)
    {
        if (this.Left is PropertyNode && this.Right is NullValueNode)
        {
            context.Append("(exists(");
            this.Left.ToRql(context);
            context.Append(") and ");
            this.Left.ToRql(context);
            context.Append(" != null)");

            return;
        }

        this.Left.ToRql(context);
        context.Append(" != ");
        this.Right.ToRql(context);
    }

    public override string ToString()
    {
        return this.ToString("is not");
    }
}
