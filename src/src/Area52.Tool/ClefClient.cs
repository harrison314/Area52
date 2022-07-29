using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Area52.Tool;

internal static class ClefClient
{
    private static readonly HttpClient httpClient = new HttpClient();
    private const string ContentType = "application/vnd.serilog.clef";

    public static async Task Upload(string baseAdress, string clefContnet, string? apiKey, CancellationToken cancellationToken)
    {
        Uri endpointUri = ConstructUrl(baseAdress);
        using StringContent stringContent = new StringContent(clefContnet, Encoding.UTF8, ContentType);

        if (apiKey != null)
        {
            stringContent.Headers.Add("X-Seq-ApiKey", apiKey);
        }

        using HttpResponseMessage result = await httpClient.PostAsync(endpointUri, stringContent, cancellationToken);
        result.EnsureSuccessStatusCode();
    }

    private static Uri ConstructUrl(string baseAdress)
    {
        Uri baseuri = new Uri(baseAdress);
        Uri raletive = new Uri("/api/events/raw?clef", UriKind.Relative);

        return new Uri(baseuri, raletive);
    }
}
