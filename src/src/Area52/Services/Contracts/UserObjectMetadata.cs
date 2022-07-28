namespace Area52.Services.Contracts;

public class UserObjectMetadata
{
    public DateTime Created
    {
        get;
        set;
    }

    public string CreatdBy
    {
        get;
        set;
    }

    public string CreatdById
    {
        get;
        set;
    }

    public UserObjectMetadata()
    {
        this.CreatdBy = string.Empty;
        this.CreatdById = string.Empty;
    }
}
