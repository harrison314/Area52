using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Area52.Services.Implementation.QueryParser;
using Xunit.Abstractions;

namespace Area52.UnitTests.Implementation.QueryParser;

public class ParserTests
{
    private readonly ITestOutputHelper output;

    public ParserTests(ITestOutputHelper output)
    {
        this.output = output;
    }

    [Theory]
    [InlineData("Property is 'value'")]
    [InlineData("[Property] is 'value'")]
    [InlineData("[Property with space] is 'value'")]
    [InlineData("[0] is 'value'")]
    [InlineData("Property == 'value'")]
    [InlineData("Property is not 'value'")]
    [InlineData("Property != 'value'")]
    [InlineData("Property < 'value'")]
    [InlineData("Property < 12.36")]
    [InlineData("Property > 'value'")]
    [InlineData("Property > 12.36")]
    [InlineData("Property <= 'value'")]
    [InlineData("Property <= 12.36")]
    [InlineData("Property >= 'value'")]
    [InlineData("Property >= 12.36")]
    [InlineData("Property exists any")]
    [InlineData("Property is not null")]
    [InlineData("Property between 12 and 18.9")]
    [InlineData("Property >= 12 and Property >= 18.9")]
    [InlineData("'value'")]
    [InlineData("Property startsWith 'value'")]
    [InlineData("Property endsWith 'value'")]
    [InlineData("Property is not 'value1' and Property is not 'value2'")]
    [InlineData("Property is not 'value1' && Property is not 'value2'")]
    [InlineData("Property is 'value1' or Property is 'value2'")]
    [InlineData("Property is 'value1' || Property is 'value2'")]
    [InlineData("RequestId is '0HMJ3E0UJPORG:00000007' and (Exception is not null or Level is 'Error')")]
    public void Parser_SimpleParse_ParseExamples(string query)
    {
        IAstNode astNode = Parser.SimpleParse(query);
        Assert.NotNull(astNode);

        this.output.WriteLine(astNode.ToString());
    }

    [Theory]
    [InlineData("Timestamp > datetime('2022-06-01')")]
    [InlineData("Timestamp > -45.56")]
    [InlineData("Applicaton search 'Area52'")]
    [InlineData("Property is cs 'value'")]
    [InlineData("logid('abfgdthxyya')")]
    [InlineData("logid(\"LogEvent/1459-B\")")]
    [InlineData("logid(\"LogEvent/1459-B\") or logid(\"LogEvent/1489-B\")")]
    public void Parser_SimpleParse_SpecialCases(string query)
    {
        IAstNode astNode = Parser.SimpleParse(query);
        Assert.NotNull(astNode);

        this.output.WriteLine(astNode.ToString());
    }
}
