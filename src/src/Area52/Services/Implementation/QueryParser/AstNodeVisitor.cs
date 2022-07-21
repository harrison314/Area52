using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Area52.Services.Implementation.QueryParser.Nodes;

namespace Area52.Services.Implementation.QueryParser;

internal abstract class AstNodeVisitor
{
    public virtual void Visit(IAstNode node)
    {
        node = this.PreVisitNode(node);

        switch (node)
        {
            case AndNode andNode:
                this.VisitInternal(andNode);
                break;
            case ArrayNode arrayNode:
                this.VisitInternal(arrayNode);
                break;
            case BetweenNode betweenNode:
                this.VisitInternal(betweenNode);
                break;
            case EqNode eqNode:
                this.VisitInternal(eqNode);
                break;
            case ExistsNode existsNode:
                this.VisitInternal(existsNode);
                break;
            case FnOpNode fnOpNode:
                this.VisitInternal(fnOpNode);
                break;
            case GtNode gtNode:
                this.VisitInternal(gtNode);
                break;
            case GtOrEqNode gtOrEqNode:
                this.VisitInternal(gtOrEqNode);
                break;
            case InNode inNode:
                this.VisitInternal(inNode);
                break;
            case LtNode ltNode:
                this.VisitInternal(ltNode);
                break;
            case LtOrEqNode ltOrEqNode:
                this.VisitInternal(ltOrEqNode);
                break;
            case NotEqNode notEqNode:
                this.VisitInternal(notEqNode);
                break;
            case NullValueNode nullValueNode:
                this.VisitInternal(nullValueNode);
                break;
            case NumberValueNode numberValueNode:
                this.VisitInternal(numberValueNode);
                break;
            case OrNode orNode:
                this.VisitInternal(orNode);
                break;
            case PropertyNode propertyNode:
                this.VisitInternal(propertyNode);
                break;
            case SearchNode searchNode:
                this.VisitInternal(searchNode);
                break;
            case StringValueNode stringValueNode:
                this.VisitInternal(stringValueNode);
                break;

            default:
                throw new InvalidProgramException($"Node type {node.GetType().FullName} is not implement in visitor.");
        }
    }

    protected virtual IAstNode PreVisitNode(IAstNode node)
    {
        return node;
    }

    protected virtual void VisitInternal(AndNode node)
    {
        this.Visit(node.Left);
        this.Visit(node.Right);
    }

    protected virtual void VisitInternal(ArrayNode node)
    {
        foreach (var aNode in node.Nodes)
        {
            this.Visit(aNode);
        }
    }

    protected virtual void VisitInternal(BetweenNode node)
    {
        this.Visit(node.Property);
        this.Visit(node.FromValue);
        this.Visit(node.ToValue);
    }

    protected virtual void VisitInternal(EqNode node)
    {
        this.Visit(node.Left);
        this.Visit(node.Right);
    }

    protected virtual void VisitInternal(ExistsNode node)
    {
        this.Visit(node.Nested);
    }

    protected virtual void VisitInternal(FnOpNode node)
    {
        this.Visit(node.Left);
        this.Visit(node.Right);
    }

    protected virtual void VisitInternal(GtNode node)
    {
        this.Visit(node.Left);
        this.Visit(node.Right);
    }

    protected virtual void VisitInternal(GtOrEqNode node)
    {
        this.Visit(node.Left);
        this.Visit(node.Right);
    }

    protected virtual void VisitInternal(InNode node)
    {
        this.Visit(node.Left);
        this.Visit(node.Right);
    }

    protected virtual void VisitInternal(LtNode node)
    {
        this.Visit(node.Left);
        this.Visit(node.Right);
    }

    protected virtual void VisitInternal(LtOrEqNode node)
    {
        this.Visit(node.Left);
        this.Visit(node.Right);
    }

    protected virtual void VisitInternal(NotEqNode node)
    {
        this.Visit(node.Left);
        this.Visit(node.Right);
    }

    protected virtual void VisitInternal(NullValueNode node)
    {
        // NOP
    }

    protected virtual void VisitInternal(NumberValueNode node)
    {
        // NOP
    }

    protected virtual void VisitInternal(OrNode node)
    {
        this.Visit(node.Left);
        this.Visit(node.Right);
    }

    protected virtual void VisitInternal(PropertyNode node)
    {
        // NOP
    }

    protected virtual void VisitInternal(SearchNode node)
    {
        this.Visit(node.Left);
        this.Visit(node.Right);
    }

    protected virtual void VisitInternal(StringValueNode node)
    {
        // NOP
    }
}
