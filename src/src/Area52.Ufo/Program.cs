using Microsoft.AspNetCore.Builder;

namespace Area52.Ufo
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
            WebApplication app = builder.Build();

            app.MapGet("/", () => "Hello World!");

            app.Run();
        }
    }
}
