using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Area52.Infrastructure.Clef;

namespace Area52.UnitTests.Infrastructure.Clef;

public class ClefParserTests
{
    [Theory]
    [InlineData("{'@t':'2016-06-07T03:44:57.8532799Z','@mt':'Hello, {User}','User':'nblumhardt'}")]
    [InlineData("{'@t':'2022-07-29T06:17:58.4251450Z'}")]
    [InlineData("{'@t':'2022-07-14T19:03:38.3270683+00:00','@m':'Start sleep to: 4539 ms','@mt':'Start sleep to: {sleepTime} ms','@l':'Debug','sleepTime':4539,'SourceContext':'Area52.LogProducer.SimpleHostedService','0':29,'Scope':['Run 29']}")]
    [InlineData("{'@t':'2022-07-29T06:17:58.4251450Z','@mt':'Hosting starting','@l':'Debug','EventId':{'Id':1},'SourceContext':'Microsoft.Extensions.Hosting.Internal.Host','Application':'FooBar'}")]
    [InlineData("{'@t':'2022-07-29T06:29:24.2977384Z','@mt':'Assigning ErrorId {0} to exception','@l':'Error','@x':'Error exception', '0':'5589'}")]
    [InlineData("{'@t':'2016-06-07T03:44:57.8532799Z','@mt':'Data: {data}','data':12}")]
    [InlineData("{'@t':'2016-06-07T03:44:57.8532799Z','@mt':'Data: {data}','data':12.3569}")]
    [InlineData("{'@t':'2016-06-07T03:44:57.8532799Z','@mt':'Data: {data}','data':true}")]
    [InlineData("{'@t':'2016-06-07T03:44:57.8532799Z','@mt':'Data: {data}','data':false}")]
    [InlineData("{'@t':'2016-06-07T03:44:57.8532799Z','@mt':'Data: {data}','data':null}")]
    [InlineData("{'@t':'2016-06-07T03:44:57.8532799Z','@mt':'Data: {data}','data':[]}")]
    [InlineData("{'@t':'2016-06-07T03:44:57.8532799Z','@mt':'Data: {data}','data':[1,2,3]}")]
    [InlineData("{'@t':'2016-06-07T03:44:57.8532799Z','@mt':'Data: {data}','data':{}}")]
    [InlineData("{'@t':'2016-06-07T03:44:57.8532799Z','@mt':'Data: {data}','data':{'foo':'bar'}}")]
    [InlineData("{'@t':'2016-06-07T03:44:57.8532799Z','@mt':'Data: {data}','data':{'foo':'bar'},'@i':145}")]
    [InlineData("{'@t':'2016-06-07T03:44:57.8532799Z','@mt':'Data: {data}','data':{'foo':'bar'},'@i':'a145'}")]
    public void ClefParser_Read_Success(string logLine)
    {
        string jsonLine = logLine.Replace('\'', '"');
        byte[] data = Encoding.UTF8.GetBytes(jsonLine);
        Services.Contracts.LogEntity logEntry = ClefParser.Read(data, true);

        Assert.NotNull(logEntry);
    }

    [Theory]
    [InlineData("{'@t':'2016-06-07T03:44:57.8532799Z','@mt':'Data: {data}','data':{'foo':'bar'},'@i':{'Id':145,'Name':'FooBar'}}")]
    [InlineData("{'@t':'2016-06-07T03:44:57.8532799Z','@mt':'Data: {data}','data':{'foo':'bar'},'@i':'a145'}")]
    [InlineData("{'@t':'2016-06-07T03:44:57.8532799Z','@mt':'Hello, {User}','User':'nblumhardt'}")]
    [InlineData("{'@t':'2016-06-07T03:44:57.8532799Z','@mt':'Data: {data}','data':{'foo':'bar'},'@i':145}")]
    public void ClefParserNotStrict_Read_Success(string logLine)
    {
        string jsonLine = logLine.Replace('\'', '"');
        byte[] data = Encoding.UTF8.GetBytes(jsonLine);
        Services.Contracts.LogEntity logEntry = ClefParser.Read(data, false);

        Assert.NotNull(logEntry);
    }

    [Fact]
    public async Task ClefParser_Write_Success()
    {
        Services.Contracts.LogEntity entry = new Services.Contracts.LogEntity()
        {
            EventId = "4589",
            Exception = "Exception",
            Level = "Info",
            LevelNumeric = 3,
            Message = "Hello John!",
            MessageTemplate = "Hello {user}!",
            Timestamp = new DateTimeOffset(2022, 8, 1, 16, 25, 43, TimeSpan.Zero),
            Properties = new Services.Contracts.LogEntityProperty[]
            {
                new Services.Contracts.LogEntityProperty("user", "John"),
                new Services.Contracts.LogEntityProperty("Application", "Area52.Test"),
                new Services.Contracts.LogEntityProperty("Count", 12),
                new Services.Contracts.LogEntityProperty("Context","ctx"),
            }
        };

        using MemoryStream ms = new MemoryStream();
        await ClefParser.Write(entry, ms);

        Assert.NotEqual(0L, ms.Position);
    }
}
