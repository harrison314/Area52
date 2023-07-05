using Area52.Services.Contracts;
using Area52.Services.Implementation.Mongo.Models;
using Area52.Services.Implementation.Raven.Indexes;
using Area52.Services.Implementation.Raven.Models;
using Area52.Services.Implementation.Utils;
using Microsoft.Extensions.Caching.Memory;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Area52.Services.Implementation.Mongo;

public class ApiKeyServices : IApiKeyServices
{
    private const string CacheName = "MongoDB:ApiKeys.1";
    private readonly IMongoDatabase mongoDatabase;
    private readonly IMemoryCache memoryCache;
    private readonly ILogger<ApiKeyServices> logger;

    public ApiKeyServices(IMongoDatabase mongoDatabase, IMemoryCache memoryCache, ILogger<ApiKeyServices> logger)
    {
        this.mongoDatabase = mongoDatabase;
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

        IMongoCollection<MongoApiKeyModel> collection = this.mongoDatabase.GetCollection<MongoApiKeyModel>(CollectionNames.MongoApiKeyModel);
        bool alreadyExist = await collection.AsQueryable().Where(t => t.Name == name || t.Key == key).AnyAsync(cancellationToken);
        if (alreadyExist)
        {
            throw new Exception(""); //TODO
        }


        MongoApiKeyModel model = new MongoApiKeyModel()
        {
            IsEnabled = true,
            Key = key,
            Name = name,
            Metadata = new UserObjectMetadata()
            {
                Created = DateTime.UtcNow,
                CreatedBy = "System",
                CreatedById = "System"
            }
        };

        await collection.InsertOneAsync(model, null, cancellationToken);

        this.logger.LogInformation("Create new Api Key with name {name} and id {apiKeyId}.", model.Name, model.Id);
        await this.RefreshCache(cancellationToken);

        return model.Id.ToString();
    }

    public async Task Delete(string id, CancellationToken cancellationToken)
    {
        this.logger.LogTrace("Enetring to Delete with id {apiKeyId}.", id);

        ObjectId objectId = ObjectId.Parse(id);

        IMongoCollection<MongoApiKeyModel> collection = this.mongoDatabase.GetCollection<MongoApiKeyModel>(CollectionNames.MongoApiKeyModel);
        DeleteResult result = await collection.DeleteOneAsync(t => t.Id == objectId, cancellationToken);
        if (result.DeletedCount != 1)
        {
            //TODO
            throw new Exception("");
        }

        this.logger.LogInformation("Remove ApiKey with id {apiKeyId}.", id);

        await this.RefreshCache(cancellationToken);
    }


    public async Task<IReadOnlyList<ApiKeyData>> ListApiKeys(CancellationToken cancellationToken)
    {
        this.logger.LogTrace("Entering to ListApiKeys");

        IMongoCollection<MongoApiKeyModel> collection = this.mongoDatabase.GetCollection<MongoApiKeyModel>(CollectionNames.MongoApiKeyModel);

        List<MongoApiKeyModel> keys = await collection.AsQueryable().Where(t => t.IsEnabled)
             .OrderBy(t => t.Metadata.Created)
             .ToListAsync(cancellationToken);

        List<ApiKeyData> data = new List<ApiKeyData>(keys.Count);
        foreach (MongoApiKeyModel model in keys)
        {
            data.Add(new ApiKeyData(model.Id.ToString(),
                model.Name,
                model.Key,
                model.Metadata.Created));
        }

        return data;
    }

    public async Task StoreSettings(ApiKeySettings settings, CancellationToken cancellationToken)
    {
        this.logger.LogTrace("Entering to StoreSettings with {IsEnabled}.", settings.IsEnabled);

        IMongoCollection<MongoApiKeySettingsModel> collection = this.mongoDatabase.GetCollection<MongoApiKeySettingsModel>(CollectionNames.MongoApiKeySettingsModel);
        MongoApiKeySettingsModel? apiKeySettings = await collection.AsQueryable()
            .SingleOrDefaultAsync(cancellationToken);

        if (apiKeySettings == null)
        {
            apiKeySettings = new MongoApiKeySettingsModel()
            {
                IsEnabled = settings.IsEnabled,
                Metadata = new UserObjectMetadata()
                {
                    Created = DateTime.UtcNow,
                    CreatedBy = "System",
                    CreatedById = "System"
                }
            };

            await collection.InsertOneAsync(apiKeySettings, new InsertOneOptions(), cancellationToken);
        }
        else
        {
            apiKeySettings.IsEnabled = settings.IsEnabled;
            await collection.ReplaceOneAsync(t => t.Id == apiKeySettings.Id, apiKeySettings);
        }

        this.logger.LogInformation("Save Api key settings (IsEnabled={ApiKeyIsEnabled}).", settings.IsEnabled);
        await this.RefreshCache(cancellationToken);
    }

    public async Task<ApiKeySettings> GetSettings(CancellationToken cancellationToken)
    {
        this.logger.LogTrace("Entering to GetSettings.");

        IMongoCollection<MongoApiKeySettingsModel> collection = this.mongoDatabase.GetCollection<MongoApiKeySettingsModel>(CollectionNames.MongoApiKeySettingsModel);
        MongoApiKeySettingsModel? apiKeySettings = await collection.AsQueryable()
            .SingleOrDefaultAsync(cancellationToken);

        if (apiKeySettings != null)
        {
            return new ApiKeySettings(apiKeySettings.IsEnabled);
        }
        else
        {
            return new ApiKeySettings(false);
        }
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

        IMongoCollection<MongoApiKeySettingsModel> settingsCollection = this.mongoDatabase.GetCollection<MongoApiKeySettingsModel>(CollectionNames.MongoApiKeySettingsModel);
        MongoApiKeySettingsModel? apiKeySettings = await settingsCollection.AsQueryable()
           .SingleOrDefaultAsync(cancellationToken);

        if (apiKeySettings == null)
        {
            return CacheKeysObject.CreateDisabled();
        }

        if (!apiKeySettings.IsEnabled)
        {
            return CacheKeysObject.CreateDisabled();
        }

        IMongoCollection<MongoApiKeyModel> keysCollection = this.mongoDatabase.GetCollection<MongoApiKeyModel>(CollectionNames.MongoApiKeyModel);
        List<string> keys = await keysCollection.AsQueryable().Where(t => t.IsEnabled)
             .Select(t => t.Key)
             .ToListAsync(cancellationToken);

        this.logger.LogDebug("Create a new keys collection.");
        return CacheKeysObject.Create(keys);
    }
}
