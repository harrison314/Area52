using Area52.Services.Contracts;

namespace Area52.Infrastructure.Clef;

internal class ClefHttpResult : IResult
{
    private readonly IAsyncEnumerable<LogEntity> entityStream;
    private readonly byte[] crlf;

    public ClefHttpResult(IAsyncEnumerable<LogEntity> entityStream)
    {
        this.entityStream = entityStream;
        this.crlf = new byte[] { 0x0D, 0x0A };
    }

    public async Task ExecuteAsync(HttpContext httpContext)
    {
        httpContext.Response.StatusCode = 200;
        httpContext.Response.Headers.ContentType = new Microsoft.Extensions.Primitives.StringValues("application/octet-stream");
        httpContext.Response.Headers.Add("Content-Disposition", "Attachment;FileName=Export.txt");

        await foreach(LogEntity entry in this.entityStream)
        {
            await ClefParser.Write(entry, httpContext.Response.Body);
            await httpContext.Response.Body.WriteAsync(this.crlf);
        }
    }
}