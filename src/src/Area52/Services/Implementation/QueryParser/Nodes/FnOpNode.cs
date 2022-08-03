namespace Area52.Services.Implementation.QueryParser.Nodes;

internal class FnOpNode : BinaryOpNode
{
    public static class FnNames
    {
        public const string StartsWith = "startsWith";
        public const string EndsWith = "endsWith";
    }

    public string FnName
    {
        get;
    }

    public FnOpNode(IAstNode left, IAstNode right, string functionName) : base(left, right)
    {
        this.FnName = functionName;
    }

    public override void ToRql(RqlQueryBuilderContext context)
    {
        if (this.FnName == FnNames.StartsWith)
        {
            context.Append("startsWith(");
            this.Left.ToRql(context);
            context.Append(", ");
            this.Right.ToRql(context);
            context.Append(") ");
            return;
        }

        if (this.FnName == FnNames.EndsWith)
        {
            context.Append("endsWith(");
            this.Left.ToRql(context);
            context.Append(", ");
            this.Right.ToRql(context);
            context.Append(") ");
            return;
        }

        throw new InvalidProgramException($"Invalid function name {this.FnName}.");
    }

    public override string ToString()
    {
        return this.ToString(this.FnName);
    }
}
