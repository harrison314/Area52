namespace Area52.Services.Implementation.QueryParser.Nodes;

internal class NumberValueNode : IValueNode
{
    public double Value
    {
        get;
    }

    public NumberValueNode(double value)
    {
        this.Value = value;
    }

    public void ToRql(RqlQueryBuilderContext context)
    {
        context.Append(this.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    public override string ToString()
    {
        return this.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
