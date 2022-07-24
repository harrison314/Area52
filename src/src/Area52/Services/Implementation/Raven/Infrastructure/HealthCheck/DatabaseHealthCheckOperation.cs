using Raven.Client.Documents;
using Raven.Client.Documents.Conventions;
using Raven.Client.Documents.Operations;
using Raven.Client.Http;
using Sparrow.Json;

namespace Area52.Services.Implementation.Raven.Infrastructure.HealthCheck;

internal class DatabaseHealthCheckOperation : IOperation
{
    private readonly TimeSpan timout;

    public DatabaseHealthCheckOperation(TimeSpan timout)
    {
        this.timout = timout;
    }

    public RavenCommand GetCommand(IDocumentStore store, DocumentConventions conventions, JsonOperationContext context, HttpCache cache)
    {
        return new DatabaseHealthCheckCommand(this.timout);
    }
}
