using Area52.Services.Configuration;
using Area52.Services.Contracts;
using Area52.Services.Implementation.QueryParser;
using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Area52.Services.Implementation.Raven;

public class LogReader : ILogReader
{
    private readonly IDocumentStore documentStore;
    private readonly IOptions<Area52Setup> area52Setup;
    private readonly ILogger<LogReader> logger;

    public LogReader(IDocumentStore documentStore, IOptions<Area52Setup> area52Setup, ILogger<LogReader> logger)
    {
        this.documentStore = documentStore;
        this.area52Setup = area52Setup;
        this.logger = logger;
    }

    public async Task<LogEntity?> LoadLogInfo(string id)
    {
        this.logger.LogTrace("Entering to LoadLogInfo with id {logId}", id);

        using IAsyncDocumentSession session = this.documentStore.OpenAsyncSession();
        return await session.LoadAsync<LogEntity>(id);
    }

    public async Task<ReadLastLogResult> ReadLastLogs(string query)
    {
        this.logger.LogTrace("Entering to ReadLastLogs with query {query}", query);

        try
        {
            DynamicIndexQueryBuilder builder = new DynamicIndexQueryBuilder();
            if (!string.IsNullOrWhiteSpace(query))
            {
                IAstNode ast = Parser.SimpleParse(query);
                builder.Add(ast);
            }

            QueryWithParameters reqlQuery = builder.BuildQuery();
            this.logger.LogDebug("Translate input query {query} to RQL {rql} with parameters {parameters}.", query, reqlQuery.Query, reqlQuery.Parameters);

            using var session = this.documentStore.OpenAsyncSession();

            IAsyncRawDocumentQuery<LogInfo> request = session.Advanced.AsyncRawQuery<LogInfo>(reqlQuery.Query);
            reqlQuery.SetParameters(request);
            request.NoTracking();
            request.Take(this.area52Setup.Value.LogView.MaxLogShow);
            request.Statistics(out QueryStatistics stats);
            List<LogInfo> result = await request.ToListAsync();

            this.logger.LogDebug("Executed query {query} with {resultsCount} results and {executionTimeMs} ms.",
                query,
                stats.LongTotalResults,
                stats.DurationInMs);

            return new ReadLastLogResult(result, stats.LongTotalResults);
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "Error during ReadLastLogs with query {query}.", query);
            throw;
        }
    }

    public IAsyncEnumerable<LogEntity> ReadLogs(string query, int? limit)
    {
        this.logger.LogTrace("Entering to ReadLogs with query {query} limit {limit}", query, limit);

        IAstNode? ast = string.IsNullOrWhiteSpace(query) ? null : Parser.SimpleParse(query);
        return this.ReadLogsInternals(ast, limit);
    }

    public IAsyncEnumerable<LogEntity> ReadLogs(IAstNode query, int? limit)
    {
        this.logger.LogTrace("Entering to ReadLogs with ast nodes, limit {limit}.", limit);
        return this.ReadLogsInternals(query, limit);
    }

    public async IAsyncEnumerable<LogEntity> ReadLogsInternals(IAstNode? astNodes, int? limit)
    {
        DynamicIndexQueryBuilder builder = new DynamicIndexQueryBuilder();
        builder.SetSelectClause(null);
        if (astNodes != null)
        {
            builder.Add(astNodes);
        }

        QueryWithParameters rqlQuery = builder.BuildQuery();
        if (this.logger.IsEnabled(LogLevel.Debug))
        {
            this.logger.LogDebug("Translate input query {query} to RQL {rql} with parameters {parameters}.", astNodes, rqlQuery.Query, rqlQuery.Parameters);
        }

        using var session = this.documentStore.OpenAsyncSession();

        IAsyncRawDocumentQuery<LogEntity> request = session.Advanced.AsyncRawQuery<LogEntity>(rqlQuery.Query);
        rqlQuery.SetParameters(request);
        request.NoTracking();
        if (limit.HasValue)
        {
            request.Take(limit.Value);
        }

        this.logger.LogDebug("Start downloading logs.");
        await using IAsyncEnumerator<global::Raven.Client.Documents.Commands.StreamResult<LogEntity>> asyncStream = await session.Advanced.StreamAsync(request);
        while (await asyncStream.MoveNextAsync())
        {
            yield return asyncStream.Current.Document;
        }
    }
}
