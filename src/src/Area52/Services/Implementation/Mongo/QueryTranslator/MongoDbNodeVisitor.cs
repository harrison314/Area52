using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Area52.Services.Contracts;
using Area52.Services.Implementation.Mongo.Models;
using Area52.Services.Implementation.QueryParser;
using Area52.Services.Implementation.QueryParser.Nodes;
using MongoDB.Bson;

namespace Area52.Services.Implementation.Mongo.QueryTranslator;

internal class MongoDbNodeVisitor : AstNodeVisitor
{
    private readonly Stack<BsonCtxNode> ctxStack;
    public MongoDbNodeVisitor()
    {
        this.ctxStack = new Stack<BsonCtxNode>();
    }

    public BsonDocument ToBsonFindCriteria()
    {
        if (this.ctxStack.Count == 0)
        {
            return new BsonDocument();
        }

        System.Diagnostics.Debug.Assert(this.ctxStack.Count == 1);
        BsonValue expression = this.ctxStack.Pop().Value;

        return (BsonDocument)expression;
    }

    protected override void VisitInternal(EqNode node)
    {
        if (node.IsExact || node.Right is not StringValueNode)
        {
            this.PushBinaryOperation(node, "$eq");
        }
        else
        {
            PropertyNode propertyNode = (PropertyNode)node.Left;
            StringValueNode strNode = (StringValueNode)node.Right;

            if (propertyNode.Name == nameof(LogEntity.Level))
            {
                BsonDocument levelExperssion = new BsonDocument("LevelLower", new BsonDocument("$eq", strNode.Value.ToLowerInvariant()));
                this.ctxStack.Push(new BsonCtxNode(levelExperssion, QueryNodeType.Other));
                return;
            }

            string? specialName = propertyNode.Name switch
            {
                nameof(LogEntity.Timestamp) => "TimestampIndex.Sortable",
                nameof(LogEntity.Message) => "Message",
                nameof(LogEntity.Exception) => "Exception",
                //nameof(LogEntity.Level) => "Level",
                nameof(LogEntity.MessageTemplate) => "MessageTemplate",
                _ => null
            };

            if (specialName != null)
            {
                string regexExpr = string.Concat("^",
                    System.Text.RegularExpressions.Regex.Escape(strNode.Value),
                    "$");
                BsonDocument regexExpression = new BsonDocument(specialName, new BsonDocument()
                {
                    { "$regex", regexExpr },
                    { "$options", "i" }
                });

                this.ctxStack.Push(new BsonCtxNode(regexExpression, QueryNodeType.Other));
                return;
            }

            BsonDocument expression = this.ConstructPropertyOperation(propertyNode.Name,
            InternalValueType.LowercaseString,
            new BsonDocument()
            {
                { "$eq", strNode.Value.ToLowerInvariant() }
            });

            this.ctxStack.Push(new BsonCtxNode(expression, QueryNodeType.Other));
        }
    }

    protected override void VisitInternal(GtNode node)
    {
        this.PushBinaryOperation(node, "$gt");
    }

    protected override void VisitInternal(GtOrEqNode node)
    {
        this.PushBinaryOperation(node, "$gte");
    }

    protected override void VisitInternal(LtNode node)
    {
        this.PushBinaryOperation(node, "$lt");
    }

    protected override void VisitInternal(LtOrEqNode node)
    {
        this.PushBinaryOperation(node, "$lte");
    }

    protected override void VisitInternal(InNode node)
    {
        System.Diagnostics.Debug.Assert(node.Left is PropertyNode);
        System.Diagnostics.Debug.Assert(node.Right is ArrayNode);

        PropertyNode propertyNode = (PropertyNode)node.Left;
        ArrayNode valueNode = (ArrayNode)node.Right;

        BsonArray array = new BsonArray(valueNode.Nodes.Select(t => this.ConvertValue((IValueNode)t)));

        BsonDocument expression = this.ConstructPropertyOperation(propertyNode.Name,
            valueNode.Nodes.All(t => t is NumberValueNode),
            new BsonDocument() { { "$in", array } });

        this.ctxStack.Push(new BsonCtxNode(expression, QueryNodeType.Other));
    }

    protected override void VisitInternal(NotEqNode node)
    {
        System.Diagnostics.Debug.Assert(node.Left is PropertyNode);

        if (node.Right is NullValueNode)
        {
            this.PushExists((PropertyNode)node.Left);
            this.PushBinaryOperation(node, "$ne");

            BsonDocument andOperation = new BsonDocument("$and", new BsonArray()
            {
                this.ctxStack.Pop().Value,
                this.ctxStack.Pop().Value
            });

            this.ctxStack.Push(new BsonCtxNode(andOperation, QueryNodeType.And));
        }
        else
        {
            this.PushBinaryOperation(node, "$ne");
        }
    }

