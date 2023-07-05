using Area52.Services.Configuration;
using Area52.Services.Contracts;
using Microsoft.Extensions.Options;
using System.Buffers;
using System.Text;

namespace Area52.Infrastructure.Clef;

public class EventMiddleware
{
    private readonly RequestDelegate next;
    private readonly IApiKeyServices apiKeyServices;
    private readonly ILogWriter logWriter;
    private readonly IOptions<Area52Setup> area52Setup;
    private readonly ILogger<EventMiddleware> logger;

    public EventMiddleware(RequestDelegate next, IApiKeyServices apiKeyServices, ILogWriter logWriter, IOptions<Area52Setup> area52Setup, ILogger<EventMiddleware> logger)
    {
        this.next = next;
        this.apiKeyServices = apiKeyServices;
        this.logWriter = logWriter;
        this.area52Setup = area52Setup;
        this.logger = logger;
    }

    public async Task Invoke(HttpContext httpContext)
    {
        if (httpContext.Request.Method == "POST" && httpContext.Request.Path == "/api/events/raw")
        {
            // Api key verifictaion
            httpContext.Request.Headers.TryGetValue("X-Seq-ApiKey", out Microsoft.Extensions.Primitives.StringValues apiKeyHeader);
            if (!await this.apiKeyServices.VerifyApiKey(apiKeyHeader.SingleOrDefault(), default))
            {
                this.logger.LogDebug("Api key {apiKey} is not allowed api key.", apiKeyHeader.SingleOrDefault());
                httpContext.Response.StatusCode = 401;
                return;
            }

            string? line = null;
            int? maxErrors = this.area52Setup.Value.MaxErrorInClefBatch;
            byte[] buffer = ArrayPool<byte>.Shared.Rent(2048);
            try
            {
                // TODO: Optimize
                using TextReader tr = new StreamReader(httpContext.Request.Body, System.Text.Encoding.UTF8);
                List<LogEntity> list = new List<LogEntity>();
                int errorCounter = 0;
                while ((line = await tr.ReadLineAsync()) != null)
                {
                    try
                    {
                        int encodedLen = Encoding.UTF8.GetByteCount(line);
                        if (encodedLen >= buffer.Length)
                        {
                            ArrayPool<byte>.Shared.Return(buffer, false);
                            int reuested = Encoding.UTF8.GetByteCount(line);
                            buffer = ArrayPool<byte>.Shared.Rent(reuested);
                            encodedLen = Encoding.UTF8.GetBytes(line, buffer); //TODO: remove
                        }

                        encodedLen = Encoding.UTF8.GetBytes(line, buffer);
                        list.Add(ClefParser.Read(buffer.AsSpan(0, encodedLen)));
                    }
                    catch (InvalidOperationException ex)
                    {
                        this.logger.LogWarning(ex, "Problem with single line {line}", line);
                        errorCounter++;

                        if (maxErrors.HasValue && errorCounter > maxErrors.Value)
                        {
                            this.logger.LogDebug("Error in this batch more than maximum limit {maximumLimit}.", maxErrors.Value);
                            throw;
                        }
                    }
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
