namespace Area52.Services.Contracts;

public interface IApiKeyServices
{
    Task<ApiKeySettings> GetSettings(CancellationToken cancellationToken);

    Task StoreSettings(ApiKeySettings settings, CancellationToken cancellationToken);

    Task<string> Create(string name, string? key, CancellationToken cancellationToken);

    Task Delete(string id, CancellationToken cancellationToken);

    Task<IReadOnlyList<ApiKeyData>> ListApiKeys(CancellationToken cancellationToken);

    ValueTask<bool> VerifyApiKey(string? key, CancellationToken cancellationToken);
}

public record ApiKeySettings(bool IsEnabled);

public record ApiKeyData(string Id, string Name, string Key, DateTime Created);

internal class ApiKeyServicesConstants
{
    public static readonly TimeSpan CacheTimeout = TimeSpan.FromMinutes(5.0);
}
