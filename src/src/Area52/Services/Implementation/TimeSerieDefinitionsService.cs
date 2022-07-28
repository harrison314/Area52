using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Area52.Services.Contracts;
using Area52.Services.Implementation.QueryParser.Nodes;

namespace Area52.Services.Implementation;

public class TimeSerieDefinitionsService : ITimeSerieDefinitionsService
{
    private readonly ITimeSerieDefinitionsRepository repository;

    public TimeSerieDefinitionsService(ITimeSerieDefinitionsRepository repository)
    {
        this.repository = repository;
    }

    public void CheckQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new Area52QueryException("Query can not be empty string.");
        }

        QueryParser.IAstNode queryNodes = QueryParser.Parser.SimpleParse(query);
        TimeSeriesDefinicitonCheckVisitor timeSeriesDefinicitonCheckVisitor = new TimeSeriesDefinicitonCheckVisitor();
        timeSeriesDefinicitonCheckVisitor.Visit(queryNodes);
    }

    public Task<string> Create(TimeSerieDefinition timeSerieDefinition)
    {

        this.CheckQuery(timeSerieDefinition.Query);
        timeSerieDefinition.ValueFieldName = string.IsNullOrWhiteSpace(timeSerieDefinition.ValueFieldName) ? null : timeSerieDefinition.ValueFieldName.Trim();
        timeSerieDefinition.TagFieldName = string.IsNullOrWhiteSpace(timeSerieDefinition.TagFieldName) ? null : timeSerieDefinition.TagFieldName.Trim();
        timeSerieDefinition.Enabled = true;

        timeSerieDefinition.Metadata = new UserObjectMetadata()
        {
            Created = DateTime.UtcNow,
            CreatdBy = "System", //TODO
            CreatdById = "System"
        };

        return this.repository.Create(timeSerieDefinition);
    }

    public Task<TimeSerieDefinition> FindById(string id)
    {
        return this.repository.FindById(id);
    }

    public Task<IReadOnlyList<TimeSerieDefinitionInfo>> FindDefinictions()
    {
        return this.repository.FindDefinictions();
    }
}

internal class TimeSeriesDefinicitonCheckVisitor : QueryParser.AstNodeVisitor
{
    public TimeSeriesDefinicitonCheckVisitor()
    {

    }

    protected override void VisitInternal(PropertyNode node)
    {
        if (node.Name == nameof(LogEntity.Timestamp))
        {
            throw new Area52QueryException($"Property {nameof(LogEntity.Timestamp)} is not allowed in this query.");
        }

        base.VisitInternal(node);
    }
}
