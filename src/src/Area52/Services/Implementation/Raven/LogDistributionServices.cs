using System.Text;
using Area52.Services.Configuration;
using Area52.Services.Contracts;
using Area52.Services.Implementation.QueryParser;
using Area52.Services.Implementation.QueryParser.Nodes;
using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using Raven.Client.Documents.Queries.Facets;
using Raven.Client.Documents.Session;

namespace Area52.Services.Implementation.Raven;

public class LogDistributionServices : ILogDistributionServices
{
    private readonly IDocumentStore documentStore;
    private readonly IOptions<Area52Setup> area52Setup;
    private readonly ILogger<LogDistributionServices> logger;

    public class TimestampModel
    {
        public DateTimeOffset Timestamp
        {
            get;
            set;
        }
    }

    public LogDistributionServices(IDocumentStore documentStore, IOptions<Area52Setup> area52Setup, ILogger<LogDistributionServices> logger)
    {
        this.documentStore = documentStore;
        this.area52Setup = area52Setup;
        this.logger = logger;
    }

    public async Task<LogsDistribution> GetLogsDistribution(string query)
    {
        this.logger.LogTrace("Entering to GetLogsDistribution with query {query}.", query);

        IAstNode? ast = string.IsNullOrWhiteSpace(query) ? null : Parser.SimpleParse(query);

        using IAsyncDocumentSession session = this.documentStore.OpenAsyncSession();
        DateTime? startDate = await this.GetBoundary(session, ast, true);
        DateTime? endDate = await this.GetBoundary(session, ast, false);
        if (!startDate.HasValue || !endDate.HasValue)
        {
            return new LogsDistribution(new List<LogsDistributionItem>(), TimeSpan.FromDays(1.0));
        }

        TimeSpan timeDistance = endDate.Value - startDate.Value;
        LogViewSetup logViewSetup = this.area52Setup.Value.LogView;

        if (timeDistance < logViewSetup.DistributionMaxHourTimeInterval)
        {
            return await this.GetHoursDistribution(session, ast, startDate.Value, endDate.Value);
        }

        if (timeDistance <= logViewSetup.DistributionMaxTimeInterval)
        {
            return await this.GetDaysDistribution(session, ast, null);
        }

        return await this.GetDaysDistribution(session, ast, endDate.Value.Add(-logViewSetup.DistributionMaxTimeInterval));
    }

    private async Task<LogsDistribution> GetDaysDistribution(IAsyncDocumentSession session, IAstNode? astNode, DateTime? newStartDate)
    {
        this.logger.LogTrace("Enter to GetDaysDistribution.");

        IAstNode? updatedNode = (astNode != null, newStartDate.HasValue) switch
        {
            (true, true) => new AndNode(astNode!, new GtOrEqNode(new PropertyNode(nameof(LogEntity.Timestamp)), new StringValueNode(newStartDate!.Value.ToString(FormatConstants.SortableDateTimeFormat)))),
            (true, false) => astNode,
            (false, true) => new GtOrEqNode(new PropertyNode(nameof(LogEntity.Timestamp)), new StringValueNode(newStartDate!.Value.ToString(FormatConstants.SortableDateTimeFormat))),
            (false, false) => null,
        };

        RqlQueryBuilderContext context = new RqlQueryBuilderContext("p_");
        updatedNode?.ToRql(context);

        StringBuilder sb = new StringBuilder();
        Dictionary<string, object> parameters = new Dictionary<string, object>();

        sb.AppendLine("from index 'LogMainIndex'");
        sb.Append("where ");
        context.IntoStringBuilder(sb, parameters);
        sb.AppendLine();
        sb.AppendLine("select facet(TimestampDateOnly)");

        string completeQuery = sb.ToString();
        this.logger.LogDebug("Translate input query in GetDaysDistribution to QRL {query}.", completeQuery);

        IAsyncRawDocumentQuery<FacetResult> request = session.Advanced.AsyncRawQuery<FacetResult>(completeQuery);
        foreach ((string name, object value) in parameters)
        {
            request.AddParameter(name, value);
        }
        request.NoTracking();
        var r = await request.ExecuteAggregationAsync();

        List<LogsDistributionItem> items = r["TimestampDateOnly"]
            .Values
            .Select(t => new LogsDistributionItem(DateTime.Parse(t.Range), t.Count))
            .ToList();

        return new LogsDistribution(items, TimeSpan.FromDays(1.0));
    }