    protected override void VisitInternal(SearchNode node)
    {
        System.Diagnostics.Debug.Assert(node.Left is PropertyNode or NullValueNode);
        System.Diagnostics.Debug.Assert(node.Right is StringValueNode);

        StringValueNode stringNode = (StringValueNode)node.Right;

        if (node.Left is PropertyNode propertyNode && propertyNode.Name != "LogFullText")
        {
            throw new QuerySyntaxMongoException("Invalid use of search operator for MongoDb back-end.");
        }

        BsonDocument expression = new BsonDocument()
        {
            {"$text", new BsonDocument()
                {
                    { "$search", stringNode.Value },
                    { "$caseSensitive", false },
                    { "$diacriticSensitive", false }
                }
            }
        };

        this.ctxStack.Push(new BsonCtxNode(expression, QueryNodeType.Other));
    }

    protected override void VisitInternal(StringValueNode node)
    {
        throw new InvalidProgramException("StringValueNode can not use.");
    }

    protected override void VisitInternal(NullValueNode node)
    {
        throw new InvalidProgramException("NullValueNode can not use.");
    }

    protected override void VisitInternal(NumberValueNode node)
    {
        throw new InvalidProgramException("NumberValueNode can not use.");
    }

    protected override void VisitInternal(ArrayNode node)
    {
        throw new InvalidProgramException("ArrayNode can not use.");
    }

    protected override void VisitInternal(PropertyNode node)
    {
        throw new InvalidProgramException("PropertyNode can not use.");
    }

    protected override void VisitInternal(ExistsNode node)
    {
        System.Diagnostics.Debug.Assert(node.Nested is PropertyNode);

        this.PushExists(((PropertyNode)node.Nested));
    }

    protected override void VisitInternal(AndNode node)
    {
        base.Visit(node.Left);
        base.Visit(node.Right);

        BsonCtxNode rightCtx = this.ctxStack.Pop();
        BsonCtxNode leftCtx = this.ctxStack.Pop();

        BsonDocument expression = (leftCtx.Type, rightCtx.Type) switch
        {
            (QueryNodeType.And, QueryNodeType.And) => this.JoinArrays("$and", leftCtx.Value, rightCtx.Value),
            (QueryNodeType.And, _) => this.JoinArrays("$and", leftCtx.Value, this.CreateFakeArrayOp("$and", rightCtx.Value)),
            (_, QueryNodeType.And) => this.JoinArrays("$and", this.CreateFakeArrayOp("$and", leftCtx.Value), rightCtx.Value),
            (_, _) => new BsonDocument("$and", new BsonArray() { leftCtx.Value, rightCtx.Value }),
        };

        this.ctxStack.Push(new BsonCtxNode(expression, QueryNodeType.And));
    }

    protected override void VisitInternal(OrNode node)
    {
        base.Visit(node.Left);
        base.Visit(node.Right);

        BsonCtxNode rightCtx = this.ctxStack.Pop();
        BsonCtxNode leftCtx = this.ctxStack.Pop();

        BsonDocument expression = (leftCtx.Type, rightCtx.Type) switch
        {
            (QueryNodeType.Or, QueryNodeType.Or) => this.JoinArrays("$or", leftCtx.Value, rightCtx.Value),
            (QueryNodeType.Or, _) => this.JoinArrays("$or", leftCtx.Value, this.CreateFakeArrayOp("$or", rightCtx.Value)),
            (_, QueryNodeType.Or) => this.JoinArrays("$or", this.CreateFakeArrayOp("$or", leftCtx.Value), rightCtx.Value),
            (_, _) => new BsonDocument("$or", new BsonArray() { leftCtx.Value, rightCtx.Value }),
        };

        this.ctxStack.Push(new BsonCtxNode(expression, QueryNodeType.Or));
    }

    protected override void VisitInternal(BetweenNode node)
    {
        System.Diagnostics.Debug.Assert(node.Property is PropertyNode);
        System.Diagnostics.Debug.Assert(node.FromValue is IValueNode);
        System.Diagnostics.Debug.Assert(node.ToValue is IValueNode);

        PropertyNode propertyNode = (PropertyNode)node.Property;
        IValueNode fromValueNode = (IValueNode)node.FromValue;
        IValueNode toValueNode = (IValueNode)node.ToValue;

        bool useNumeric = fromValueNode is NumberValueNode && toValueNode is NumberValueNode;

        BsonDocument expressionFrom = this.ConstructPropertyOperation(propertyNode.Name,
            useNumeric,
            new BsonDocument() { { "$gte", this.ConvertValue(fromValueNode) } });

        BsonDocument expressionTo = this.ConstructPropertyOperation(propertyNode.Name,
           useNumeric,
           new BsonDocument() { { "$lte", this.ConvertValue(toValueNode) } });

        BsonDocument expression = new BsonDocument("$and", new BsonArray() { expressionFrom, expressionTo });
        this.ctxStack.Push(new BsonCtxNode(expression, QueryNodeType.And));
    }

