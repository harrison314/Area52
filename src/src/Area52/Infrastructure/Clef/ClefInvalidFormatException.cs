using System.Runtime.Serialization;

namespace Area52.Infrastructure.Clef;

[Serializable]
public class ClefInvalidFormatException : FormatException
{
    public ClefInvalidFormatException()
    {
    }

    public ClefInvalidFormatException(string? message) 
        : base(message)
    {
    }

    public ClefInvalidFormatException(string? message, Exception? innerException) 
        : base(message, innerException)
    {
    }

    protected ClefInvalidFormatException(SerializationInfo info, StreamingContext context) 
        : base(info, context)
    {
    }
}
