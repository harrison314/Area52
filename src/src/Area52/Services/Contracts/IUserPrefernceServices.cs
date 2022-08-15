using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Area52.Services.Contracts;

public interface IUserPrefernceServices
{
    Task SaveQueries(List<SturedQuery> queries, CancellationToken cancellationToken);

    Task<IReadOnlyList<SturedQuery>> GetQueries(CancellationToken cancellationToken);
}

public record SturedQuery(string Name, string Query);
