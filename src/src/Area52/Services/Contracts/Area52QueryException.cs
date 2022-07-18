using System.Runtime.Serialization;

namespace Area52.Services.Contracts;

[Serializable]
public class Area52QueryException : Area52Exception
{
    public Area52QueryException()
    {
    }

    public Area52QueryException(string? message)
        : base(message)
    {
    }

    public Area52QueryException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }

    protected Area52QueryException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}
