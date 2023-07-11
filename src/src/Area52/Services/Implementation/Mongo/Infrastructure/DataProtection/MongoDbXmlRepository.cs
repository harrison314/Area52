using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Area52.Services.Implementation.Mongo.Models;
using Microsoft.AspNetCore.DataProtection.Repositories;
using MongoDB.Driver;

namespace Area52.Services.Implementation.Mongo.Infrastructure.DataProtection;

public class MongoDbXmlRepository : IXmlRepository
{
    private readonly IMongoDatabase mongoDatabase;
    private readonly ILogger<MongoDbXmlRepository> logger;

    public MongoDbXmlRepository(IMongoDatabase mongoDatabase, ILogger<MongoDbXmlRepository> logger)
    {
        this.mongoDatabase = mongoDatabase;
        this.logger = logger;
    }

    public void StoreElement(XElement element, string friendlyName)
    {
        this.logger.LogTrace("Entering to StoreElement with friendlyName={friendlyName}.", friendlyName);

        DataProtectionKey key = new DataProtectionKey()
        {
            Created = DateTime.UtcNow,
            FriendlyName = friendlyName,
            Expiration = this.GetExpiration(element),
            Xml = element.ToString(SaveOptions.DisableFormatting)
        };

        IMongoCollection<DataProtectionKey> collection = this.mongoDatabase.GetCollection<DataProtectionKey>(CollectionNames.DataProtectionKeys);
        collection.InsertOne(key);
    }

    public IReadOnlyCollection<XElement> GetAllElements()
    {
        this.logger.LogTrace("Entering to GetAllElements.");

        DateTime utcNow = DateTime.UtcNow;
        IMongoCollection<DataProtectionKey> collection = this.mongoDatabase.GetCollection<DataProtectionKey>(CollectionNames.DataProtectionKeys);
        List<string> result = collection.AsQueryable().Where(t => t.Expiration == null || t.Expiration > utcNow).Select(t => t.Xml).ToList();
       
        List<XElement> xmlList = new List<XElement>(result.Count);
        xmlList.AddRange(result.Select(t => XElement.Parse(t)));

        return xmlList;
    }

    private DateTime? GetExpiration(XElement element)
    {
        XElement? expirationElement = element.Element("expirationDate");
        if (expirationElement == null)
        {
            this.logger.LogWarning("Expiration date is not present in Xelement.");
            return null;
        }

        if (DateTime.TryParse(expirationElement.Value, out DateTime expiration))
        {
            return expiration.ToUniversalTime();
        }

        return null;
    }

}
