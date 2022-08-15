using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Area52.Services.Contracts;
using Area52.Services.Implementation.Raven.Indexes;
using Area52.Services.Implementation.Raven.Models;
using Raven.Client.Documents;

namespace Area52.Services.Implementation.Raven;

public class UserPrefernceServices : IUserPrefernceServices
{
    private readonly IDocumentStore documentStore;
    private readonly ILogger<UserPrefernceServices> logger;

    public UserPrefernceServices(IDocumentStore documentStore, ILogger<UserPrefernceServices> logger)
    {
        this.documentStore = documentStore;
        this.logger = logger;
    }

    public async Task<IReadOnlyList<SturedQuery>> GetQueries(CancellationToken cancellationToken)
    {
        this.logger.LogTrace("Entering to GetQueries.");

        using var session = this.documentStore.OpenAsyncSession();
        UserPrefernce? prefernces = await session.Query<UserPrefernceIndex.Result, UserPrefernceIndex>()
            .Where(t => t.UserId == "System")
            .OfType<UserPrefernce>()
            .FirstOrDefaultAsync(cancellationToken);

        return (prefernces == null) ? new List<SturedQuery>() : prefernces.SturedQuerys;
    }

    public async Task SaveQueries(List<SturedQuery> queries, CancellationToken cancellationToken)
    {
        this.logger.LogTrace("Entering to SaveQueries.");

        using var session = this.documentStore.OpenAsyncSession();
        UserPrefernce? prefernces = await session.Query<UserPrefernceIndex.Result, UserPrefernceIndex>()
            .Where(t => t.UserId == "System")
            .OfType<UserPrefernce>()
            .FirstOrDefaultAsync(cancellationToken);

        if (prefernces == null)
        {
            prefernces = new UserPrefernce()
            {
                Metadata = new UserObjectMetadata()
                {
                    Created = DateTime.UtcNow,
                    CreatedBy = "System",
                    CreatedById = "System"
                }
            };

            await session.StoreAsync(prefernces, cancellationToken);
        }

        prefernces.SturedQuerys = queries;
        await session.SaveChangesAsync(cancellationToken);

        this.logger.LogInformation("Save stored queries.");
    }
}
