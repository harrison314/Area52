using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Area52.Tool.Commands;

public class WatchStdinCommand : AsyncCommand<WatchStdinCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [Description("Clef endpoint URL.")]
        [CommandOption("-u|--url")]
        public string Url
        {
            get;
            set;
        }

        [Description("Clef API key.")]
        [CommandOption("-k|--api-key")]
        public string? ApiKey
        {
            get;
            set;
        }

        [Description("Log count in one upload batch.")]
        [CommandOption("-b|--batch-size")]
        [DefaultValue(500)]
        public int MaxLineInBatch
        {
            get;
            set;
        }

        [Description("Log flash timeout in second.")]
        [CommandOption("-t|--log-timeout")]
        [DefaultValue(16)]
        public int LogTimoutInSec
        {
            get;
            set;
        }


        public Settings()
        {
            this.Url = string.Empty;
        }
    }

    private bool canceled;

    public WatchStdinCommand()
    {
        this.canceled = false;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, WatchStdinCommand.Settings settings)
    {
        AnsiConsole.MarkupLine("Watch file STDIN");

        await using WatchLogSender sender = new WatchLogSender(settings.Url,
            settings.ApiKey,
            settings.MaxLineInBatch,
            TimeSpan.FromSeconds(settings.LogTimoutInSec));

        Console.CancelKeyPress += this.Console_CancelKeyPress;
        while (!this.canceled)
        {
            string? line = Console.ReadLine();
            bool hasSend = false;
            if (line != null)
            {
                hasSend = await sender.TrySend(this.ValidateJson(line));
                await Task.Delay(500);
            }
            else
            {
                hasSend = await sender.TrySend();
                await Task.Delay(1000);
            }

            if (hasSend)
            {
                AnsiConsole.MarkupLine("Send data to server...");
            }
        }

        return 0;
    }

    private string[] ValidateJson(string line)
    {
        try
        {
            _ = System.Text.Json.JsonSerializer.Deserialize<ClefObject>(line.AsSpan(),
                 SourceGenerationContext.Default.ClefObject);

            return new string[] { line };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex.ToString());
            return Array.Empty<string>();
        }
    }

    private void Console_CancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        this.canceled = true;
    }
}
