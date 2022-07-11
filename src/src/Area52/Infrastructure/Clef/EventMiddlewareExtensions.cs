namespace Area52.Infrastructure.Clef;

public static class EventMiddlewareExtensions
{
    public static IApplicationBuilder UseEventMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<EventMiddleware>();
    }
}