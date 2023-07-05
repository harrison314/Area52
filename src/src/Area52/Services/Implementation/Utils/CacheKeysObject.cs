namespace Area52.Services.Implementation.Utils;

internal class CacheKeysObject
{
    private HashSet<string>? keys;

    private CacheKeysObject()
    {

    }

    public static CacheKeysObject CreateDisabled()
    {
        return new CacheKeysObject();
    }

    public static CacheKeysObject Create(IEnumerable<string> keys)
    {
        CacheKeysObject instance = new CacheKeysObject();
        instance.keys = new HashSet<string>(keys, new ConstanstTimeStringComparer());

        return instance;
    }

    public bool ContainsKey(string? key)
    {
        if (key == null && this.keys == null)
        {
            return true;
        }

        //TODO: Key size limits

        if (this.keys != null && key != null && this.keys.Contains(key))
        {
            return true;
        }

        return false;
    }
}
