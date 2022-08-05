namespace Area52.Services.Contracts;

public class UserObjectMetadata
{
    public DateTime Created
    {
        get;
        set;
    }

    public string CreatedBy
    {
        get;
        set;
    }

    public string CreatedById
    {
        get;
        set;
    }

    public UserObjectMetadata()
    {
        this.CreatedBy = string.Empty;
        this.CreatedById = string.Empty;
    }
}
