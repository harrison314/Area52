using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Area52.Ufo.Services.Configuration;
using Area52.Ufo.Services.Contracts;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Area52.Ufo.Services.Implementation;

public class BatchClefClient : IBatchClefClient
{
    private const string ContentType = "application/vnd.serilog.clef";

    private readonly IHttpClientFactory httpClientFactory;
    private readonly IOptions<UfoSetup> ufoSetup;
    private readonly ILogger<BatchClefClient> logger;

    public BatchClefClient(IHttpClientFactory httpClientFactory,
        IOptions<UfoSetup> ufoSetup,
        ILogger<BatchClefClient> logger)
    {
        this.httpClientFactory = httpClientFactory;
        this.ufoSetup = ufoSetup;
        this.logger = logger;
    }

    public async Task DirectSend(string clefLines, CancellationToken cancellationToken)
    {
        this.logger.LogTrace("Entering to DirectSend.");

        Uri endpointUri = this.ConstructUrl(this.ufoSetup.Value.Area52Endpoint);
        string? apiKey = this.ufoSetup.Value.ApiKey;

        using StringContent stringContent = new StringContent(clefLines, Encoding.UTF8, ContentType);

        if (apiKey != null)
        {
            stringContent.Headers.Add("X-Seq-ApiKey", apiKey);
        }

        HttpClient httpClient = this.httpClientFactory.CreateClient();
        using HttpResponseMessage result = await httpClient.PostAsync(endpointUri, stringContent, cancellationToken);
        result.EnsureSuccessStatusCode();
    }

    public int GetReamingCount()
    {
        this.logger.LogTrace("Entering to GetReamingCount.");

        return this.ufoSetup.Value.RecomandetBatchSize;
    }

    private Uri ConstructUrl(string baseAdress)
    {
        Uri baseuri = new Uri(baseAdress);
        Uri raletive = new Uri("/api/events/raw?clef", UriKind.Relative);

        return new Uri(baseuri, raletive);
    }
}
