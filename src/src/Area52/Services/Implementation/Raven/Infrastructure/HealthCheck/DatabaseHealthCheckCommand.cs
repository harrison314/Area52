using Raven.Client.Http;
using Sparrow.Json;

namespace Area52.Services.Implementation.Raven.Infrastructure.HealthCheck;

internal class DatabaseHealthCheckCommand : RavenCommand
{
    public DatabaseHealthCheckCommand(TimeSpan? timout = null)
    {
        this.Timeout = timout ?? TimeSpan.FromSeconds(15.0);
    }

    public override HttpRequestMessage CreateRequest(JsonOperationContext ctx, ServerNode node, out string url)
    {
        url = $"{node.Url}/databases/{node.Database}/healthcheck";

        return new HttpRequestMessage()
        {
            Method = HttpMethod.Get
        };
    }
}
