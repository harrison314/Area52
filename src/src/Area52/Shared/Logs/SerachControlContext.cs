using Area52.Services.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Area52.Shared.Logs;

public delegate void QueryChangeDelegate(string queryApendix, bool replace, bool exceute);

public class SerachControlContext
{
    public event QueryChangeDelegate? OnQueryChange;

    public SerachControlContext()
    {

    }

    public void AddToQuery(LogEntityProperty property)
    {
        this.OnQueryChange?.Invoke(this.ToQuery(property), false, false);
    }

    public void SearchNow(LogEntityProperty property)
    {
        this.OnQueryChange?.Invoke(this.ToQuery(property), true, true);
    }

    private string ToQuery(LogEntityProperty property)
    {
        if (property.Valued.HasValue)
        {
            return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "{0} is {1}",
                property.Name,
                property.Valued.Value);
        }

        if (property.Values != null)
        {
            return string.Concat(property.Name,
                " is '",
                property.Values.Replace("'", "\\'").Replace("\n", "\\n"),
                "'");
        }

        throw new InvalidProgramException("Missing value of LogEntityProperty.");
    }
}
