using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Area52.Services.Implementation;

internal static class FormatConstants
{
    /// <summary>
    /// Sortable string format for show in GUI.
    /// </summary>
    public const string ShowSortableDateTimeFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'ffff";
    
    /// <summary>
    /// Sortable string format for UTC time into database.
    /// </summary>
    public const string SortableDateTimeFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffffff'Z'";
}
