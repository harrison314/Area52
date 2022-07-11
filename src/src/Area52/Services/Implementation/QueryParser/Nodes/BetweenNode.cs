using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Area52.Services.Implementation.QueryParser.Nodes;

internal class BetweenNode : IAstNode
{
    public IAstNode Property 
    { 
        get;
    }

    public IAstNode FromValue 
    { 
        get;
    }

    public IAstNode ToValue 
    { 
        get; 
    }

    public BetweenNode(IAstNode property, IAstNode fromValue, IAstNode toValue)
    {
        Property = property;
        FromValue = fromValue;
        ToValue = toValue;
    }

    public void ToRql(RqlQueryBuilderContext context)
    {
        this.Property.ToRql(context);
        context.Append(" between ");
        this.FromValue.ToRql(context);
        context.Append(" and ");
        this.ToValue.ToRql(context);
    }

    public override string ToString()
    {
        return $"{this.Property} between {this.FromValue} and {this.ToValue}";
    }
}