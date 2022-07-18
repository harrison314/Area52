using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization.Attributes;

namespace Area52.Services.Implementation.Mongo.Models;

public class LockAcquire
{
    [BsonId]
    public string Id 
    { 
        get; 
        set; 
    }

    public DateTime ExpiresIn 
    { 
        get;
        set; 
    }

    public bool Acquired 
    { 
        get; 
        set;
    }

    public Guid AcquireId 
    {
        get; 
        set;
    }

    public LockAcquire()
    {
        this.Id = string.Empty;
    }
}
