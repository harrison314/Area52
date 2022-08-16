using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Area52.Tool.Commands.LogWatcher;

internal class LogFile
{
    public string Path
    {
        get;
        private set;
    }

    public long PastSize
    {
        get;
        private set;
    }

    public LogFile(string path)
    {
        this.Path = path;
        UpdateLastSize(this);
    }

    public string[]? Read()
    {
        // Figure out the new size
        var newSize = new FileInfo(this.Path).Length;

        if (newSize == this.PastSize)
        {
            return null;
        }

        // If the sizes differ, find out the target offset.
        var seekOffset = (newSize > this.PastSize) ? this.PastSize : 0;
        UpdateLastSize(this);

        return ReadFromOffset(this.Path, seekOffset);
    }

    private static string[] ReadFromOffset(string path, long seekOffset)
    {
        using FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using StreamReader reader = new StreamReader(fs);
        
        reader.BaseStream.Seek(seekOffset, SeekOrigin.Begin);

        List<string> lines = new List<string>();
        string? line = null;
        while ((line = reader.ReadLine()) != null)
        {
            lines.Add(line);
        }

        return lines.ToArray();
    }

    private static void UpdateLastSize(LogFile f)
    {
        f.PastSize = new FileInfo(f.Path).Length;
    }
}
