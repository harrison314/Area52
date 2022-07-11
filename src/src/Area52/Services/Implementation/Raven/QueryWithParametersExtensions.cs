using Raven.Client.Documents.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Area52.Services.Implementation.Raven;

internal static class QueryWithParametersExtensions
{
    public static void SetParameters<T>(this QueryWithParameters parameters, IAsyncRawDocumentQuery<T> rawDocumentQuery)
    {
        foreach ((string key, object value) in parameters.Parameters)
        {
            rawDocumentQuery.AddParameter(key, value);
        }
    }
}
