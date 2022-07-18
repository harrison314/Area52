namespace Area52.Services.Implementation.QueryParser.Nodes;

internal class EqNode : BinaryOpNode
{
    public bool IsExact
    {
        get;
    }

    public EqNode(IAstNode left, IAstNode righht, bool isExact) : base(left, righht)
    {
        this.IsExact = isExact;
    }

    public override void ToRql(RqlQueryBuilderContext context)
    {
        if (this.IsExact)
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
        return this.ToString(this.IsExact ? "is exact" : "is");
    }
}