    protected override void VisitInternal(FnOpNode node)
    {
        System.Diagnostics.Debug.Assert(node.Left is PropertyNode);
        System.Diagnostics.Debug.Assert(node.Right is StringValueNode);

        PropertyNode propertyNode = (PropertyNode)node.Left;
        StringValueNode valueNode = (StringValueNode)node.Right;

        string regexExpr = node.FnName switch
        {
            FnOpNode.FnNames.StartsWith => string.Concat("^", System.Text.RegularExpressions.Regex.Escape(valueNode.Value)),
            FnOpNode.FnNames.EndsWith => string.Concat(System.Text.RegularExpressions.Regex.Escape(valueNode.Value), "$"),
            _ => throw new InvalidProgramException($"FnName {node.FnName} is not supported.")
        };

        BsonDocument expression = this.ConstructPropertyOperation(propertyNode.Name,
            false,
            new BsonDocument()
            {
                { "$regex", regexExpr },
                { "$options", "i" }
            });

        this.ctxStack.Push(new BsonCtxNode(expression, QueryNodeType.Other));
    }

    private void PushExists(PropertyNode node)
    {
        string queryPropertyName = node.Name;
        string? proprtyName = this.TryGetSimplePropertyName(queryPropertyName);

        if (proprtyName != null)
        {
            BsonDocument expression = new BsonDocument()
            {
                {proprtyName, new BsonDocument("$exists", true)}
            };
            this.ctxStack.Push(new BsonCtxNode(expression, QueryNodeType.Other));
        }
        else
        {
            BsonDocument expression = new BsonDocument()
            {
                {"Properties.Name", queryPropertyName }
            };

            this.ctxStack.Push(new BsonCtxNode(expression, QueryNodeType.Other));
        }
    }
    private BsonDocument CreateFakeArrayOp(string op, BsonValue value)
    {
        return new BsonDocument(op, new BsonArray() { value });
    }

    private BsonDocument JoinArrays(string op, BsonValue left, BsonValue right)
    {
        BsonArray leftArray = (BsonArray)((BsonDocument)left).GetValue(op);
        BsonArray rightArray = (BsonArray)((BsonDocument)right).GetValue(op);

        return new BsonDocument(op, new BsonArray(leftArray.Values.Concat(rightArray.Values)));
    }

    private BsonDocument ConstructPropertyOperation(string propertyName, bool useDoubleValue, BsonValue operationDefinition)
    {
        return this.ConstructPropertyOperation(propertyName,
            useDoubleValue ? InternalValueType.Double : InternalValueType.String,
            operationDefinition);
    }

    private BsonDocument ConstructPropertyOperation(string propertyName, InternalValueType valueType, BsonValue operationDefinition)
    {
        string? simplePropertyName = this.TryGetSimplePropertyName(propertyName);

        if (simplePropertyName != null)
        {
            return new BsonDocument()
            {
                {simplePropertyName, operationDefinition }
            };
        }

        string valueProperty = valueType switch
        {
            InternalValueType.String => nameof(LogEntityPropertyForMongo.Values),
            InternalValueType.LowercaseString => nameof(LogEntityPropertyForMongo.ValuesLower),
            InternalValueType.Double => nameof(LogEntityPropertyForMongo.Valued),
            _ => throw new InvalidProgramException($"Enum value {valueType} is not supported.")
        };

        BsonDocument elemMathc = new BsonDocument("$elemMatch", new BsonDocument()
        {
            {"Name",  propertyName },
            {valueProperty, operationDefinition}
        });

        return new BsonDocument("Properties", elemMathc);
    }

    private string? TryGetSimplePropertyName(string propertyName)
    {
        return propertyName switch
        {
            nameof(LogEntity.Timestamp) => "TimestampIndex.Sortable",
            nameof(LogEntity.Message) => "Message",
            nameof(LogEntity.LevelNumeric) => "LevelNumeric",
            nameof(LogEntity.Exception) => "Exception",
            nameof(LogEntity.EventId) => "EventId",
            nameof(LogEntity.Level) => "Level",
            nameof(LogEntity.MessageTemplate) => "MessageTemplate",
            _ => null
        };
    }

    private void PushBinaryOperation(BinaryOpNode binaryOpNode, string op)
    {
        System.Diagnostics.Debug.Assert(binaryOpNode.Left is PropertyNode);
        System.Diagnostics.Debug.Assert(binaryOpNode.Right is IValueNode);

        PropertyNode propertyNode = (PropertyNode)binaryOpNode.Left;
        IValueNode valueNode = (IValueNode)binaryOpNode.Right;

        BsonDocument expression = this.ConstructPropertyOperation(propertyNode.Name,
            (valueNode is NumberValueNode),
            new BsonDocument() { { op, this.ConvertValue(valueNode) } });

        this.ctxStack.Push(new BsonCtxNode(expression, QueryNodeType.Other));
    }

    private BsonValue ConvertValue(IValueNode valueNode)
    {
        return valueNode switch
        {
            NullValueNode _ => BsonNull.Value,
            StringValueNode str => new BsonString(str.Value),
            NumberValueNode num => new BsonDouble(num.Value),
            _ => throw new InvalidProgramException($"Invalid value node {valueNode.GetType().FullName}")
        };
    }
}
