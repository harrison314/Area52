using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Area52.Services.Contracts;

public interface ILogWriter
{
    Task Write(IReadOnlyList<LogEntity> logs);
}
