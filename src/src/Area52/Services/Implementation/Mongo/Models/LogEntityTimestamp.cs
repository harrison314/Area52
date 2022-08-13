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

    public LogEntityTimestamp()
    {
        this.Sortable = string.Empty;
    }

    public LogEntityTimestamp(DateTimeOffset time)
    {
        this.Utc = time.UtcDateTime;
        this.Sortable = time.ToString(FormatConstants.SortableDateTimeFormat);
    }
}
