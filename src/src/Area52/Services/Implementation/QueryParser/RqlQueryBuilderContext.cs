using System.Text;

namespace Area52.Services.Implementation.QueryParser;

public class RqlQueryBuilderContext
{
    private readonly StringBuilder sb;
    private readonly string parameterPrefix;
    private Dictionary<string, object> parameters;
    private int parameterNumber;

    public string FulltextPropertyName
    {
        get => "LogFullText";
    }

    public RqlQueryBuilderContext(string parameterPrefix)
    {
        this.sb = new StringBuilder();
        this.parameters = new Dictionary<string, object>();
        this.parameterNumber = 0;
        this.parameterPrefix = parameterPrefix;
    }

    public void Append(string chunk)
    {
        this.sb.Append(chunk);
    }

    public void Append(char chunk)
    {
        this.sb.Append(chunk);
    }

    public string AddParameterWithValue(object value)
    {
        int pn = this.parameterNumber;
        this.parameterNumber++;

        string name = string.Format(System.Globalization.CultureInfo.InvariantCulture,
            "{0}{1}",
            this.parameterPrefix,
            pn);

        this.parameters.Add(name, value);

        return name;
    }

    public void IntoStringBuilder(StringBuilder destination, Dictionary<string, object> parameters)
    {
        destination.Append(this.sb);

        foreach ((string key, object value) in this.parameters)
        {
            parameters.Add(key, value);
        }
    }

    public override string ToString()
    {
        return this.sb.ToString();
    }
}
