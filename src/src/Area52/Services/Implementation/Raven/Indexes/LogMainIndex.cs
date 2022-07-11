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
        this.Map = logs => from log in logs
                           select new
                           {
                               Level = log.Level,
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

                               _ = log.Properties.Select(t => this.CreateField(t.Name, (object)t.Values ?? (object)t.Valued.Value, new CreateFieldOptions()
                               {
                                   Indexing = FieldIndexing.Exact,
                                   Storage = FieldStorage.Yes,
                                   TermVector = FieldTermVector.No
                               }))
                           };

        this.Index("LevelNumeric", FieldIndexing.Exact);
        this.Index("LogFullText", FieldIndexing.Search);
        this.TermVector("Message", FieldTermVector.Yes);
    }
}
