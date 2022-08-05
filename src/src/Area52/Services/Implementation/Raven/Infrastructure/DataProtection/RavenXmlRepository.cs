using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Area52.Services.Implementation.Raven.Models;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Raven.Client.Documents;

namespace Area52.Services.Implementation.Raven.Infrastructure.DataProtection;

public class RavenXmlRepository : IXmlRepository
{
    private readonly IDocumentStore documentStore;
    private readonly ILogger<RavenXmlRepository> logger;

    public RavenXmlRepository(IDocumentStore documentStore, ILogger<RavenXmlRepository> logger)
    {
        this.documentStore = documentStore;
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

        using global::Raven.Client.Documents.Session.IDocumentSession session = this.documentStore.OpenSession();
        session.Store(key);
        if (key.Expiration.HasValue)
        {
            DateTime dbExpiration = key.Expiration.Value + TimeSpan.FromDays(31.0);
            session.Advanced.GetMetadataFor(key)[global::Raven.Client.Constants.Documents.Metadata.Expires] = dbExpiration;
        }

        session.SaveChanges();
    }

    public IReadOnlyCollection<XElement> GetAllElements()
    {
        this.logger.LogTrace("Entering to GetAllElements.");

        using global::Raven.Client.Documents.Session.IDocumentSession session = this.documentStore.OpenSession();
        var result = session.Query<DataProtectionKey>().Select(t => t.Xml).ToList();

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
