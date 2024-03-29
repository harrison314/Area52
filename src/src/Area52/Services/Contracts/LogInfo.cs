﻿namespace Area52.Services.Contracts;

public class LogInfo
{
    public string Id
    {
        get;
        set;
    }

    public DateTimeOffset Timestamp
    {
        get;
        set;
    }

    public string Level
    {
        get;
        set;
    }

    public string Message
    {
        get;
        set;
    }

    public LogInfo()
    {
        this.Id = string.Empty;
        this.Level = string.Empty;
        this.Message = string.Empty;
    }
}