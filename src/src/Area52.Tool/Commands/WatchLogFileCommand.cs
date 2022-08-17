using System.ComponentModel;
using Area52.Tool.Commands.LogWatcher;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Area52.Tool.Commands;

public class WatchLogFileCommand : AsyncCommand<WatchLogFileCommand.Settings>
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

        [Description("Clef file Path.")]
        [CommandArgument(0, "[Path]")]
        public string FilePath
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
            this.FilePath = string.Empty;
        }
    }

    private bool canceled;

    public WatchLogFileCommand()
    {
        this.canceled = false;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, WatchLogFileCommand.Settings settings)
    {
        AnsiConsole.MarkupLine("Watch file [green]{0}[/]", settings.FilePath.EscapeMarkup());

        LogFile logFile = new LogFile(settings.FilePath);

        await using WatchLogSender sender = new WatchLogSender(settings.Url,
            settings.ApiKey,
            settings.MaxLineInBatch,
            TimeSpan.FromSeconds(settings.LogTimoutInSec));

        Console.CancelKeyPress += this.Console_CancelKeyPress;
        while (!this.canceled)
        {
            string[]? lines = logFile.Read();
            bool hasSend = false;
            if (lines != null)
            {
                AnsiConsole.MarkupLine("Read [green]{0}[/] lines.", lines.Length);
                hasSend = await sender.TrySend(lines.Where(this.ValidateJson));
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

    private bool ValidateJson(string line)
    {
        try
        {
            _ = System.Text.Json.JsonSerializer.Deserialize<ClefObject>(line.AsSpan(),
                 SourceGenerationContext.Default.ClefObject);

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex.ToString());
            return false;
        }
    }

    private void Console_CancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        this.canceled = true;
    }
}
