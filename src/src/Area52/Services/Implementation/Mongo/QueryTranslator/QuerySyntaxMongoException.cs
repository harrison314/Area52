using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Area52.Services.Contracts;

namespace Area52.Services.Implementation.Mongo.QueryTranslator;

[Serializable]
public class QuerySyntaxMongoException : Area52QueryException
{
    public QuerySyntaxMongoException()
    {
    }

    public QuerySyntaxMongoException(string? message) : base(message)
    {
    }

    public QuerySyntaxMongoException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    protected QuerySyntaxMongoException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}
