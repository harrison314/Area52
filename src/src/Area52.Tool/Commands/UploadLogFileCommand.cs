using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Area52.Tool.Commands;

public class UploadLogFileCommand : AsyncCommand<UploadLogFileCommand.Settings>
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

        [Description("Clef file path.")]
        [CommandOption("-p|--path")]
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


        public Settings()
        {
            this.Url = string.Empty;
            this.FilePath = string.Empty;
        }
    }

    public UploadLogFileCommand()
    {

    }

    public override async Task<int> ExecuteAsync(CommandContext context, UploadLogFileCommand.Settings settings)
    {
        using FileStream fs = new FileStream(settings.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 2048);
        using TextReader tr = new StreamReader(fs, Encoding.UTF8);
        AnsiConsole.MarkupLine("Opoen file [green]{0}[/]", settings.FilePath.EscapeMarkup());

        string? line;

        int counter = 0;
        StringBuilder sb = new StringBuilder();
        while ((line = await tr.ReadLineAsync()) != null)
        {
            sb.AppendLine(line);
            counter++;

            if (counter >= settings.MaxLineInBatch)
            {
                await this.UploadLogs(settings, sb, counter);
                sb.Clear();
                counter = 0;
            }
        }

        if (counter > 0)
        {
            await this.UploadLogs(settings, sb, counter);
        }

        return 0;
    }

    private async Task UploadLogs(Settings settings, StringBuilder sb, int count)
    {
        AnsiConsole.MarkupLine("[silver]Start uploading...[/]");
        DateTime startTime = DateTime.UtcNow;
        try
        {
            await ClefClient.Upload(settings.Url, sb.ToString(), settings.ApiKey, default);
            TimeSpan duration = DateTime.UtcNow - startTime;

            AnsiConsole.MarkupLine("Upload [green]{0}[/] logs in [green]{1}[/].", count, duration);
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex, ExceptionFormats.Default);
        }
    }
}
