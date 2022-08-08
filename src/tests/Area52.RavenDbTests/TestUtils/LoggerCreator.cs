using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Area52.RavenDbTests.TestUtils;

internal static class LoggerCreator
{
    public static ILogger<T> Create<T>()
    {
        NullLoggerFactory factory = new NullLoggerFactory();
        return factory.CreateLogger<T>();
    }
}
