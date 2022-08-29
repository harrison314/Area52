namespace Area52.Ufo.Services.Configuration;

public class FolderWatchSetup
{
    public bool Enabled
    {
        get;
        init;
    }

    public string Folder
    {
        get;
        init;
    }

    public string? GlobPattern
    {
        get;
        init;
    }

    public string TmpFolder
    {
        get;
        init;
    }

    public FolderWatchSetup()
    {
        this.Folder = string.Empty;
        this.TmpFolder = string.Empty;
    }
}
