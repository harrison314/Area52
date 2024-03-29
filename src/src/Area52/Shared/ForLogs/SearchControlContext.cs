﻿using Area52.Services.Contracts;
using Area52.Services.Implementation;
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

    public void AddToQuery(DateTimeOffset logTime, int secunds)
    {
        this.OnQueryChange?.Invoke(this.ToNearQuery(logTime, secunds), false, false);
    }

    public void AddToQuery(DateTimeOffset logTime, int secunds, bool execute)
    {
        this.OnQueryChange?.Invoke(this.ToNearQuery(logTime, secunds), false, execute);
    }

    public void SearchNow(LogEntityProperty property, string? binOperator = null)
    {
        this.OnQueryChange?.Invoke(this.ToQuery(property, binOperator), true, true);
    }

    public void SearchNow(string query)
    {
        this.OnQueryChange?.Invoke(query, true, true);
    }

    public void SearchNow(DateTimeOffset logTime, int secunds)
    {
        this.OnQueryChange?.Invoke(this.ToNearQuery(logTime, secunds), true, true);
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
                this.EscapePropertyName(property.Name),
                binOperator,
                property.Valued.Value);
        }

        if (property.Values != null)
        {
            return string.Concat(this.EscapePropertyName(property.Name),
                " ",
                binOperator,
                " '",
                property.Values.Replace("'", "\\'").Replace("\n", "\\n"),
                "'");
        }

        throw new InvalidProgramException("Missing value of LogEntityProperty.");
    }

    private string ToNearQuery(DateTimeOffset time, int secunds)
    {
        TimeSpan interval = TimeSpan.FromSeconds(secunds);
        return $"Timestamp between '{(time - interval).ToString(FormatConstants.SortableDateTimeFormat)}' and '{(time + interval).ToString(FormatConstants.SortableDateTimeFormat)}'";
    }

    private string EscapePropertyName(string propetyName)
    {
        bool contaisOnlyDigits = true;
        bool containsWhitespace = false;

        for (int i = 0; i < propetyName.Length; i++)
        {
            contaisOnlyDigits &= char.IsDigit(propetyName, i);
            containsWhitespace |= char.IsWhiteSpace(propetyName, i);
            if (containsWhitespace)
            {
                break;
            }
        }

        if (contaisOnlyDigits || containsWhitespace)
        {
            return string.Concat("[", propetyName, "]");
        }

        return propetyName;
    }
}
