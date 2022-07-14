using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Area52.Services.Contracts;
using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using Raven.Client.Exceptions;

namespace Area52.Services.Implementation.Raven;

public class RavenDbIndexJob : IStartupJob
{
    private readonly IDocumentStore documentStore;
    private readonly ILogger<RavenDbIndexJob> logger;

    public RavenDbIndexJob(IDocumentStore documentStore, ILogger<RavenDbIndexJob> logger)
    {
        this.documentStore = documentStore;
        this.logger = logger;
    }

    public async ValueTask Execute(CancellationToken cancellationToken)
    {
        this.logger.LogTrace("Entering to Execute.");

        try
        {
            await IndexCreation.CreateIndexesAsync(typeof(Program).Assembly, 
                this.documentStore,
                token: cancellationToken);

        }
        catch (RavenException ex)
        {
            this.logger.LogError(ex, "Error during CreateIndexesAsync in RavenDB.");
        }
    }
}
