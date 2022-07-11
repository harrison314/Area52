using Area52.Services.Implementation.QueryParser;
using System.Text;

namespace Area52.Services.Implementation.Raven;

internal class DynamicIndexQueryBuilder
{
    private StringBuilder whereClausule;
    private Dictionary<string, object> parameters;
    private int nexParamesterNumber;
    private int? limit = null;
    private string? selectClausule;

    public DynamicIndexQueryBuilder()
    {
        this.whereClausule = new StringBuilder();
        this.selectClausule = "id() as Id, Timestamp, Message, Level";
        this.parameters = new Dictionary<string, object>();
        this.nexParamesterNumber = 0;
    }

    public void SetLimit(int limit)
    {
        this.limit = limit;
    }

    public void SetSelectClausule(string? select)
    {
        this.selectClausule = select;
    }

    public void Add(IAstNode node)
    {
        RqlQueryBuilderContext context = new RqlQueryBuilderContext($"p{this.nexParamesterNumber++}_");
        node.ToRql(context);
        if (this.whereClausule.Length > 0)
        {
            this.whereClausule.AppendLine();
            this.whereClausule.Append("and (");
            context.IntoStringBuilder(whereClausule, this.parameters);
            this.whereClausule.AppendLine(")");
        }
        else
        {
            context.IntoStringBuilder(whereClausule, this.parameters);
        }
    }

    public QueryWithParameters BuildQuery()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("from index 'LogMainIndex'");
        if (this.whereClausule.Length > 0)
        {
            sb.Append("where ");
            sb.Append(this.whereClausule);
            sb.AppendLine();
        }

        sb.AppendLine("order by Timestamp desc");
        if (this.selectClausule != null)
        {
            sb.Append("select ");
            sb.AppendLine(this.selectClausule);
        }

        if (this.limit.HasValue)
        {
            sb.Append("limit ").Append(this.limit.Value).AppendLine();
        }

        return new QueryWithParameters(sb.ToString(), this.parameters);
    }
}
