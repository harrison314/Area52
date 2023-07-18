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

        public DateTime TimestampDateOnly
        {
            get;
            set;
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
                                   Indexing = FieldIndexing.Exact,
                                   Storage = FieldStorage.No,
                                   TermVector = FieldTermVector.No
                               }),
                               LevelExact = this.CreateField("Level", log.Level, new CreateFieldOptions()
                               {
                                   Indexing = FieldIndexing.Default,
                                   Storage = FieldStorage.No,
                                   TermVector = FieldTermVector.No
                               }),
                               //Level = log.Level,
                               LevelNumeric = log.LevelNumeric,
                               Timestamp = log.Timestamp,
                               TimestampDateOnly = log.Timestamp.Date.Date,
                               Message = log.Message,
                               Exception = log.Exception,

                               LogFullText = string.Concat(log.Timestamp.ToString("s"),
                                   " ",
                                   log.Level,
                                   " ",
                                   log.Message,
                                   log.Exception),
                               _Exact = log.Properties.Where(t => t.Values != null).Select(t => this.CreateField(t.Name, t.Values, new CreateFieldOptions()
                               {
                                   Indexing = FieldIndexing.Exact,
                                   Storage = FieldStorage.No,
                                   TermVector = FieldTermVector.No
                               })),
                               _ = log.Properties.Where(t => t.Values != null).Select(t => this.CreateField(t.Name, t.Values, new CreateFieldOptions()
                               {
                                   Indexing = FieldIndexing.Default,
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

        //this.Index(t => t.Level, FieldIndexing.Default);
        //this.Index(t => t.Level, FieldIndexing.Exact);
        //this.Index("LevelNumeric", FieldIndexing.Exact);


        this.Index("LevelNumeric", FieldIndexing.Exact);
        this.Index("LogFullText", FieldIndexing.Search);

        //this.Store(t => t.Timestamp, FieldStorage.Yes);
        //this.Store(t => t.Level, FieldStorage.Yes);
        //this.Store(t => t.Message, FieldStorage.Yes);
    }
}
