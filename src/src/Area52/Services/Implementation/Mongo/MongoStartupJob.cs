using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Area52.Services.Contracts;
using Area52.Services.Implementation.Mongo.Models;
using MongoDB.Driver;

namespace Area52.Services.Implementation.Mongo;

public class MongoStartupJob : IStartupJob
{
    private readonly IMongoDatabase mongoDatabase;
    private readonly ILogger<MongoStartupJob> logger;

    public MongoStartupJob(IMongoDatabase mongoDatabase, ILogger<MongoStartupJob> logger)
    {
        this.mongoDatabase = mongoDatabase;
        this.logger = logger;
    }

    public async ValueTask Execute(CancellationToken cancellationToken)
    {
        this.logger.LogTrace("Entering to Execute.");

        using IAsyncCursor<string> cl = await this.mongoDatabase.ListCollectionNamesAsync(null, cancellationToken);
        List<string> collections = await cl.ToListAsync(cancellationToken);

        if (!collections.Contains("_Migrations"))
        {
            await this.mongoDatabase.CreateCollectionAsync("_Migrations", null, cancellationToken);
            this.logger.LogInformation("Created MongoDB collection {collectionName}.", "_Migrations");
        }

        await this.MigrationUp("Initial", this.Migration_Initial, cancellationToken);
    }

    private async Task MigrationUp(string migrationUniqName, Func<CancellationToken, Task> migration, CancellationToken cancellationToken)
    {
        this.logger.LogTrace("Entering to MigrationUp with {migrationUniqName}.", migrationUniqName);
        IMongoCollection<Models.DbMigration> collection = this.mongoDatabase.GetCollection<Models.DbMigration>("_Migrations");
        using IAsyncCursor<Models.DbMigration> cl = await collection.FindAsync(t => t.Id == migrationUniqName, cancellationToken: cancellationToken);

        if (!await cl.AnyAsync(cancellationToken))
        {
            this.logger.LogDebug("Starting executing migration {migrationUniqName}.", migrationUniqName);
            await migration.Invoke(cancellationToken);

            await collection.InsertOneAsync(new Models.DbMigration()
            {
                Id = migrationUniqName,
                Executed = DateTime.UtcNow,
                AppVersion = typeof(MongoStartupJob).Assembly!.GetName()!.Version!.ToString()
            });

            this.logger.LogTrace("Executed migration {migrationUniqName}.", migrationUniqName);
        }
    }

