using System.Text;
using Spectre.Console;

namespace Area52.Tool.Commands;

internal class WatchLogSender : IAsyncDisposable
{
    private readonly string endpoint;
    private readonly string? apiKey;
    private readonly StringBuilder sb;
    private int linesCount;
    private int linesLimit;
    private readonly TimeSpan timeLimit;
    private DateTime lastSend;

    public WatchLogSender(string endpoint, string? apiKey, int linesLimit, TimeSpan timeLimit)
    {
        this.endpoint = endpoint;
        this.apiKey = apiKey;
        this.linesLimit = linesLimit;
        this.timeLimit = timeLimit;
        this.linesCount = 0;
        this.sb = new StringBuilder();
        this.lastSend = DateTime.UtcNow;
    }

    public async ValueTask<bool> TrySend(IEnumerable<string> lines)
    {
        foreach (string line in lines)
        {
            this.sb.AppendLine(line);
            this.linesCount++;
        }

        //AnsiConsole.MarkupLine("Internal buffer contains [green]{0}[/] lines.", this.linesCount);

        if (this.linesCount >= this.linesLimit
            || (this.linesCount > 0 && (DateTime.UtcNow - this.lastSend) > this.timeLimit))
        {
            await this.FlushBuffer();
            return true;
        }

        return false;
    }

    public async ValueTask DisposeAsync()
    {
        if (this.linesCount > 0)
        {
            await this.FlushBuffer();
        }
    }

    private async Task FlushBuffer()
    {
        await ClefClient.Upload(this.endpoint,
            this.sb.ToString(),
            this.apiKey,
            default);

        AnsiConsole.MarkupLine("Send [green]{0}[/] lines to server.", this.linesCount);

        this.linesCount = 0;
        this.sb.Clear();
        this.lastSend = DateTime.UtcNow;
    }
}
