using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Area52.Tool.Commands.LogWatcher;

using FileStreamDict = Dictionary<string, LogFile>;

internal class Watcher
{
    public static void StartWatching(string[] directories, int cycleLength, Action<string, string[]> onChange)
    {

        FileStreamDict streams = ChangeCycle(directories, new FileStreamDict(), (string file, string[] lines) => { });

        while (true)
        {
            streams = ChangeCycle(directories, streams, onChange);
            Thread.Sleep(cycleLength);
        }
    }

    public static async Task StartWatchingAsync(string[] directories, TimeSpan cycleLength, Func<string, string[], Task> onChange, CancellationToken cancellationToken)
    {

        FileStreamDict streams = ChangeCycle(directories, new FileStreamDict(), (string file, string[] lines) => { });

        while (!cancellationToken.IsCancellationRequested)
        {
            streams = await ChangeCycleAsync(directories, streams, onChange);
            await Task.Delay(cycleLength, cancellationToken);
        }
    }

    private static FileStreamDict ChangeCycle(string[] directories, FileStreamDict existingFiles, Action<string, string[]> onChange)
    {
        FileStreamDict streams = AddNewFilesToStreams(directories, existingFiles);

        foreach (KeyValuePair<string, LogFile> entry in streams)
        {
            string[]? lines = entry.Value.Read();
            if (lines != null)
            {
                onChange(entry.Key, lines);
            }
        }

        return streams;
    }
    private static async ValueTask<FileStreamDict> ChangeCycleAsync(string[] directories, FileStreamDict existingFiles, Func<string, string[], Task> onChange)
    {
        FileStreamDict streams = AddNewFilesToStreams(directories, existingFiles);

        foreach (KeyValuePair<string, LogFile> entry in streams)
        {
            string[]? lines = entry.Value.Read();
            if (lines != null)
            {
                await onChange(entry.Key, lines);
            }
        }

        return streams;
    }

    private static FileStreamDict AddNewFilesToStreams(string[] directories, FileStreamDict existingFiles)
    {
        IEnumerable<string> files = directories.Select(path => Directory.GetFiles(path)).SelectMany(x => x);
        FileStreamDict outMap = new FileStreamDict();

        foreach (string file in files)
        {
            if (!existingFiles.TryGetValue(file, out LogFile? value))
            {
                value = new LogFile(file);
            }

            outMap[file] = value;
        }

        return outMap;
    }
}
