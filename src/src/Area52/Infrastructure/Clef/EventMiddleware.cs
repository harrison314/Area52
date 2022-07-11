using Area52.Services.Contracts;
using System.Text;

namespace Area52.Infrastructure.Clef;

public class EventMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogWriter logWriter;
    private readonly ILogger<EventMiddleware> logger;

    public EventMiddleware(RequestDelegate next, ILogWriter logWriter, ILogger<EventMiddleware> logger)
    {
        _next = next;
        this.logWriter = logWriter;
        this.logger = logger;
    }

    public async Task Invoke(HttpContext httpContext)
    {
        if (httpContext.Request.Method == "POST" && httpContext.Request.Path == "/api/events/raw")
        {
            string? line = null;
            try
            {
                // TODO: Optimalize
                using TextReader tr = new StreamReader(httpContext.Request.Body, System.Text.Encoding.UTF8);
                List<LogEntity> list = new List<LogEntity>();
                while ((line = await tr.ReadLineAsync()) != null)
                {
                    list.Add(ClefParser.Read(Encoding.UTF8.GetBytes(line)));
                }

                await this.logWriter.Write(list);
                httpContext.Response.StatusCode = 201;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Problem with line {line}", line);
                httpContext.Response.StatusCode = 500;
            }
            return;
        }

        await _next(httpContext);
    }
}
