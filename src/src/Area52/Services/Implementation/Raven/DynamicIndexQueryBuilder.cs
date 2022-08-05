using Area52.Services.Implementation.QueryParser;
using System.Text;

namespace Area52.Services.Implementation.Raven;

internal class DynamicIndexQueryBuilder
{
    private StringBuilder whereClause;
    private Dictionary<string, object> parameters;
    private int nexParameterNumber;
    private int? limit = null;
    private string? selectClause;

    public DynamicIndexQueryBuilder()
    {
        this.whereClause = new StringBuilder();
        this.selectClause = "id() as Id, Timestamp, Message, Level";
        this.parameters = new Dictionary<string, object>();
        this.nexParameterNumber = 0;
    }

    public void SetLimit(int limit)
    {
        this.limit = limit;
    }

    public void SetSelectClause(string? select)
    {
        this.selectClause = select;
    }

    public void Add(IAstNode node)
    {
        RqlQueryBuilderContext context = new RqlQueryBuilderContext($"p{this.nexParameterNumber++}_");
        node.ToRql(context);
        if (this.whereClause.Length > 0)
        {
            this.whereClause.AppendLine();
            this.whereClause.Append("and (");
            context.IntoStringBuilder(this.whereClause, this.parameters);
            this.whereClause.AppendLine(")");
        }
        else
        {
            context.IntoStringBuilder(this.whereClause, this.parameters);
        }
    }

    public QueryWithParameters BuildQuery()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("from index 'LogMainIndex'");
        if (this.whereClause.Length > 0)
        {
            sb.Append("where ");
            sb.Append(this.whereClause);
            sb.AppendLine();
        }

        sb.AppendLine("order by Timestamp desc");
        if (this.selectClause != null)
        {
            sb.Append("select ");
            sb.AppendLine(this.selectClause);
        }

        if (this.limit.HasValue)
        {
            sb.Append("limit ").Append(this.limit.Value).AppendLine();
        }

        return new QueryWithParameters(sb.ToString(), this.parameters);
    }
}
