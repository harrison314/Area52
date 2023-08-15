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
                    .WithExample(new[] { "upload", "logFile.txt", "logFile2.txt", "-u", "https://area52.local/" })
                    .WithExample(new[] { "upload", "logFile.txt", "-u", "https://area52.local/", "--api-key", "XXXXXXXXXXXXXXXXX", "--batch-size", "250" });

                config.AddCommand<Commands.WatchLogFileCommand>("watch")
                    .WithDescription("Watch CLEF file and send logs to endpoint.")
                    .WithExample(new[] { "watch", "logFile.txt", "-u", "https://area52.local/" })
                    .WithExample(new[] { "watch", "logFile.txt", "-u", "https://area52.local/", "--api-key", "XXXXXXXXXXXXXXXXX", "--batch-size", "250" });

                config.AddCommand<Commands.WatchLogFileCommand>("stdin")
                    .WithDescription("Watch stdin and send logs to endpoint.")
                    .WithExample(new[] { "stdin", "-u", "https://area52.local/" })
                    .WithExample(new[] { "stdin", "-u", "https://area52.local/", "--api-key", "XXXXXXXXXXXXXXXXX", "--batch-size", "250" });
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
