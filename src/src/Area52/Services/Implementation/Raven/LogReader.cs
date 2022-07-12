using Area52.Services.Contracts;
using Area52.Services.Implementation.QueryParser;
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
    private readonly ILogger<LogReader> logger;

    public LogReader(IDocumentStore documentStore, ILogger<LogReader> logger)
    {
        this.documentStore = documentStore;
        this.logger = logger;
    }

    public async Task<LogEntity> LoadLogInfo(string id)
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
            this.logger.LogDebug("Translate input query {query} to RQL {rql}.", query, reqlQuery.Query);

            using var session = this.documentStore.OpenAsyncSession();

            IAsyncRawDocumentQuery<LogInfo> request = session.Advanced.AsyncRawQuery<LogInfo>(reqlQuery.Query);
            reqlQuery.SetParameters(request);
            request.NoTracking();
            request.Take(150);
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

    public async IAsyncEnumerable<LogEntity> ReadLogs(string query, int? limit)
    {
        this.logger.LogTrace("Entering to ReadLogs with query {query} limit {limit}", query, limit);

        DynamicIndexQueryBuilder builder = new DynamicIndexQueryBuilder();
        builder.SetSelectClausule(null);
        if (!string.IsNullOrWhiteSpace(query))
        {
            IAstNode ast = Parser.SimpleParse(query);
            builder.Add(ast);
        }

        QueryWithParameters rqlQuery = builder.BuildQuery();
        this.logger.LogDebug("Translate input query {query} to RQL {rql}.", query, rqlQuery.Query);

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

        this.logger.LogDebug("Finishing downloading logs for query {query}.", query);

    }
}
