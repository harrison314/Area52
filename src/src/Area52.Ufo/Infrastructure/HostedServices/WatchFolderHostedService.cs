using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Area52.Ufo.Services.Configuration;
using Area52.Ufo.Services.Contracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Area52.Ufo.Infrastructure.HostedServices;

public class WatchFolderHostedService : BackgroundService
{
    private readonly IBatchClefClient batchClefClient;
    private readonly IOptions<FolderWatchSetup> setup;
    private readonly ILogger<WatchFolderHostedService> logger;

    public WatchFolderHostedService(IBatchClefClient batchClefClient,
        IOptions<FolderWatchSetup> setup,
        ILogger<WatchFolderHostedService> logger)
    {
        this.batchClefClient = batchClefClient;
        this.setup = setup;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        this.logger.LogTrace("Entering to ExecuteAsync.");

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(500, stoppingToken);

            FolderWatchSetup localSetup = this.setup.Value;
            string[] paths = this.GetFiles(localSetup);
            if (paths.Length == 0)
            {
                continue;
            }

            StringBuilder sb = new StringBuilder(500 * 1024);

            foreach (string orgLogFilePath in paths)
            {
                try
                {
                    if (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }

                    string newPath = this.MooveLogFile(localSetup, orgLogFilePath);
                    await this.SendFileContent(newPath, sb, stoppingToken);

                    await this.RemoveFile(newPath, stoppingToken);
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "Error during processing file {logFile}.", orgLogFilePath);
                    //TODO
                }
            }
        }
    }

    private async Task SendFileContent(string newPath, StringBuilder buffer, CancellationToken stoppingToken)
    {
        this.logger.LogTrace("Entering to SendFileContent with newPath {newPath}.", newPath);

        using FileStream fs = new FileStream(newPath, FileMode.Open, FileAccess.ReadWrite);
        using TextReader tr = new StreamReader(fs, Encoding.UTF8);

        int recomandetCount = this.batchClefClient.GetReamingCount();
        int count = 0;
        string? line = null;
        buffer.Clear();

        while ((line = await tr.ReadLineAsync()) != null)
        {
            stoppingToken.ThrowIfCancellationRequested();
            buffer.AppendLine(line);
            count++;

            if (count >= recomandetCount)
            {
                await this.batchClefClient.DirectSend(buffer.ToString(), stoppingToken);

                buffer.Clear();
                count = 0;
            }
        }

        if (count > 0)
        {
            await this.batchClefClient.DirectSend(buffer.ToString(), stoppingToken);

            buffer.Clear();
            count = 0;
        }

        this.logger.LogInformation("Send file {newPath} into server.", newPath);
    }

    private string[] GetFiles(FolderWatchSetup localSetup)
    {
        this.logger.LogTrace("Entering to GetFiles.");

        DirectoryInfo directoryInfo = new DirectoryInfo(localSetup.Folder);
        if (!directoryInfo.Exists)
        {
            throw new Exception(); //TODO
        }

        FileInfo[] files = directoryInfo.GetFiles(localSetup.GlobPattern ?? "*", SearchOption.TopDirectoryOnly);
        string[] paths = new string[files.Length];
        for (int i = 0; i < paths.Length; i++)
        {
            paths[i] = files[i].FullName;
        }

        return paths;
    }

    private string MooveLogFile(FolderWatchSetup localSetup, string originalFile)
    {
        this.logger.LogTrace("Entering to MooveLogFile with originalFile {originalFile}.", originalFile);

        string newPath = Path.Combine(localSetup.TmpFolder, $"{Guid.NewGuid():D}.log");

        File.Move(originalFile, newPath);
        this.logger.LogDebug("Move log file from {fromLogFile} to {tmpLogFile}.", originalFile, newPath);

        return newPath;
    }

    private async Task RemoveFile(string newLogPath, CancellationToken cancellationToken)
    {
        this.logger.LogTrace("Entering to RemoveFile with newLogPath {newLogPath}.", newLogPath);

        for (int i = 1; i < int.MaxValue; i++)
        {
            try
            {
                File.Delete(newLogPath);
                cancellationToken.ThrowIfCancellationRequested();
                break;
            }
            catch (DirectoryNotFoundException)
            {
                throw;
            }
            catch (IOException ex)
            {
                if (i > 4)
                {
                    throw;
                }

                this.logger.LogWarning(ex, "Retry problem with delete file {newLogPath}.", newLogPath);
                await Task.Delay(100 * i, cancellationToken);
            }
        }

        this.logger.LogDebug("Removed newLogPath {newLogPath}.", newLogPath);
    }
}