    private async Task Migration_Initial(CancellationToken cancellationToken)
    {
        await this.mongoDatabase.CreateCollectionAsync(CollectionNames.LogEntities, null, cancellationToken);

        var collection = this.mongoDatabase.GetCollection<Models.MongoLogEntity>(CollectionNames.LogEntities);

        await collection.Indexes.CreateOneAsync(new CreateIndexModel<Models.MongoLogEntity>(
             new BsonDocumentIndexKeysDefinition<Models.MongoLogEntity>(new MongoDB.Bson.BsonDocument()
             {
                {"Properties.Name", 1 },
                {"Properties.Values", 1 },
                {"Properties.ValuesLower", 1 },
                {"Properties.Valued", 1 },
             }),
             new CreateIndexOptions()
             {
                 Background = true,
                 Name = "LogEntitys_Properties_IX"
             }));

        await collection.Indexes.CreateOneAsync(new CreateIndexModel<Models.MongoLogEntity>(
             new BsonDocumentIndexKeysDefinition<Models.MongoLogEntity>(new MongoDB.Bson.BsonDocument()
             {
                {"TimestampIndex.Utc", -1 },
             }),
             new CreateIndexOptions()
             {
                 Background = true,
                 Name = "LogEntitys_TimestampIndexUtc_IX"
             }));

        await collection.Indexes.CreateOneAsync(new CreateIndexModel<Models.MongoLogEntity>(
             new BsonDocumentIndexKeysDefinition<Models.MongoLogEntity>(new MongoDB.Bson.BsonDocument()
             {
                {"TimestampIndex.Sortable", -1 },
             }),
             new CreateIndexOptions()
             {
                 Background = true,
                 Name = "LogEntitys_TimestampIndexSortable_IX"
             }));

        await collection.Indexes.CreateOneAsync(new CreateIndexModel<Models.MongoLogEntity>(
            new BsonDocumentIndexKeysDefinition<Models.MongoLogEntity>(new MongoDB.Bson.BsonDocument()
            {
                {"Level",1 },
            }),
            new CreateIndexOptions()
            {
                Background = true,
                Name = "LogEntitys_Level_IX"
            }));

        await collection.Indexes.CreateOneAsync(new CreateIndexModel<Models.MongoLogEntity>(
            new BsonDocumentIndexKeysDefinition<Models.MongoLogEntity>(new MongoDB.Bson.BsonDocument()
            {
                {"LevelLower",1 },
            }),
            new CreateIndexOptions()
            {
                Background = true,
                Name = "LogEntitys_LevelLower_IX"
            }));

        await collection.Indexes.CreateOneAsync(new CreateIndexModel<Models.MongoLogEntity>(
            new BsonDocumentIndexKeysDefinition<Models.MongoLogEntity>(new MongoDB.Bson.BsonDocument()
            {
                {"LevelNumeric",1 },
            }),
            new CreateIndexOptions()
            {
                Background = true,
                Name = "LogEntitys_LevelNumeric_IX"
            }));

        await collection.Indexes.CreateOneAsync(new CreateIndexModel<Models.MongoLogEntity>(
            new BsonDocumentIndexKeysDefinition<Models.MongoLogEntity>(new MongoDB.Bson.BsonDocument()
            {
                {"LogFullText", "text" },
            }),
            new CreateIndexOptions()
            {
                Background = true,
                Name = "LogEntitys_Fulltext_IX",
                TextIndexVersion = 3
            }));

        await this.mongoDatabase.CreateCollectionAsync(CollectionNames.LockAcquires, null, cancellationToken);
        //await this.mongoDatabase.CreateCollectionAsync(CollectionNames.DataProtectionKeys, new CreateCollectionOptions(), cancellationToken);
        await this.mongoDatabase.CreateCollectionAsync(CollectionNames.MongoTimeSeriesDefinition, null, cancellationToken);

        var mongoTimeSerieDefinitionCollection = this.mongoDatabase.GetCollection<Models.MongoTimeSerieDefinition>(CollectionNames.LogEntities);
        await mongoTimeSerieDefinitionCollection.Indexes.CreateOneAsync(new CreateIndexModel<Models.MongoTimeSerieDefinition>(
           new BsonDocumentIndexKeysDefinition<Models.MongoTimeSerieDefinition>(new MongoDB.Bson.BsonDocument()
           {
                {"Enabled",1 },
                {"Metadata.Created", -1 },
           }),
           new CreateIndexOptions()
           {
               Background = false,
               Name = "MongoTimeSerieDefinition_IX"
           }));

        await this.mongoDatabase.CreateCollectionAsync(CollectionNames.MongoTimeSeriesItems,
            new CreateCollectionOptions<Models.MongoTimeSerieItem>()
            {
                TimeSeriesOptions = new TimeSeriesOptions(nameof(Models.MongoTimeSerieItem.Timestamp), new Optional<string>(nameof(Models.MongoTimeSerieItem.Meta))),
            },
            cancellationToken);

        await this.mongoDatabase.CreateCollectionAsync(CollectionNames.MongoUserPrefernce, null, cancellationToken);

        var mongoUserPreferenceCollection = this.mongoDatabase.GetCollection<Models.MongoUserPrefernce>(CollectionNames.MongoUserPrefernce);
        await mongoUserPreferenceCollection.Indexes.CreateOneAsync(new CreateIndexModel<Models.MongoUserPrefernce>(
           new BsonDocumentIndexKeysDefinition<Models.MongoUserPrefernce>(new MongoDB.Bson.BsonDocument()
           {
                {"Metadata.CreatedById", 1 },
           }),
           new CreateIndexOptions()
           {
               Background = false,
               Name = "MongoUserPrefernce_IX"
           }));

        await this.mongoDatabase.CreateCollectionAsync(CollectionNames.MongoApiKeySettingsModel, null, cancellationToken);
        await this.mongoDatabase.CreateCollectionAsync(CollectionNames.MongoApiKeyModel, null, cancellationToken);

    }
}
