using System.Text;

namespace Area52.Services.Implementation.QueryParser.Nodes;

internal abstract class BinaryOpNode : IAstNode
{
    public IAstNode Left 
    { 
        get; 
    }

    public IAstNode Right 
    { 
        get;
    }

    public BinaryOpNode(IAstNode left, IAstNode right)
    {
        this.Left = left;
        this.Right = right;
    }

    public abstract void ToRql(RqlQueryBuilderContext context);

    protected string ToString(string op)
    {
        StringBuilder sb = new StringBuilder();
        this.FormatNode(this.Left, sb);
        sb.Append(' ').Append(op).Append(' ');
        this.FormatNode(this.Right, sb);

        return sb.ToString();
    }

    private void FormatNode(IAstNode node, StringBuilder sb)
    {
        bool addBraces = node switch
        {
            EqNode _ => false,
            BinaryOpNode _ => true,
            _ => false
        };

        if (addBraces)
        {
            sb.Append('(').Append(node.ToString()).Append(')');
        }
        else
        {
            sb.Append(node.ToString());
        }
    }
}
