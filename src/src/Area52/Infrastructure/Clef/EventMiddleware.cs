using Area52.Services.Configuration;
using Area52.Services.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
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

            int? maxErrors = this.area52Setup.Value.MaxErrorInClefBatch;
            BufferBuilder buffer = new BufferBuilder(2048);
            try
            {
                int errorCounter = 0;
                var reader = httpContext.Request.BodyReader;
                List<LogEntity> list = new List<LogEntity>();
                while (!httpContext.RequestAborted.IsCancellationRequested)
                {
                    ReadResult result = await reader.ReadAsync(httpContext.RequestAborted);
                    ReadOnlySequence<byte> resultBuffer = result.Buffer;

                    while (this.TryReadLine(ref resultBuffer, ref buffer))
                    {
                        if (!buffer.IsEmpty())
                        {
                            this.ParseLine(list, maxErrors, ref buffer, ref errorCounter);
                        }
                    }

                    reader.AdvanceTo(result.Buffer.End);

                    if (result.IsCompleted)
                    {
                        if (!buffer.IsEmpty())
                        {
                            this.ParseLine(list, maxErrors, ref buffer, ref errorCounter);
                        }

                        break;
                    }
                }

                await reader.CompleteAsync();

                await this.logWriter.Write(list);

                httpContext.Response.StatusCode = 201;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Problem with line.");
                httpContext.Response.StatusCode = 500;
            }
            finally
            {
                buffer.Dispose();
            }
            return;
        }

        await this.next(httpContext);
    }

    private void ParseLine(List<LogEntity> logs, int? maxErrors, ref BufferBuilder bufferBuilder, ref int errorCounter)
    {
        try
        {
            logs.Add(ClefParser.Read(bufferBuilder.AsSpan()));
            bufferBuilder.Clear();
        }
        catch (InvalidOperationException ex)
        {
            this.logger.LogWarning(ex, "Problem with single line {line}", this.EncodeLastLine(ref bufferBuilder));
            bufferBuilder.Clear();
            errorCounter++;

            if (maxErrors.HasValue && errorCounter > maxErrors.Value)
            {
                this.logger.LogDebug("Error in this batch more than maximum limit {maximumLimit}.", maxErrors.Value);
                throw;
            }
        }
    }

    private string EncodeLastLine(ref BufferBuilder bufferBuilder)
    {
        try
        {
            return Encoding.UTF8.GetString(bufferBuilder.AsSpan());
        }
        catch (Exception innerEx)
        {
            this.logger.LogDebug(innerEx, "Error during tranlate line to string.");
            return Convert.ToBase64String(bufferBuilder.AsSpan());
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryReadLine(ref ReadOnlySequence<byte> buffer, ref BufferBuilder bufferBuilder)
    {
        var reader = new SequenceReader<byte>(buffer);
        if (reader.TryReadTo(sequence: out var putBuffer, BinaryHelper.NewLine))
        {
            bufferBuilder.Append(ref putBuffer);
            buffer = buffer.Slice(reader.Position);

            return true;
        }
        else
        {
            bufferBuilder.Append(ref buffer);
            return false;
        }
    }
}
