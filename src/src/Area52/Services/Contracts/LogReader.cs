using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Area52.Services.Contracts;

public interface ILogReader
{
    Task<ReadLastLogResult> ReadLastLogs(string query);

    Task<LogEntity> LoadLogInfo(string id);

    IAsyncEnumerable<LogEntity> ReadLogs(string query, int? limit);
}
