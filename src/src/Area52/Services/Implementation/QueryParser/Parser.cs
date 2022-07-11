using Area52.Services.Implementation.QueryParser.Nodes;
using Piglet.Parser;
using Piglet.Parser.Configuration;

namespace Area52.Services.Implementation.QueryParser;

public class Parser
{
    private static IParser<IAstNode> nodeParser = CreateParser();
    private static IParser<IAstNode> CreateParser()
    {
        var configurator = ParserFactory.Configure<IAstNode>();

        ITerminal<IAstNode> stringTerminal = configurator.CreateTerminal("\"([^\"]+|\\\\\")*\"|'([^']+|\\\\')*'", ParseString);
        ITerminal<IAstNode> nullTerminal = configurator.CreateTerminal("null", _ => new NullValueNode());
        ITerminal<IAstNode> numberTerminal = configurator.CreateTerminal("[0-9]+(\\.[0-9]+)?", ParseNumber);
        ITerminal<IAstNode> propertyTerminal = configurator.CreateTerminal("[0-9a-zA-Z_]+", ParsePropety);

        INonTerminal<IAstNode> expr = configurator.CreateNonTerminal();
        INonTerminal<IAstNode> term = configurator.CreateNonTerminal();
        INonTerminal<IAstNode> factor = configurator.CreateNonTerminal();
        INonTerminal<IAstNode> atomExpr = configurator.CreateNonTerminal();
        INonTerminal<IAstNode> arrayExpr = configurator.CreateNonTerminal();
        INonTerminal<IAstNode> valueExpr = configurator.CreateNonTerminal();


        expr.AddProduction(expr, "or", term).SetReduceFunction(p => new OrNode(p[0], p[2]));
        expr.AddProduction(expr, "||", term).SetReduceFunction(p => new OrNode(p[0], p[2]));
        expr.AddProduction(term).SetReduceFunction(p => p[0]);

        term.AddProduction(term, "and", factor).SetReduceFunction(p => new AndNode(p[0], p[2]));
        term.AddProduction(term, "&&", factor).SetReduceFunction(p => new AndNode(p[0], p[2]));
        term.AddProduction(factor).SetReduceFunction(p => p[0]);

        factor.AddProduction("(", expr, ")").SetReduceFunction(s => s[1]);

        factor.AddProduction(atomExpr).SetReduceFunction(p => p[0]);


        atomExpr.AddProduction(propertyTerminal, "is", valueExpr).SetReduceFunction(p => new EqNode(p[0], p[2], true));
        atomExpr.AddProduction(propertyTerminal, "==", valueExpr).SetReduceFunction(p => new EqNode(p[0], p[2], true));
        atomExpr.AddProduction(propertyTerminal, "is", "not", valueExpr).SetReduceFunction(p => new NotEqNode(p[0], p[3]));
        atomExpr.AddProduction(propertyTerminal, "is", "eg", valueExpr).SetReduceFunction(p => new EqNode(p[0], p[3], false));

        atomExpr.AddProduction(propertyTerminal, "!=", valueExpr).SetReduceFunction(p => new NotEqNode(p[0], p[2]));
        atomExpr.AddProduction(propertyTerminal, "in", "{", arrayExpr, "}").SetReduceFunction(p => new InNode(p[0], p[3]));

        atomExpr.AddProduction(propertyTerminal, ">=", valueExpr).SetReduceFunction(p => new GtOrEqNode(p[0], p[2]));
        atomExpr.AddProduction(propertyTerminal, ">", valueExpr).SetReduceFunction(p => new GtNode(p[0], p[2]));

        atomExpr.AddProduction(propertyTerminal, "<=", valueExpr).SetReduceFunction(p => new LtOrEqNode(p[0], p[2]));
        atomExpr.AddProduction(propertyTerminal, "<", valueExpr).SetReduceFunction(p => new LtNode(p[0], p[2]));


        atomExpr.AddProduction(propertyTerminal, "startsWith", valueExpr).SetReduceFunction(p => new FnOpNode(p[0], p[2], "startsWith"));
        atomExpr.AddProduction(propertyTerminal, "endsWith", valueExpr).SetReduceFunction(p => new FnOpNode(p[0], p[2], "endsWith"));
        atomExpr.AddProduction(propertyTerminal, "search", valueExpr).SetReduceFunction(p => new SearchNode(p[0], p[2]));

        atomExpr.AddProduction(propertyTerminal, "exists", "any").SetReduceFunction(p => new ExistsNode(p[0]));
        atomExpr.AddProduction(propertyTerminal, "between", valueExpr, @"and", valueExpr).SetReduceFunction(p => new BetweenNode(p[0], p[2], p[4]));
        atomExpr.AddProduction(stringTerminal).SetReduceFunction(p => new SearchNode(p[0]));

        arrayExpr.AddProduction(arrayExpr, ",", stringTerminal).SetReduceFunction(p => ((ArrayNode)p[0]).Add(p[2]));
        arrayExpr.AddProduction(stringTerminal).SetReduceFunction(p => new ArrayNode().Add(p[0]));
        arrayExpr.AddProduction().SetReduceFunction(_ => new ArrayNode());


        valueExpr.AddProduction(stringTerminal).SetReduceToFirst();
        valueExpr.AddProduction(numberTerminal).SetReduceToFirst();
        valueExpr.AddProduction(nullTerminal).SetReduceToFirst();
        valueExpr.AddProduction("datetime", "(", stringTerminal, ")").SetReduceFunction(p =>
        {
            StringValueNode str = (StringValueNode)p[2];
            DateTime dt = DateTime.Parse(str.Value);

            return new StringValueNode(dt.ToString("s"));
        });

        return configurator.CreateParser();
    }

    public static IAstNode SimpleParse(string input)
    {
        return nodeParser.Parse(input.Trim());
    }

    private static StringValueNode ParseString(string value)
    {
        char start = value[0];
        if (start == '\'' || start == '"')
        {
            return StringValueNode.FromLiteral(value);
        }

       
        throw new ArgumentException("valie is not string");
    }

    private static IValueNode ParseNumber(string value)
    {
        return new NumberValueNode(double.Parse(value, System.Globalization.CultureInfo.InvariantCulture));
    }

    private static PropertyNode ParsePropety(string value)
    {
        return new PropertyNode(value);
    }
}