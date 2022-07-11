using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Area52.Services.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;

namespace Area52.Infrastructure.Clef;

public static class ClefFileDownloadEndpointExtensions
{
    public static IEndpointRouteBuilder MapDownloadClefFile(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/events/download", ([FromQuery(Name = "q")] string query, [FromServices] ILogReader logReader) =>
        {
            IAsyncEnumerable<LogEntity> result = logReader.ReadLogs(query, 150);
            return new ClefHttpResult(result);
        });

        endpoints.MapGet("/events/download/all", ([FromQuery(Name = "q")] string query, [FromServices] ILogReader logReader) =>
        {
            IAsyncEnumerable<LogEntity> result = logReader.ReadLogs(query, null);
            return new ClefHttpResult(result);
        });

        return endpoints;
    }
}
