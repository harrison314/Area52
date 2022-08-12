using Area52.Services.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Area52.Shared.ForLogs;

public delegate void QueryChangeDelegate(string queryApendix, bool replace, bool exceute);

public class SearchControlContext
{
    public event QueryChangeDelegate? OnQueryChange;

    public SearchControlContext()
    {

    }

    public void AddToQuery(LogEntityProperty property, string? binOperator = null)
    {
        this.OnQueryChange?.Invoke(this.ToQuery(property, binOperator), false, false);
    }

    public void SearchNow(LogEntityProperty property, string? binOperator = null)
    {
        this.OnQueryChange?.Invoke(this.ToQuery(property, binOperator), true, true);
    }

    private string ToQuery(LogEntityProperty property, string? binOperator)
    {
        if (binOperator == null)
        {
            binOperator = "is";
        }

        if (property.Valued.HasValue)
        {
            return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "{0} {1} {2}",
                property.Name,
                binOperator,
                property.Valued.Value);
        }

        if (property.Values != null)
        {
            return string.Concat(property.Name,
                " ",
                binOperator,
                " '",
                property.Values.Replace("'", "\\'").Replace("\n", "\\n"),
                "'");
        }

        throw new InvalidProgramException("Missing value of LogEntityProperty.");
    }
}
