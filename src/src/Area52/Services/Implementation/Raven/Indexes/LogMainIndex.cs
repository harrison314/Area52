using Area52.Services.Contracts;
using Raven.Client.Documents.Indexes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Area52.Services.Implementation.Raven.Indexes;

public class LogMainIndex : AbstractIndexCreationTask<LogEntity>
{
    public class Result
    {
        public string Level
        {
            get;
            init;
        }

        public int LevelNumeric
        {
            get;
            init;
        }

        public DateTimeOffset Timestamp
        {
            get;
            init;
        }

        public string Message
        {
            get;
            init;
        }

        public string? Exception
        {
            get;
            init;
        }

        public Result()
        {
            this.Level = string.Empty;
            this.Message = string.Empty;
        }
    }

    public LogMainIndex()
    {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
        this.Map = logs => from log in logs
                           select new
                           {
                               Level = this.CreateField("Level", log.Level, new CreateFieldOptions()
                               {
                                   Indexing = FieldIndexing.Exact | FieldIndexing.Default,
                                   Storage = FieldStorage.No,
                                   TermVector = FieldTermVector.No
                               }),
                               LevelNumeric = log.LevelNumeric,
                               Timestamp = log.Timestamp,
                               Message = log.Message,
                               Exception = log.Exception,

                               LogFullText = string.Concat(log.Timestamp.ToString("s"),
                                   " ",
                                   log.Level,
                                   " ",
                                   log.Message,
                                   log.Exception),

                               _ = log.Properties.Where(t => t.Values != null).Select(t => this.CreateField(t.Name, t.Values, new CreateFieldOptions()
                               {
                                   Indexing = FieldIndexing.Exact | FieldIndexing.Default,
                                   Storage = FieldStorage.No,
                                   TermVector = FieldTermVector.No
                               })),
                               _Numbers = log.Properties.Where(t => t.Valued.HasValue).Select(t => this.CreateField(t.Name, t.Valued!.Value, new CreateFieldOptions()
                               {
                                   Indexing = FieldIndexing.Exact,
                                   Storage = FieldStorage.No,
                                   TermVector = FieldTermVector.No
                               })),
                           };
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

        this.Index("LevelNumeric", FieldIndexing.Exact);
        this.Index("LogFullText", FieldIndexing.Search);
    }
}
