namespace Area52.Services.Implementation.QueryParser.Nodes;

internal class EqNode : BinaryOpNode
{
    private readonly bool isExact;

    public EqNode(IAstNode left, IAstNode righht, bool isExact) : base(left, righht)
    {
        this.isExact = isExact;
    }

    public override void ToRql(RqlQueryBuilderContext context)
    {
        if (this.isExact)
        {
            context.Append("exact(");
            this.Left.ToRql(context);
            context.Append(" == ");
            this.Righht.ToRql(context);
            context.Append(")");
        }
        else
        {
            this.Left.ToRql(context);
            context.Append(" == ");
            this.Righht.ToRql(context);
        }
    }

    public override string ToString()
    {
        return this.ToString(this.isExact ? "is exact" : "is");
    }
}
