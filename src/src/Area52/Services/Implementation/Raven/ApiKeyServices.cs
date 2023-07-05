using Area52.Services.Contracts;
using Area52.Services.Implementation.Raven.Indexes;
using Area52.Services.Implementation.Raven.Models;
using Area52.Services.Implementation.Utils;
using Microsoft.Extensions.Caching.Memory;
using Raven.Client.Documents;

namespace Area52.Services.Implementation.Raven;

public class ApiKeyServices : IApiKeyServices
{
    private const string CacheName = "RavenDb:ApiKeys.1";
    private readonly IDocumentStore documentStore;
    private readonly IMemoryCache memoryCache;
    private readonly ILogger<ApiKeyServices> logger;

    public ApiKeyServices(IDocumentStore documentStore, IMemoryCache memoryCache, ILogger<ApiKeyServices> logger)
    {
        this.documentStore = documentStore;
        this.memoryCache = memoryCache;
        this.logger = logger;
    }

    public async Task<string> Create(string name, string? key, CancellationToken cancellationToken)
    {
        this.logger.LogTrace("Entering to Create with name {name}, keyValue {keyValue}.", name, key);

        if (string.IsNullOrEmpty(key))
        {
            key = ApiKeyBuilder.BuildApiKey();
        }

        using var session = this.documentStore.OpenAsyncSession();
        bool alreadyExist = await session.Query<ApiKeyModelIndex.Result, ApiKeyModelIndex>().AnyAsync(t => t.Name == name || t.Key == key, cancellationToken);
        if (alreadyExist)
        {
            throw new Exception(""); //TODO
        }

        ApiKeyModel model = new ApiKeyModel()
        {
            IsEnabled = true,
            Name = name,
            Key = key,
            Metadata = new UserObjectMetadata()
            {
                Created = DateTime.UtcNow,
                CreatedBy = "System",
                CreatedById = "System"
            }
        };

        await session.StoreAsync(model, cancellationToken);
        await session.SaveChangesAsync(cancellationToken);

        this.logger.LogInformation("Create new Api Key with name {name} and id {apiKeyId}.", model.Name, model.Id);
        await this.RefreshCache(cancellationToken);

        return model.Id;
    }

    public async Task Delete(string id, CancellationToken cancellationToken)
    {
        this.logger.LogTrace("Enetring to Delete with id {apiKeyId}.", id);

        using var session = this.documentStore.OpenAsyncSession();
        ApiKeyModel? model = await session.LoadAsync<ApiKeyModel>(id, cancellationToken);
        if (model == null)
        {
            //TODO: vynimka
        }

        session.Delete(model);
        await session.SaveChangesAsync(cancellationToken);
        this.logger.LogInformation("Remove ApiKey with id {apiKeyId}.", id);

        await this.RefreshCache(cancellationToken);
    }

    public async Task<IReadOnlyList<ApiKeyData>> ListApiKeys(CancellationToken cancellationToken)
    {
        this.logger.LogTrace("Entering to ListApiKeys");

        using var session = this.documentStore.OpenAsyncSession();
        List<ApiKeyModel> keys = await session.Query<ApiKeyModelIndex.Result, ApiKeyModelIndex>()
            .OrderBy(t => t.Created)
            .OfType<ApiKeyModel>()
            .ToListAsync(cancellationToken);

        List<ApiKeyData> data = new List<ApiKeyData>(keys.Count);
        foreach (ApiKeyModel model in keys)
        {
            data.Add(new ApiKeyData(model.Id,
                model.Name,
                model.Key,
                model.Metadata.Created));
        }

        return data;
    }

    public async Task<ApiKeySettings> GetSettings(CancellationToken cancellationToken)
    {
        this.logger.LogTrace("Entering to GetSettings");

        using var session = this.documentStore.OpenAsyncSession();
        ApiKeySettingsModel? apiKeySettings = await session.Query<ApiKeySettingsModel>()
            .FirstOrDefaultAsync(cancellationToken);

        if (apiKeySettings == null)
        {
            return new ApiKeySettings(false);
        }
        else
        {
            return new ApiKeySettings(apiKeySettings.IsEnabled);
        }
    }

    public async Task StoreSettings(ApiKeySettings settings, CancellationToken cancellationToken)
    {
        this.logger.LogTrace("Entering to StoreSettings with {IsEnabled}.", settings.IsEnabled);

        using var session = this.documentStore.OpenAsyncSession();
        ApiKeySettingsModel? apiKeySettings = await session.Query<ApiKeySettingsModel>()
            .FirstOrDefaultAsync(cancellationToken);

        if (apiKeySettings == null)
        {
            apiKeySettings = new ApiKeySettingsModel()
            {
                Metadata = new UserObjectMetadata()
                {
                    Created = DateTime.UtcNow,
                    CreatedBy = "System",
                    CreatedById = "System"
                }
            };

            await session.StoreAsync(apiKeySettings, cancellationToken);
        }

        apiKeySettings.IsEnabled = settings.IsEnabled;

        await session.SaveChangesAsync(cancellationToken);

        this.logger.LogInformation("Save Api key settings (IsEnabled={ApiKeyIsEnabled}).", settings.IsEnabled);

        await this.RefreshCache(cancellationToken);
    }

    public async ValueTask<bool> VerifyApiKey(string? key, CancellationToken cancellationToken)
    {
        this.logger.LogTrace("Entering to VerifyApiKey");

        CacheKeysObject cacheKeysObject;

        if (this.memoryCache.TryGetValue<CacheKeysObject>(CacheName, out cacheKeysObject))
        {
            return cacheKeysObject.ContainsKey(key);
        }

        cacheKeysObject = await this.memoryCache.GetOrCreateAsync<CacheKeysObject>(CacheName, async entity =>
        {
            CacheKeysObject keys = await this.GetCachedKeys(cancellationToken).ConfigureAwait(false);
            entity.AbsoluteExpirationRelativeToNow = ApiKeyServicesConstants.CacheTimeout;

            return keys;
        });

        return cacheKeysObject.ContainsKey(key);

        //TODO: Check Api key in database?
    }

    private async Task RefreshCache(CancellationToken cancellationToken)
    {
        this.logger.LogTrace("Entering to RefreshCache.");

        this.memoryCache.Remove(CacheName);
        _ = await this.memoryCache.GetOrCreateAsync<CacheKeysObject>(CacheName, async entity =>
        {
            try
            {
                CacheKeysObject keys = await this.GetCachedKeys(cancellationToken).ConfigureAwait(false);
                entity.AbsoluteExpirationRelativeToNow = ApiKeyServicesConstants.CacheTimeout;

                this.logger.LogDebug("RefreshCache for Api keys.");
                return keys;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error during RefreshCache.");
                throw;
            }
        });
    }

    private async Task<CacheKeysObject> GetCachedKeys(CancellationToken cancellationToken)
    {
        this.logger.LogTrace("Entering to GetCachedKeys.");

        using var session = this.documentStore.OpenAsyncSession();
        ApiKeySettingsModel? apiKeySettings = await session.Query<ApiKeySettingsModel>()
            .FirstOrDefaultAsync(cancellationToken);

        if (apiKeySettings == null)
        {
            return CacheKeysObject.CreateDisabled();
        }

        if (!apiKeySettings.IsEnabled)
        {
            return CacheKeysObject.CreateDisabled();
        }

        List<string> keys = await session.Query<ApiKeyModelIndex.Result, ApiKeyModelIndex>()
            .Select(t => t.Key)
            .ToListAsync(cancellationToken);

        this.logger.LogDebug("Create a new keys collection.");
        return CacheKeysObject.Create(keys);
    }
}
