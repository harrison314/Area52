namespace Area52.Services.Implementation.QueryParser.Nodes;

internal class FnOpNode : BinaryOpNode
{
    private readonly string functionName;

    public FnOpNode(IAstNode left, IAstNode righht, string functionName) : base(left, righht)
    {
        this.functionName = functionName;
    }

    public override void ToRql(RqlQueryBuilderContext context)
    {
        if (this.functionName == "startsWith")
        {
            context.Append("startsWith(");
            this.Left.ToRql(context);
            context.Append(", ");
            this.Righht.ToRql(context);
            context.Append(") ");
            return;
        }

        if (this.functionName == "endsWith")
        {
            context.Append("endsWith(");
            this.Left.ToRql(context);
            context.Append(", ");
            this.Righht.ToRql(context);
            context.Append(") ");
            return;
        }

        throw new InvalidProgramException($"Inavlid function name {this.functionName}.");
    }

    public override string ToString()
    {
        return this.ToString(this.functionName);
    }
}