    private async Task<LogsDistribution> GetHoursDistribution(IAsyncDocumentSession session, IAstNode? astNode, DateTime startDate, DateTime endDate)
    {
        this.logger.LogTrace("Enter to GetHoursDistribution.");

        RqlQueryBuilderContext context = new RqlQueryBuilderContext("p_");
        astNode?.ToRql(context);

        StringBuilder sb = new StringBuilder();
        Dictionary<string, object> parameters = new Dictionary<string, object>();

        sb.AppendLine("from index 'LogMainIndex'");
        sb.Append("where ");
        context.IntoStringBuilder(sb, parameters);
        sb.AppendLine();
        sb.AppendLine("select facet(");

        Dictionary<string, DateTime> facetDecoder = new Dictionary<string, DateTime>();
        bool isFirst = true;

        endDate = this.MaxUtcValue(endDate, startDate.AddHours(1.001));
        for (DateTime hour = this.TrimUtcHour(startDate); hour <= endDate; hour += TimeSpan.FromHours(1.0))
        {
            string facet = $"Timestamp between '{hour.ToString(FormatConstants.SortableDateTimeFormat)}' and '{hour.AddHours(1.0).ToString(FormatConstants.SortableDateTimeFormat)}'";
            facetDecoder.Add(facet.Replace("'", string.Empty), hour);

            if (isFirst)
            {
                isFirst = false;
            }
            else
            {
                sb.Append(", ");
            }

            sb.Append(facet);
        }

        sb.Append(")");

        string completeQuery = sb.ToString();
        this.logger.LogDebug("Translate input query in GetDaysDistribution to QRL {query}.", completeQuery);

        IAsyncRawDocumentQuery<FacetResult> request = session.Advanced.AsyncRawQuery<FacetResult>(completeQuery);
        foreach ((string name, object value) in parameters)
        {
            request.AddParameter(name, value);
        }
        request.NoTracking();
        Dictionary<string, FacetResult> r = await request.ExecuteAggregationAsync();

        List<LogsDistributionItem> items = r["Timestamp"]
            .Values
            .Select(t => new LogsDistributionItem(facetDecoder[t.Range], t.Count))
            .ToList();

        return new LogsDistribution(items, TimeSpan.FromHours(1.0));
    }

    private async Task<DateTime?> GetBoundary(IAsyncDocumentSession session, IAstNode? astNode, bool first)
    {
        this.logger.LogTrace("Enter to GetBoundary with first {first}.", first);

        Dictionary<string, object> parameters = new Dictionary<string, object>();

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("from index 'LogMainIndex'");
        if (astNode != null)
        {
            RqlQueryBuilderContext context = new RqlQueryBuilderContext("p_");
            astNode?.ToRql(context);

            sb.Append("where ");
            context.IntoStringBuilder(sb, parameters);
            sb.AppendLine();
        }

        if (first)
        {
            sb.AppendLine("order by Timestamp asc");
        }
        else
        {
            sb.AppendLine("order by Timestamp desc");
        }

        sb.AppendLine("select Timestamp");

        string completeQuery = sb.ToString();
        this.logger.LogDebug("Translate input query in GetBoundary to QRL {query}.", completeQuery);

        IAsyncRawDocumentQuery<TimestampModel> request = session.Advanced.AsyncRawQuery<TimestampModel>(completeQuery);
        foreach ((string name, object value) in parameters)
        {
            request.AddParameter(name, value);
        }
        request.NoTracking();
        request.Take(1);
        List<TimestampModel> selectedDates = await request.ToListAsync();

        return (selectedDates.Count == 0) ? null : new DateTime?(selectedDates[0].Timestamp.UtcDateTime);
    }

    private DateTime TrimUtcHour(DateTime value)
    {
        value = value.ToUniversalTime();
        return new DateTime(value.Year, value.Month, value.Day, value.Hour, 0, 0, DateTimeKind.Utc);
    }

    private DateTime MaxUtcValue(DateTime v1, DateTime v2)
    {
        v1 = v1.ToUniversalTime();
        v2 = v2.ToUniversalTime();

        return (v1 > v2) ? v1 : v2;
    }
}

