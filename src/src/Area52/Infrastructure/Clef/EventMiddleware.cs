using Area52.Services.Contracts;
using System.Buffers;
using System.Text;

namespace Area52.Infrastructure.Clef;

public class EventMiddleware
{
    private readonly RequestDelegate next;
    private readonly ILogWriter logWriter;
    private readonly ILogger<EventMiddleware> logger;

    public EventMiddleware(RequestDelegate next, ILogWriter logWriter, ILogger<EventMiddleware> logger)
    {
        this.next = next;
        this.logWriter = logWriter;
        this.logger = logger;
    }

    public async Task Invoke(HttpContext httpContext)
    {
        if (httpContext.Request.Method == "POST" && httpContext.Request.Path == "/api/events/raw")
        {
            string? line = null;
            byte[] buffer = ArrayPool<byte>.Shared.Rent(2048);
            try
            {
                // TODO: Optimalize
                using TextReader tr = new StreamReader(httpContext.Request.Body, System.Text.Encoding.UTF8);
                List<LogEntity> list = new List<LogEntity>();
                while ((line = await tr.ReadLineAsync()) != null)
                {
                    int encodedLen = Encoding.UTF8.GetByteCount(line);
                    if (encodedLen >= buffer.Length)
                    {
                        ArrayPool<byte>.Shared.Return(buffer, false);
                        int reuested = Encoding.UTF8.GetByteCount(line);
                        buffer = ArrayPool<byte>.Shared.Rent(reuested);
                        encodedLen = Encoding.UTF8.GetBytes(line, buffer);
                    }

                    encodedLen = Encoding.UTF8.GetBytes(line, buffer);
                    list.Add(ClefParser.Read(buffer.AsSpan(0, encodedLen)));
                }

                await this.logWriter.Write(list);
                httpContext.Response.StatusCode = 201;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Problem with line {line}", line);
                httpContext.Response.StatusCode = 500;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer, false);
            }
            return;
        }

        await this.next(httpContext);
    }
}
