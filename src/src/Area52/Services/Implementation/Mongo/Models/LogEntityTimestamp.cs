using MongoDB.Bson.Serialization.Attributes;

namespace Area52.Services.Implementation.Mongo.Models;

public class LogEntityTimestamp
{
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime Utc
    {
        get;
        set;
    }

    public string Sortable
    {
        get;
        set;
    }

    public long DateTruncateUnixTime
    {
        get;
        set;
    }

    public long HourTruncateUnixTime
    {
        get;
        set;
    }

    public LogEntityTimestamp()
    {
        this.Sortable = string.Empty;
    }

    public LogEntityTimestamp(DateTimeOffset time)
    {
        this.Utc = time.UtcDateTime;
        this.Sortable = time.ToString(FormatConstants.SortableDateTimeFormat);

        const long secundPerHour = 60 * 60;
        const long secundPerDay = secundPerHour * 24;

        long unixTime = time.ToUnixTimeSeconds();
        this.DateTruncateUnixTime = (unixTime / secundPerDay) * secundPerDay;
        this.HourTruncateUnixTime = (unixTime / secundPerHour) * secundPerHour;
    }
}
