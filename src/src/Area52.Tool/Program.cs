using Spectre.Console;
using Spectre.Console.Cli;

namespace Area52.Tool
{
    public static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            CommandApp app = new CommandApp();
            app.Configure(config =>
            {
#if DEBUG
                config.PropagateExceptions();
                config.ValidateExamples();
#endif

                config.AddCommand<Commands.UploadLogFileCommand>("upload")
                    .WithDescription("Upload CLEF file to endpoint.")
                    .WithExample(new[] { "upload", "-u", "https://area52.local/", "-p", "logFile.txt" })
                    .WithExample(new[] { "upload", "-u", "https://area52.local/", "-p", "logFile.txt", "--api-key", "XXXXXXXXXXXXXXXXX", "--batch-size", "250" });
            });

            try
            {
                return await app.RunAsync(args);
            }
            catch (Exception ex)
            {
                AnsiConsole.WriteException(ex, ExceptionFormats.Default);
                return -1;
            }
        }
    }
}
