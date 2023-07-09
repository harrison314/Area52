using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Area52.Infrastructure.Clef;
using Area52.Services.Configuration;
using Area52.Services.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Area52.UnitTests.Infrastructure.Clef;

public class EventMiddlewareTests
{
    [Fact]
    public async Task EventMiddleware_ReadEmpty_Logs()
    {
        string content = "";
        IReadOnlyList<LogEntity>? result = await this.ExecuteMidlware(content);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task EventMiddleware_ReadSingle_Logs()
    {
        string content = "{'@t':'2022-07-14T19:03:38.3270683+00:00','@m':'Start sleep to: 4539 ms','@mt':'Start sleep to: {sleepTime} ms','@l':'Debug','sleepTime':4539,'SourceContext':'Area52.LogProducer.SimpleHostedService','0':29,'Scope':['Run 29']}";
        IReadOnlyList<LogEntity>? result = await this.ExecuteMidlware(content.Replace('\'', '"'));

        Assert.NotNull(result);
        Assert.Equal(1, result!.Count);
    }

    [Fact]
    public async Task EventMiddleware_ReadSingleWithNewLine_Logs()
    {
        string content = "{'@t':'2022-07-14T19:03:38.3270683+00:00','@m':'Start sleep to: 4539 ms','@mt':'Start sleep to: {sleepTime} ms','@l':'Debug','sleepTime':4539,'SourceContext':'Area52.LogProducer.SimpleHostedService','0':29,'Scope':['Run 29']}\n";
        IReadOnlyList<LogEntity>? result = await this.ExecuteMidlware(content.Replace('\'', '"'));

        Assert.NotNull(result);
        Assert.Equal(1, result!.Count);
    }

    [Fact]
    public async Task EventMiddleware_ReadTree_Logs()
    {
        string content = @"{'@t':'2022-07-14T19:03:38.3270683+00:00','@m':'Start sleep to: 4539 ms','@mt':'Start sleep to: {sleepTime} ms','@l':'Debug','sleepTime':4539,'SourceContext':'Area52.LogProducer.SimpleHostedService','0':29,'Scope':['Run 29']}
{'@t':'2016-06-07T03:44:57.8532899Z','@mt':'Data: {data}','data':true}
{'@t':'2016-06-07T03:44:57.8552799Z','@mt':'Data: {data}','data':{}}
";
        IReadOnlyList<LogEntity>? result = await this.ExecuteMidlware(content.Replace('\'', '"'));

        Assert.NotNull(result);
        Assert.Equal(3, result!.Count);
    }


    [Fact]
    public async Task EventMiddleware_ReadMore_Logs()
    {
        string content = @"{'@t':'2023-07-09T11:28:00.3382597+00:00','@m':'Used memory: 97824768 with user: JohnDoe.','@mt':'Used memory: {workingSet} with user: {user}.','@l':'informational','workingSet':97824768,'user':'JohnDoe','SourceContext':'Area52.LogProducer.SimpleHostedService','0':17,'Scope':'[\u0022Run 17\u0022]'}
{'@t':'2023-07-09T11:28:00.3382885+00:00','@m':'Any warning log. ThreadID 17.','@mt':'Any warning log. ThreadID {ThreadId}.','@l':'Warning','ThreadId':17,'SourceContext':'Area52.LogProducer.SimpleHostedService','0':17,'Scope':'[\u0022Run 17\u0022]'}
{'@t':'2023-07-09T11:28:00.3382996+00:00','@m':'Example error log.','@mt':'Example error log.','@l':'Error','@x':'System.IO.InvalidDataException: Random invalid exacption.','SourceContext':'Area52.LogProducer.SimpleHostedService','0':17,'Scope':'[\u0022Run 17\u0022]'}
{'@t':'2023-07-09T11:28:00.3383099+00:00','@m':'Example critical log.','@mt':'Example critical log.','@l':'Fatal','SourceContext':'Area52.LogProducer.SimpleHostedService','0':17,'Scope':'[\u0022Run 17\u0022]'}
{'@t':'2023-07-09T11:28:00.3383301+00:00','@m':'Start sleep to: 3969 ms','@mt':'Start sleep to: {sleepTime} ms','@l':'Debug','sleepTime':3969,'SourceContext':'Area52.LogProducer.SimpleHostedService','0':18,'Scope':'[\u0022Run 18\u0022]'}
{'@t':'2023-07-09T11:28:00.3378393+00:00','@m':'Example debug log with 18.','@mt':'Example debug log with {RunNumber}.','@l':'Debug','RunNumber':18,'SourceContext':'Area52.LogProducer.SimpleHostedService','0':17,'Scope':'[\u0022Run 17\u0022]'}
{'@t':'2023-07-09T11:28:00.2895594+00:00','@m':'Starting log trace with guid 8bbb838b-3391-4c10-9ad0-1b1a2a04292e.','@mt':'Starting log trace with guid {logId}.','@l':'Verbose','logId':'8bbb838b-3391-4c10-9ad0-1b1a2a04292e','SourceContext':'Area52.LogProducer.SimpleHostedService','0':17,'Scope':'[\u0022Run 17\u0022]'}
{'@t':'2023-07-09T11:27:57.0383462+00:00','@m':'Example debug log with 17.','@mt':'Example debug log with {RunNumber}.','@l':'Debug','RunNumber':17,'SourceContext':'Area52.LogProducer.SimpleHostedService','0':16,'Scope':'[\u0022Run 16\u0022]'}
{'@t':'2023-07-09T11:27:57.0387301+00:00','@m':'Used memory: 97816576 with user: JohnDoe.','@mt':'Used memory: {workingSet} with user: {user}.','@l':'informational','workingSet':97816576,'user':'JohnDoe','SourceContext':'Area52.LogProducer.SimpleHostedService','0':16,'Scope':'[\u0022Run 16\u0022]'}
{'@t':'2023-07-09T11:27:57.0387977+00:00','@m':'Any warning log. ThreadID 19.','@mt':'Any warning log. ThreadID {ThreadId}.','@l':'Warning','ThreadId':19,'SourceContext':'Area52.LogProducer.SimpleHostedService','0':16,'Scope':'[\u0022Run 16\u0022]'}
{'@t':'2023-07-09T11:27:57.0388191+00:00','@m':'Example error log.','@mt':'Example error log.','@l':'Error','@x':'System.IO.InvalidDataException: Random invalid exacption.','SourceContext':'Area52.LogProducer.SimpleHostedService','0':16,'Scope':'[\u0022Run 16\u0022]'}
{'@t':'2023-07-09T11:27:57.0388346+00:00','@m':'Example critical log.','@mt':'Example critical log.','@l':'Fatal','SourceContext':'Area52.LogProducer.SimpleHostedService','0':16,'Scope':'[\u0022Run 16\u0022]'}
{'@t':'2023-07-09T11:27:57.0388677+00:00','@m':'Start sleep to: 3240 ms','@mt':'Start sleep to: {sleepTime} ms','@l':'Debug','sleepTime':3240,'SourceContext':'Area52.LogProducer.SimpleHostedService','0':17,'Scope':'[\u0022Run 17\u0022]'}
{'@t':'2023-07-09T11:27:57.0018669+00:00','@m':'Starting log trace with guid 4cc8fb01-ba4c-4909-95ff-80bb9f9a4f41.','@mt':'Starting log trace with guid {logId}.','@l':'Verbose','logId':'4cc8fb01-ba4c-4909-95ff-80bb9f9a4f41','SourceContext':'Area52.LogProducer.SimpleHostedService','0':16,'Scope':'[\u0022Run 16\u0022]'}
{'@t':'2023-07-09T11:27:52.9525452+00:00','@m':'Example debug log with 16.','@mt':'Example debug log with {RunNumber}.','@l':'Debug','RunNumber':16,'SourceContext':'Area52.LogProducer.SimpleHostedService','0':15,'Scope':'[\u0022Run 15\u0022]'}
{'@t':'2023-07-09T11:27:52.952818+00:00','@m':'Used memory: 97828864 with user: JohnDoe.','@mt':'Used memory: {workingSet} with user: {user}.','@l':'informational','workingSet':97828864,'user':'JohnDoe','SourceContext':'Area52.LogProducer.SimpleHostedService','0':15,'Scope':'[\u0022Run 15\u0022]'}
{'@t':'2023-07-09T11:27:52.9528299+00:00','@m':'Any warning log. ThreadID 17.','@mt':'Any warning log. ThreadID {ThreadId}.','@l':'Warning','ThreadId':17,'SourceContext':'Area52.LogProducer.SimpleHostedService','0':15,'Scope':'[\u0022Run 15\u0022]'}
{'@t':'2023-07-09T11:27:52.9528355+00:00','@m':'Example error log.','@mt':'Example error log.','@l':'Error','@x':'System.IO.InvalidDataException: Random invalid exacption.','SourceContext':'Area52.LogProducer.SimpleHostedService','0':15,'Scope':'[\u0022Run 15\u0022]'}
{'@t':'2023-07-09T11:27:52.952839+00:00','@m':'Example critical log.','@mt':'Example critical log.','@l':'Fatal','SourceContext':'Area52.LogProducer.SimpleHostedService','0':15,'Scope':'[\u0022Run 15\u0022]'}
{'@t':'2023-07-09T11:27:52.9528772+00:00','@m':'Start sleep to: 4037 ms','@mt':'Start sleep to: {sleepTime} ms','@l':'Debug','sleepTime':4037,'SourceContext':'Area52.LogProducer.SimpleHostedService','0':16,'Scope':'[\u0022Run 16\u0022]'}
{'@t':'2023-07-09T11:27:52.9218778+00:00','@m':'Starting log trace with guid e63268f0-f287-4d5d-9642-fbbf6216a623.','@mt':'Starting log trace with guid {logId}.','@l':'Verbose','logId':'e63268f0-f287-4d5d-9642-fbbf6216a623','SourceContext':'Area52.LogProducer.SimpleHostedService','0':15,'Scope':'[\u0022Run 15\u0022]'}
{'@t':'2023-07-09T11:27:48.8192242+00:00','@m':'Example debug log with 15.','@mt':'Example debug log with {RunNumber}.','@l':'Debug','RunNumber':15,'SourceContext':'Area52.LogProducer.SimpleHostedService','0':14,'Scope':'[\u0022Run 14\u0022]'}
{'@t':'2023-07-09T11:27:48.8195013+00:00','@m':'Used memory: 97771520 with user: JohnDoe.','@mt':'Used memory: {workingSet} with user: {user}.','@l':'informational','workingSet':97771520,'user':'JohnDoe','SourceContext':'Area52.LogProducer.SimpleHostedService','0':14,'Scope':'[\u0022Run 14\u0022]'}
{'@t':'2023-07-09T11:27:48.8195158+00:00','@m':'Any warning log. ThreadID 16.','@mt':'Any warning log. ThreadID {ThreadId}.','@l':'Warning','ThreadId':16,'SourceContext':'Area52.LogProducer.SimpleHostedService','0':14,'Scope':'[\u0022Run 14\u0022]'}
{'@t':'2023-07-09T11:27:48.819522+00:00','@m':'Example error log.','@mt':'Example error log.','@l':'Error','@x':'System.IO.InvalidDataException: Random invalid exacption.','SourceContext':'Area52.LogProducer.SimpleHostedService','0':14,'Scope':'[\u0022Run 14\u0022]'}
{'@t':'2023-07-09T11:27:48.8195285+00:00','@m':'Example critical log.','@mt':'Example critical log.','@l':'Fatal','SourceContext':'Area52.LogProducer.SimpleHostedService','0':14,'Scope':'[\u0022Run 14\u0022]'}
{'@t':'2023-07-09T11:27:48.8195446+00:00','@m':'Start sleep to: 4095 ms','@mt':'Start sleep to: {sleepTime} ms','@l':'Debug','sleepTime':4095,'SourceContext':'Area52.LogProducer.SimpleHostedService','0':15,'Scope':'[\u0022Run 15\u0022]'}
{'@t':'2023-07-09T11:27:48.7885514+00:00','@m':'Starting log trace with guid 59ec6a31-5ed4-4b93-9f44-e9ef4e051481.','@mt':'Starting log trace with guid {logId}.','@l':'Verbose','logId':'59ec6a31-5ed4-4b93-9f44-e9ef4e051481','SourceContext':'Area52.LogProducer.SimpleHostedService','0':14,'Scope':'[\u0022Run 14\u0022]'}
{'@t':'2023-07-09T11:27:48.1233239+00:00','@m':'Used memory: 97763328 with user: JohnDoe.','@mt':'Used memory: {workingSet} with user: {user}.','@l':'informational','workingSet':97763328,'user':'JohnDoe','SourceContext':'Area52.LogProducer.SimpleHostedService','0':13,'Scope':'[\u0022Run 13\u0022]'}
{'@t':'2023-07-09T11:27:48.1233413+00:00','@m':'Any warning log. ThreadID 16.','@mt':'Any warning log. ThreadID {ThreadId}.','@l':'Warning','ThreadId':16,'SourceContext':'Area52.LogProducer.SimpleHostedService','0':13,'Scope':'[\u0022Run 13\u0022]'}
{'@t':'2023-07-09T11:27:48.1233487+00:00','@m':'Example error log.','@mt':'Example error log.','@l':'Error','@x':'System.IO.InvalidDataException: Random invalid exacption.','SourceContext':'Area52.LogProducer.SimpleHostedService','0':13,'Scope':'[\u0022Run 13\u0022]'}
{'@t':'2023-07-09T11:27:48.123353+00:00','@m':'Example critical log.','@mt':'Example critical log.','@l':'Fatal','SourceContext':'Area52.LogProducer.SimpleHostedService','0':13,'Scope':'[\u0022Run 13\u0022]'}
{'@t':'2023-07-09T11:27:48.1233643+00:00','@m':'Start sleep to: 642 ms','@mt':'Start sleep to: {sleepTime} ms','@l':'Debug','sleepTime':642,'SourceContext':'Area52.LogProducer.SimpleHostedService','0':14,'Scope':'[\u0022Run 14\u0022]'}
{'@t':'2023-07-09T11:27:48.1229962+00:00','@m':'Example debug log with 14.','@mt':'Example debug log with {RunNumber}.','@l':'Debug','RunNumber':14,'SourceContext':'Area52.LogProducer.SimpleHostedService','0':13,'Scope':'[\u0022Run 13\u0022]'}
{'@t':'2023-07-09T11:27:48.0872904+00:00','@m':'Starting log trace with guid 60322819-3f74-4fba-bbe8-a2fbbafc6b35.','@mt':'Starting log trace with guid {logId}.','@l':'Verbose','logId':'60322819-3f74-4fba-bbe8-a2fbbafc6b35','SourceContext':'Area52.LogProducer.SimpleHostedService','0':13,'Scope':'[\u0022Run 13\u0022]'}
{'@t':'2023-07-09T11:27:45.1380287+00:00','@m':'Used memory: 97722368 with user: JohnDoe.','@mt':'Used memory: {workingSet} with user: {user}.','@l':'informational','workingSet':97722368,'user':'JohnDoe','SourceContext':'Area52.LogProducer.SimpleHostedService','0':12,'Scope':'[\u0022Run 12\u0022]'}
{'@t':'2023-07-09T11:27:45.1380521+00:00','@m':'Any warning log. ThreadID 16.','@mt':'Any warning log. ThreadID {ThreadId}.','@l':'Warning','ThreadId':16,'SourceContext':'Area52.LogProducer.SimpleHostedService','0':12,'Scope':'[\u0022Run 12\u0022]'}
{'@t':'2023-07-09T11:27:45.1381098+00:00','@m':'Example error log.','@mt':'Example error log.','@l':'Error','@x':'System.IO.InvalidDataException: Random invalid exacption.','SourceContext':'Area52.LogProducer.SimpleHostedService','0':12,'Scope':'[\u0022Run 12\u0022]'}
{'@t':'2023-07-09T11:27:45.1381186+00:00','@m':'Example critical log.','@mt':'Example critical log.','@l':'Fatal','SourceContext':'Area52.LogProducer.SimpleHostedService','0':12,'Scope':'[\u0022Run 12\u0022]'}
{'@t':'2023-07-09T11:27:45.1381351+00:00','@m':'Start sleep to: 2949 ms','@mt':'Start sleep to: {sleepTime} ms','@l':'Debug','sleepTime':2949,'SourceContext':'Area52.LogProducer.SimpleHostedService','0':13,'Scope':'[\u0022Run 13\u0022]'}
{'@t':'2023-07-09T11:27:45.1377561+00:00','@m':'Example debug log with 13.','@mt':'Example debug log with {RunNumber}.','@l':'Debug','RunNumber':13,'SourceContext':'Area52.LogProducer.SimpleHostedService','0':12,'Scope':'[\u0022Run 12\u0022]'}
{'@t':'2023-07-09T11:27:45.1051625+00:00','@m':'Starting log trace with guid 587fd6d1-d773-499b-9a31-1d643c1ea13c.','@mt':'Starting log trace with guid {logId}.','@l':'Verbose','logId':'587fd6d1-d773-499b-9a31-1d643c1ea13c','SourceContext':'Area52.LogProducer.SimpleHostedService','0':12,'Scope':'[\u0022Run 12\u0022]'}
{'@t':'2023-07-09T11:27:40.1490253+00:00','@m':'Start sleep to: 4953 ms','@mt':'Start sleep to: {sleepTime} ms','@l':'Debug','sleepTime':4953,'SourceContext':'Area52.LogProducer.SimpleHostedService','0':12,'Scope':'[\u0022Run 12\u0022]'}
{'@t':'2023-07-09T11:27:40.148941+00:00','@m':'Used memory: 97628160 with user: JohnDoe.','@mt':'Used memory: {workingSet} with user: {user}.','@l':'informational','workingSet':97628160,'user':'JohnDoe','SourceContext':'Area52.LogProducer.SimpleHostedService','0':11,'Scope':'[\u0022Run 11\u0022]'}
{'@t':'2023-07-09T11:27:40.1489576+00:00','@m':'Any warning log. ThreadID 16.','@mt':'Any warning log. ThreadID {ThreadId}.','@l':'Warning','ThreadId':16,'SourceContext':'Area52.LogProducer.SimpleHostedService','0':11,'Scope':'[\u0022Run 11\u0022]'}
{'@t':'2023-07-09T11:27:40.1489865+00:00','@m':'Example error log.','@mt':'Example error log.','@l':'Error','@x':'System.IO.InvalidDataException: Random invalid exacption.','SourceContext':'Area52.LogProducer.SimpleHostedService','0':11,'Scope':'[\u0022Run 11\u0022]'}
{'@t':'2023-07-09T11:27:40.1489907+00:00','@m':'Example critical log.','@mt':'Example critical log.','@l':'Fatal','SourceContext':'Area52.LogProducer.SimpleHostedService','0':11,'Scope':'[\u0022Run 11\u0022]'}
{'@t':'2023-07-09T11:27:40.1476975+00:00','@m':'Example debug log with 12.','@mt':'Example debug log with {RunNumber}.','@l':'Debug','RunNumber':12,'SourceContext':'Area52.LogProducer.SimpleHostedService','0':11,'Scope':'[\u0022Run 11\u0022]'}
{'@t':'2023-07-09T11:27:40.1159579+00:00','@m':'Starting log trace with guid 24141e94-4900-46ad-ab28-7377d56e1956.','@mt':'Starting log trace with guid {logId}.','@l':'Verbose','logId':'24141e94-4900-46ad-ab28-7377d56e1956','SourceContext':'Area52.LogProducer.SimpleHostedService','0':11,'Scope':'[\u0022Run 11\u0022]'}
{'@t':'2023-07-09T11:27:39.264254+00:00','@m':'Example debug log with 11.','@mt':'Example debug log with {RunNumber}.','@l':'Debug','RunNumber':11,'SourceContext':'Area52.LogProducer.SimpleHostedService','0':10,'Scope':'[\u0022Run 10\u0022]'}
{'@t':'2023-07-09T11:27:39.2645179+00:00','@m':'Used memory: 97599488 with user: JohnDoe.','@mt':'Used memory: {workingSet} with user: {user}.','@l':'informational','workingSet':97599488,'user':'JohnDoe','SourceContext':'Area52.LogProducer.SimpleHostedService','0':10,'Scope':'[\u0022Run 10\u0022]'}
{'@t':'2023-07-09T11:27:39.2645502+00:00','@m':'Any warning log. ThreadID 17.','@mt':'Any warning log. ThreadID {ThreadId}.','@l':'Warning','ThreadId':17,'SourceContext':'Area52.LogProducer.SimpleHostedService','0':10,'Scope':'[\u0022Run 10\u0022]'}
{'@t':'2023-07-09T11:27:39.2645572+00:00','@m':'Example error log.','@mt':'Example error log.','@l':'Error','@x':'System.IO.InvalidDataException: Random invalid exacption.','SourceContext':'Area52.LogProducer.SimpleHostedService','0':10,'Scope':'[\u0022Run 10\u0022]'}
{'@t':'2023-07-09T11:27:39.2645612+00:00','@m':'Example critical log.','@mt':'Example critical log.','@l':'Fatal','SourceContext':'Area52.LogProducer.SimpleHostedService','0':10,'Scope':'[\u0022Run 10\u0022]'}
{'@t':'2023-07-09T11:27:39.2645916+00:00','@m':'Start sleep to: 838 ms','@mt':'Start sleep to: {sleepTime} ms','@l':'Debug','sleepTime':838,'SourceContext':'Area52.LogProducer.SimpleHostedService','0':11,'Scope':'[\u0022Run 11\u0022]'}
{'@t':'2023-07-09T11:27:39.2336145+00:00','@m':'Starting log trace with guid 8c94f4ad-779a-4d7f-b327-fcc9c17e3cf7.','@mt':'Starting log trace with guid {logId}.','@l':'Verbose','logId':'8c94f4ad-779a-4d7f-b327-fcc9c17e3cf7','SourceContext':'Area52.LogProducer.SimpleHostedService','0':10,'Scope':'[\u0022Run 10\u0022]'}
{'@t':'2023-07-09T11:27:37.2795248+00:00','@m':'Example debug log with 10.','@mt':'Example debug log with {RunNumber}.','@l':'Debug','RunNumber':10,'SourceContext':'Area52.LogProducer.SimpleHostedService','0':9,'Scope':'[\u0022Run 9\u0022]'}
{'@t':'2023-07-09T11:27:37.2797826+00:00','@m':'Used memory: 97558528 with user: JohnDoe.','@mt':'Used memory: {workingSet} with user: {user}.','@l':'informational','workingSet':97558528,'user':'JohnDoe','SourceContext':'Area52.LogProducer.SimpleHostedService','0':9,'Scope':'[\u0022Run 9\u0022]'}
{'@t':'2023-07-09T11:27:37.279814+00:00','@m':'Any warning log. ThreadID 17.','@mt':'Any warning log. ThreadID {ThreadId}.','@l':'Warning','ThreadId':17,'SourceContext':'Area52.LogProducer.SimpleHostedService','0':9,'Scope':'[\u0022Run 9\u0022]'}
{'@t':'2023-07-09T11:27:37.27985+00:00','@m':'Example error log.','@mt':'Example error log.','@l':'Error','@x':'System.IO.InvalidDataException: Random invalid exacption.','SourceContext':'Area52.LogProducer.SimpleHostedService','0':9,'Scope':'[\u0022Run 9\u0022]'}
{'@t':'2023-07-09T11:27:37.2798687+00:00','@m':'Example critical log.','@mt':'Example critical log.','@l':'Fatal','SourceContext':'Area52.LogProducer.SimpleHostedService','0':9,'Scope':'[\u0022Run 9\u0022]'}
{'@t':'2023-07-09T11:27:37.2799084+00:00','@m':'Start sleep to: 1951 ms','@mt':'Start sleep to: {sleepTime} ms','@l':'Debug','sleepTime':1951,'SourceContext':'Area52.LogProducer.SimpleHostedService','0':10,'Scope':'[\u0022Run 10\u0022]'}
{'@t':'2023-07-09T11:27:37.2523429+00:00','@m':'Starting log trace with guid 63485615-429d-4927-a030-6aa8e9cac535.','@mt':'Starting log trace with guid {logId}.','@l':'Verbose','logId':'63485615-429d-4927-a030-6aa8e9cac535','SourceContext':'Area52.LogProducer.SimpleHostedService','0':9,'Scope':'[\u0022Run 9\u0022]'}
{'@t':'2023-07-09T11:27:33.4461285+00:00','@m':'Used memory: 97603584 with user: JohnDoe.','@mt':'Used memory: {workingSet} with user: {user}.','@l':'informational','workingSet':97603584,'user':'JohnDoe','SourceContext':'Area52.LogProducer.SimpleHostedService','0':8,'Scope':'[\u0022Run 8\u0022]'}
{'@t':'2023-07-09T11:27:33.4461426+00:00','@m':'Any warning log. ThreadID 16.','@mt':'Any warning log. ThreadID {ThreadId}.','@l':'Warning','ThreadId':16,'SourceContext':'Area52.LogProducer.SimpleHostedService','0':8,'Scope':'[\u0022Run 8\u0022]'}
{'@t':'2023-07-09T11:27:33.4461484+00:00','@m':'Example error log.','@mt':'Example error log.','@l':'Error','@x':'System.IO.InvalidDataException: Random invalid exacption.','SourceContext':'Area52.LogProducer.SimpleHostedService','0':8,'Scope':'[\u0022Run 8\u0022]'}
{'@t':'2023-07-09T11:27:33.4461606+00:00','@m':'Example critical log.','@mt':'Example critical log.','@l':'Fatal','SourceContext':'Area52.LogProducer.SimpleHostedService','0':8,'Scope':'[\u0022Run 8\u0022]'}
{'@t':'2023-07-09T11:27:33.4461718+00:00','@m':'Start sleep to: 3795 ms','@mt':'Start sleep to: {sleepTime} ms','@l':'Debug','sleepTime':3795,'SourceContext':'Area52.LogProducer.SimpleHostedService','0':9,'Scope':'[\u0022Run 9\u0022]'}
{'@t':'2023-07-09T11:27:33.4458139+00:00','@m':'Example debug log with 9.','@mt':'Example debug log with {RunNumber}.','@l':'Debug','RunNumber':9,'SourceContext':'Area52.LogProducer.SimpleHostedService','0':8,'Scope':'[\u0022Run 8\u0022]'}
{'@t':'2023-07-09T11:27:33.4123255+00:00','@m':'Starting log trace with guid ce9bbe0a-5290-4b4a-9840-c8ed62501a79.','@mt':'Starting log trace with guid {logId}.','@l':'Verbose','logId':'ce9bbe0a-5290-4b4a-9840-c8ed62501a79','SourceContext':'Area52.LogProducer.SimpleHostedService','0':8,'Scope':'[\u0022Run 8\u0022]'}
{'@t':'2023-07-09T11:27:30.1744473+00:00','@m':'Example debug log with 8.','@mt':'Example debug log with {RunNumber}.','@l':'Debug','RunNumber':8,'SourceContext':'Area52.LogProducer.SimpleHostedService','0':7,'Scope':'[\u0022Run 7\u0022]'}
{'@t':'2023-07-09T11:27:30.1747324+00:00','@m':'Used memory: 98299904 with user: JohnDoe.','@mt':'Used memory: {workingSet} with user: {user}.','@l':'informational','workingSet':98299904,'user':'JohnDoe','SourceContext':'Area52.LogProducer.SimpleHostedService','0':7,'Scope':'[\u0022Run 7\u0022]'}
{'@t':'2023-07-09T11:27:30.1747452+00:00','@m':'Any warning log. ThreadID 16.','@mt':'Any warning log. ThreadID {ThreadId}.','@l':'Warning','ThreadId':16,'SourceContext':'Area52.LogProducer.SimpleHostedService','0':7,'Scope':'[\u0022Run 7\u0022]'}
{'@t':'2023-07-09T11:27:30.1747521+00:00','@m':'Example error log.','@mt':'Example error log.','@l':'Error','@x':'System.IO.InvalidDataException: Random invalid exacption.','SourceContext':'Area52.LogProducer.SimpleHostedService','0':7,'Scope':'[\u0022Run 7\u0022]'}
{'@t':'2023-07-09T11:27:30.1747562+00:00','@m':'Example critical log.','@mt':'Example critical log.','@l':'Fatal','SourceContext':'Area52.LogProducer.SimpleHostedService','0':7,'Scope':'[\u0022Run 7\u0022]'}
{'@t':'2023-07-09T11:27:30.1747723+00:00','@m':'Start sleep to: 3220 ms','@mt':'Start sleep to: {sleepTime} ms','@l':'Debug','sleepTime':3220,'SourceContext':'Area52.LogProducer.SimpleHostedService','0':8,'Scope':'[\u0022Run 8\u0022]'}
{'@t':'2023-07-09T11:27:30.1429199+00:00','@m':'Starting log trace with guid 4b486110-f6a1-45d5-af3c-b49b54a7885b.','@mt':'Starting log trace with guid {logId}.','@l':'Verbose','logId':'4b486110-f6a1-45d5-af3c-b49b54a7885b','SourceContext':'Area52.LogProducer.SimpleHostedService','0':7,'Scope':'[\u0022Run 7\u0022]'}
{'@t':'2023-07-09T11:27:26.9950981+00:00','@m':'Example debug log with 7.','@mt':'Example debug log with {RunNumber}.','@l':'Debug','RunNumber':7,'SourceContext':'Area52.LogProducer.SimpleHostedService','0':6,'Scope':'[\u0022Run 6\u0022]'}
{'@t':'2023-07-09T11:27:26.9956736+00:00','@m':'Used memory: 98283520 with user: JohnDoe.','@mt':'Used memory: {workingSet} with user: {user}.','@l':'informational','workingSet':98283520,'user':'JohnDoe','SourceContext':'Area52.LogProducer.SimpleHostedService','0':6,'Scope':'[\u0022Run 6\u0022]'}
{'@t':'2023-07-09T11:27:26.9958743+00:00','@m':'Any warning log. ThreadID 14.','@mt':'Any warning log. ThreadID {ThreadId}.','@l':'Warning','ThreadId':14,'SourceContext':'Area52.LogProducer.SimpleHostedService','0':6,'Scope':'[\u0022Run 6\u0022]'}
{'@t':'2023-07-09T11:27:26.9958893+00:00','@m':'Example error log.','@mt':'Example error log.','@l':'Error','@x':'System.IO.InvalidDataException: Random invalid exacption.','SourceContext':'Area52.LogProducer.SimpleHostedService','0':6,'Scope':'[\u0022Run 6\u0022]'}
{'@t':'2023-07-09T11:27:26.9959098+00:00','@m':'Example critical log.','@mt':'Example critical log.','@l':'Fatal','SourceContext':'Area52.LogProducer.SimpleHostedService','0':6,'Scope':'[\u0022Run 6\u0022]'}
{'@t':'2023-07-09T11:27:26.995936+00:00','@m':'Start sleep to: 3141 ms','@mt':'Start sleep to: {sleepTime} ms','@l':'Debug','sleepTime':3141,'SourceContext':'Area52.LogProducer.SimpleHostedService','0':7,'Scope':'[\u0022Run 7\u0022]'}
{'@t':'2023-07-09T11:27:26.9459214+00:00','@m':'Starting log trace with guid 8743071f-964b-437b-9226-3772b795bbc6.','@mt':'Starting log trace with guid {logId}.','@l':'Verbose','logId':'8743071f-964b-437b-9226-3772b795bbc6','SourceContext':'Area52.LogProducer.SimpleHostedService','0':6,'Scope':'[\u0022Run 6\u0022]'}
{'@t':'2023-07-09T11:27:24.4444752+00:00','@m':'Example debug log with 6.','@mt':'Example debug log with {RunNumber}.','@l':'Debug','RunNumber':6,'SourceContext':'Area52.LogProducer.SimpleHostedService','0':5,'Scope':'[\u0022Run 5\u0022]'}
{'@t':'2023-07-09T11:27:24.4449058+00:00','@m':'Used memory: 98209792 with user: JohnDoe.','@mt':'Used memory: {workingSet} with user: {user}.','@l':'informational','workingSet':98209792,'user':'JohnDoe','SourceContext':'Area52.LogProducer.SimpleHostedService','0':5,'Scope':'[\u0022Run 5\u0022]'}
{'@t':'2023-07-09T11:27:24.4449191+00:00','@m':'Any warning log. ThreadID 14.','@mt':'Any warning log. ThreadID {ThreadId}.','@l':'Warning','ThreadId':14,'SourceContext':'Area52.LogProducer.SimpleHostedService','0':5,'Scope':'[\u0022Run 5\u0022]'}
{'@t':'2023-07-09T11:27:24.4449248+00:00','@m':'Example error log.','@mt':'Example error log.','@l':'Error','@x':'System.IO.InvalidDataException: Random invalid exacption.','SourceContext':'Area52.LogProducer.SimpleHostedService','0':5,'Scope':'[\u0022Run 5\u0022]'}
{'@t':'2023-07-09T11:27:24.444949+00:00','@m':'Example critical log.','@mt':'Example critical log.','@l':'Fatal','SourceContext':'Area52.LogProducer.SimpleHostedService','0':5,'Scope':'[\u0022Run 5\u0022]'}
{'@t':'2023-07-09T11:27:24.4449898+00:00','@m':'Start sleep to: 2472 ms','@mt':'Start sleep to: {sleepTime} ms','@l':'Debug','sleepTime':2472,'SourceContext':'Area52.LogProducer.SimpleHostedService','0':6,'Scope':'[\u0022Run 6\u0022]'}
{'@t':'2023-07-09T11:27:24.4111406+00:00','@m':'Starting log trace with guid aab1b63e-be56-4c2b-b446-399a5421d471.','@mt':'Starting log trace with guid {logId}.','@l':'Verbose','logId':'aab1b63e-be56-4c2b-b446-399a5421d471','SourceContext':'Area52.LogProducer.SimpleHostedService','0':5,'Scope':'[\u0022Run 5\u0022]'}
{'@t':'2023-07-09T11:27:23.7631131+00:00','@m':'Used memory: 97972224 with user: JohnDoe.','@mt':'Used memory: {workingSet} with user: {user}.','@l':'informational','workingSet':97972224,'user':'JohnDoe','SourceContext':'Area52.LogProducer.SimpleHostedService','0':4,'Scope':'[\u0022Run 4\u0022]'}
{'@t':'2023-07-09T11:27:23.7631546+00:00','@m':'Any warning log. ThreadID 14.','@mt':'Any warning log. ThreadID {ThreadId}.','@l':'Warning','ThreadId':14,'SourceContext':'Area52.LogProducer.SimpleHostedService','0':4,'Scope':'[\u0022Run 4\u0022]'}
{'@t':'2023-07-09T11:27:23.7631695+00:00','@m':'Example error log.','@mt':'Example error log.','@l':'Error','@x':'System.IO.InvalidDataException: Random invalid exacption.','SourceContext':'Area52.LogProducer.SimpleHostedService','0':4,'Scope':'[\u0022Run 4\u0022]'}
{'@t':'2023-07-09T11:27:23.7631737+00:00','@m':'Example critical log.','@mt':'Example critical log.','@l':'Fatal','SourceContext':'Area52.LogProducer.SimpleHostedService','0':4,'Scope':'[\u0022Run 4\u0022]'}
{'@t':'2023-07-09T11:27:23.7631847+00:00','@m':'Start sleep to: 645 ms','@mt':'Start sleep to: {sleepTime} ms','@l':'Debug','sleepTime':645,'SourceContext':'Area52.LogProducer.SimpleHostedService','0':5,'Scope':'[\u0022Run 5\u0022]'}
{'@t':'2023-07-09T11:27:23.7628584+00:00','@m':'Example debug log with 5.','@mt':'Example debug log with {RunNumber}.','@l':'Debug','RunNumber':5,'SourceContext':'Area52.LogProducer.SimpleHostedService','0':4,'Scope':'[\u0022Run 4\u0022]'}
{'@t':'2023-07-09T11:27:23.7300101+00:00','@m':'Starting log trace with guid 708dac14-7aab-4e89-840d-f6da6c41fece.','@mt':'Starting log trace with guid {logId}.','@l':'Verbose','logId':'708dac14-7aab-4e89-840d-f6da6c41fece','SourceContext':'Area52.LogProducer.SimpleHostedService','0':4,'Scope':'[\u0022Run 4\u0022]'}
{'@t':'2023-07-09T11:27:20.1310342+00:00','@m':'Example debug log with 4.','@mt':'Example debug log with {RunNumber}.','@l':'Debug','RunNumber':4,'SourceContext':'Area52.LogProducer.SimpleHostedService','0':3,'Scope':'[\u0022Run 3\u0022]'}
{'@t':'2023-07-09T11:27:20.1313567+00:00','@m':'Used memory: 98144256 with user: JohnDoe.','@mt':'Used memory: {workingSet} with user: {user}.','@l':'informational','workingSet':98144256,'user':'JohnDoe','SourceContext':'Area52.LogProducer.SimpleHostedService','0':3,'Scope':'[\u0022Run 3\u0022]'}
{'@t':'2023-07-09T11:27:20.131377+00:00','@m':'Any warning log. ThreadID 14.','@mt':'Any warning log. ThreadID {ThreadId}.','@l':'Warning','ThreadId':14,'SourceContext':'Area52.LogProducer.SimpleHostedService','0':3,'Scope':'[\u0022Run 3\u0022]'}
{'@t':'2023-07-09T11:27:20.1313946+00:00','@m':'Example error log.','@mt':'Example error log.','@l':'Error','@x':'System.IO.InvalidDataException: Random invalid exacption.','SourceContext':'Area52.LogProducer.SimpleHostedService','0':3,'Scope':'[\u0022Run 3\u0022]'}
{'@t':'2023-07-09T11:27:20.1314073+00:00','@m':'Example critical log.','@mt':'Example critical log.','@l':'Fatal','SourceContext':'Area52.LogProducer.SimpleHostedService','0':3,'Scope':'[\u0022Run 3\u0022]'}
{'@t':'2023-07-09T11:27:20.1314266+00:00','@m':'Start sleep to: 3584 ms','@mt':'Start sleep to: {sleepTime} ms','@l':'Debug','sleepTime':3584,'SourceContext':'Area52.LogProducer.SimpleHostedService','0':4,'Scope':'[\u0022Run 4\u0022]'}
{'@t':'2023-07-09T11:27:20.0954972+00:00','@m':'Starting log trace with guid 95adb2e3-ff73-4c15-9999-3959497ee77c.','@mt':'Starting log trace with guid {logId}.','@l':'Verbose','logId':'95adb2e3-ff73-4c15-9999-3959497ee77c','SourceContext':'Area52.LogProducer.SimpleHostedService','0':3,'Scope':'[\u0022Run 3\u0022]'}
{'@t':'2023-07-09T11:27:17.865072+00:00','@m':'Example debug log with 3.','@mt':'Example debug log with {RunNumber}.','@l':'Debug','RunNumber':3,'SourceContext':'Area52.LogProducer.SimpleHostedService','0':2,'Scope':'[\u0022Run 2\u0022]'}
{'@t':'2023-07-09T11:27:17.865349+00:00','@m':'Used memory: 99999744 with user: JohnDoe.','@mt':'Used memory: {workingSet} with user: {user}.','@l':'informational','workingSet':99999744,'user':'JohnDoe','SourceContext':'Area52.LogProducer.SimpleHostedService','0':2,'Scope':'[\u0022Run 2\u0022]'}
{'@t':'2023-07-09T11:27:17.8653633+00:00','@m':'Any warning log. ThreadID 14.','@mt':'Any warning log. ThreadID {ThreadId}.','@l':'Warning','ThreadId':14,'SourceContext':'Area52.LogProducer.SimpleHostedService','0':2,'Scope':'[\u0022Run 2\u0022]'}
{'@t':'2023-07-09T11:27:17.8653703+00:00','@m':'Example error log.','@mt':'Example error log.','@l':'Error','@x':'System.IO.InvalidDataException: Random invalid exacption.','SourceContext':'Area52.LogProducer.SimpleHostedService','0':2,'Scope':'[\u0022Run 2\u0022]'}
{'@t':'2023-07-09T11:27:17.8653743+00:00','@m':'Example critical log.','@mt':'Example critical log.','@l':'Fatal','SourceContext':'Area52.LogProducer.SimpleHostedService','0':2,'Scope':'[\u0022Run 2\u0022]'}
{'@t':'2023-07-09T11:27:17.8654149+00:00','@m':'Start sleep to: 2229 ms','@mt':'Start sleep to: {sleepTime} ms','@l':'Debug','sleepTime':2229,'SourceContext':'Area52.LogProducer.SimpleHostedService','0':3,'Scope':'[\u0022Run 3\u0022]'}
{'@t':'2023-07-09T11:27:17.824905+00:00','@m':'Starting log trace with guid 89e298ee-b22a-4916-9807-0ebd3defe155.','@mt':'Starting log trace with guid {logId}.','@l':'Verbose','logId':'89e298ee-b22a-4916-9807-0ebd3defe155','SourceContext':'Area52.LogProducer.SimpleHostedService','0':2,'Scope':'[\u0022Run 2\u0022]'}
{'@t':'2023-07-09T11:27:14.1650059+00:00','@m':'Example error log.','@mt':'Example error log.','@l':'Error','@x':'System.IO.InvalidDataException: Random invalid exacption.','SourceContext':'Area52.LogProducer.SimpleHostedService','0':1,'Scope':'[\u0022Run 1\u0022]'}
{'@t':'2023-07-09T11:27:14.1650488+00:00','@m':'Example critical log.','@mt':'Example critical log.','@l':'Fatal','SourceContext':'Area52.LogProducer.SimpleHostedService','0':1,'Scope':'[\u0022Run 1\u0022]'}
{'@t':'2023-07-09T11:27:14.1651338+00:00','@m':'Start sleep to: 3637 ms','@mt':'Start sleep to: {sleepTime} ms','@l':'Debug','sleepTime':3637,'SourceContext':'Area52.LogProducer.SimpleHostedService','0':2,'Scope':'[\u0022Run 2\u0022]'}
{'@t':'2023-07-09T11:27:14.1648197+00:00','@m':'Used memory: 99450880 with user: JohnDoe.','@mt':'Used memory: {workingSet} with user: {user}.','@l':'informational','workingSet':99450880,'user':'JohnDoe','SourceContext':'Area52.LogProducer.SimpleHostedService','0':1,'Scope':'[\u0022Run 1\u0022]'}
{'@t':'2023-07-09T11:27:14.1649241+00:00','@m':'Any warning log. ThreadID 16.','@mt':'Any warning log. ThreadID {ThreadId}.','@l':'Warning','ThreadId':16,'SourceContext':'Area52.LogProducer.SimpleHostedService','0':1,'Scope':'[\u0022Run 1\u0022]'}
{'@t':'2023-07-09T11:27:14.163636+00:00','@m':'Example debug log with 2.','@mt':'Example debug log with {RunNumber}.','@l':'Debug','RunNumber':2,'SourceContext':'Area52.LogProducer.SimpleHostedService','0':1,'Scope':'[\u0022Run 1\u0022]'}
{'@t':'2023-07-09T11:27:14.115227+00:00','@m':'Starting log trace with guid b6bc1e5e-3023-48e6-a52a-b400a3c33618.','@mt':'Starting log trace with guid {logId}.','@l':'Verbose','logId':'b6bc1e5e-3023-48e6-a52a-b400a3c33618','SourceContext':'Area52.LogProducer.SimpleHostedService','0':1,'Scope':'[\u0022Run 1\u0022]'}
{'@t':'2023-07-09T11:27:11.164339+00:00','@m':'Entering to Index.','@mt':'Entering to Index.','@l':'Verbose','SourceContext':'Area52.LogProducer.Controllers.HomeController','ActionId':'9e64b571-93f3-43ba-82a2-df3e78081dd0','ActionName':'Area52.LogProducer.Controllers.HomeController.Index (Area52.LogProducer)','RequestId':'0HMS0DTNO13GH:00000001','RequestPath':'/','ConnectionId':'0HMS0DTNO13GH'}
{'@t':'2023-07-09T11:27:09.7006616+00:00','@m':'Start sleep to: 4400 ms','@mt':'Start sleep to: {sleepTime} ms','@l':'Debug','sleepTime':4400,'SourceContext':'Area52.LogProducer.SimpleHostedService','0':1,'Scope':'[\u0022Run 1\u0022]'}
{'@t':'2023-07-09T11:27:09.6971014+00:00','@m':'Start hosted service','@mt':'Start hosted service','@l':'informational','SourceContext':'Area52.LogProducer.SimpleHostedService'}
{'@t':'2023-07-01T11:27:48.123353+00:00','@m':'Example critical log.','@mt':'Example critical log.','@l':'Fatal','SourceContext':'Area52.LogProducer.SimpleHostedService','0':13}
";
        IReadOnlyList<LogEntity>? result = await this.ExecuteMidlware(content.Replace('\'', '"'));

        Assert.NotNull(result);
        Assert.Equal(123, result!.Count);
    }

    private async Task<IReadOnlyList<LogEntity>?> ExecuteMidlware(string inputContent)
    {
        using MemoryStream ms = new MemoryStream();
        ms.Write(Encoding.UTF8.GetBytes(inputContent).AsSpan());
        ms.Position = 0L;

        IReadOnlyList<LogEntity>? result = null;
        DefaultHttpContext defaultContext = new DefaultHttpContext();
        defaultContext.Request.ContentType = "application/vnd.serilog.clef";
        defaultContext.Request.Path = "/api/events/raw";
        defaultContext.Request.Method = "POST";
        defaultContext.Request.Body = new BufferedStream(ms, 500);

        Mock<IApiKeyServices> apiKeyMocks = new Mock<IApiKeyServices>(MockBehavior.Strict);
        apiKeyMocks.Setup(t => t.VerifyApiKey(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        Mock<ILogWriter> logWriter = new Mock<ILogWriter>(MockBehavior.Strict);
        logWriter.Setup(t => t.Write(It.IsNotNull<IReadOnlyList<LogEntity>>()))
            .Callback((IReadOnlyList<LogEntity> data) =>
            {
                result = data;
            })
            .Returns(Task.CompletedTask)
            .Verifiable();

        Area52Setup setup = new Area52Setup();

        EventMiddleware middlewareInstance = new EventMiddleware(next: (innerHttpContext) =>
        {
            return Task.CompletedTask;
        },
        apiKeyMocks.Object,
        logWriter.Object,
        Microsoft.Extensions.Options.Options.Create(setup),
        new NullLogger<EventMiddleware>());


        await middlewareInstance.Invoke(defaultContext);

        Assert.Equal(201, defaultContext.Response.StatusCode);
        logWriter.VerifyAll();

        return result;
    }

    [Fact]
    public async Task EventMiddleware_Unauthorized_Returns401()
    {
        string content = "{'@t':'2022-07-14T19:03:38.3270683+00:00','@m':'Start sleep to: 4539 ms','@mt':'Start sleep to: {sleepTime} ms','@l':'Debug','sleepTime':4539,'SourceContext':'Area52.LogProducer.SimpleHostedService','0':29,'Scope':['Run 29']}";
        using MemoryStream ms = new MemoryStream();
        ms.Write(Encoding.UTF8.GetBytes(content.Replace('\'', '"')).AsSpan());
        ms.Position = 0L;

        DefaultHttpContext defaultContext = new DefaultHttpContext();
        defaultContext.Request.ContentType = "application/vnd.serilog.clef";
        defaultContext.Request.Path = "/api/events/raw";
        defaultContext.Request.Method = "POST";
        defaultContext.Request.Body = ms;

        Mock<IApiKeyServices> apiKeyMocks = new Mock<IApiKeyServices>(MockBehavior.Strict);
        apiKeyMocks.Setup(t => t.VerifyApiKey(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        Mock<ILogWriter> logWriter = new Mock<ILogWriter>(MockBehavior.Strict);

        Area52Setup setup = new Area52Setup();

        EventMiddleware middlewareInstance = new EventMiddleware(next: (innerHttpContext) =>
        {
            return Task.CompletedTask;
        },
        apiKeyMocks.Object,
        logWriter.Object,
        Microsoft.Extensions.Options.Options.Create(setup),
        new NullLogger<EventMiddleware>());


        await middlewareInstance.Invoke(defaultContext);

        Assert.Equal(401, defaultContext.Response.StatusCode);
    }

    [Fact]
    public async Task EventMiddleware_BadApiKey_Returns401()
    {
        string content = "{'@t':'2022-07-14T19:03:38.3270683+00:00','@m':'Start sleep to: 4539 ms','@mt':'Start sleep to: {sleepTime} ms','@l':'Debug','sleepTime':4539,'SourceContext':'Area52.LogProducer.SimpleHostedService','0':29,'Scope':['Run 29']}";
        using MemoryStream ms = new MemoryStream();
        ms.Write(Encoding.UTF8.GetBytes(content.Replace('\'', '"')).AsSpan());
        ms.Position = 0L;

        DefaultHttpContext defaultContext = new DefaultHttpContext();
        defaultContext.Request.ContentType = "application/vnd.serilog.clef";
        defaultContext.Request.Path = "/api/events/raw";
        defaultContext.Request.Method = "POST";
        defaultContext.Request.Body = ms;
        defaultContext.Request.Headers.Remove("X-Seq-ApiKey");
        defaultContext.Request.Headers.Add("X-Seq-ApiKey", "BadApiKeyBadApiKey");

       Mock<IApiKeyServices> apiKeyMocks = new Mock<IApiKeyServices>(MockBehavior.Strict);
        apiKeyMocks.Setup(t => t.VerifyApiKey("BadApiKeyBadApiKey", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        Mock<ILogWriter> logWriter = new Mock<ILogWriter>(MockBehavior.Strict);

        Area52Setup setup = new Area52Setup();

        EventMiddleware middlewareInstance = new EventMiddleware(next: (innerHttpContext) =>
        {
            return Task.CompletedTask;
        },
        apiKeyMocks.Object,
        logWriter.Object,
        Microsoft.Extensions.Options.Options.Create(setup),
        new NullLogger<EventMiddleware>());


        await middlewareInstance.Invoke(defaultContext);

        Assert.Equal(401, defaultContext.Response.StatusCode);
    }

    [Fact]
    public async Task EventMiddleware_ApiKey_Success()
    {
        string content = "{'@t':'2022-07-14T19:03:38.3270683+00:00','@m':'Start sleep to: 4539 ms','@mt':'Start sleep to: {sleepTime} ms','@l':'Debug','sleepTime':4539,'SourceContext':'Area52.LogProducer.SimpleHostedService','0':29,'Scope':['Run 29']}";
        using MemoryStream ms = new MemoryStream();
        ms.Write(Encoding.UTF8.GetBytes(content.Replace('\'', '"')).AsSpan());
        ms.Position = 0L;

        DefaultHttpContext defaultContext = new DefaultHttpContext();
        defaultContext.Request.ContentType = "application/vnd.serilog.clef";
        defaultContext.Request.Path = "/api/events/raw";
        defaultContext.Request.Method = "POST";
        defaultContext.Request.Body = ms;
        defaultContext.Request.Headers.Remove("X-Seq-ApiKey");
        defaultContext.Request.Headers.Add("X-Seq-ApiKey", "EnbaledApiKey");

        Mock<IApiKeyServices> apiKeyMocks = new Mock<IApiKeyServices>(MockBehavior.Strict);
        apiKeyMocks.Setup(t => t.VerifyApiKey("EnbaledApiKey", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        Mock<ILogWriter> logWriter = new Mock<ILogWriter>(MockBehavior.Strict);
        logWriter.Setup(t => t.Write(It.IsNotNull<IReadOnlyList<LogEntity>>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        Area52Setup setup = new Area52Setup();

        EventMiddleware middlewareInstance = new EventMiddleware(next: (innerHttpContext) =>
        {
            return Task.CompletedTask;
        },
        apiKeyMocks.Object,
        logWriter.Object,
        Microsoft.Extensions.Options.Options.Create(setup),
        new NullLogger<EventMiddleware>());


        await middlewareInstance.Invoke(defaultContext);

        Assert.Equal(201, defaultContext.Response.StatusCode);
        logWriter.VerifyAll();
    }
}
