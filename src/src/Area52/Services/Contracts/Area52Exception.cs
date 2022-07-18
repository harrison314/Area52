using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Area52.Services.Contracts;

[Serializable]
public class Area52Exception : ApplicationException
{
    public Area52Exception()
    {
    }

    public Area52Exception(string? message)
        : base(message)
    {
    }

    public Area52Exception(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }

    protected Area52Exception(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}
