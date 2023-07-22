using System.Globalization;
using Raven.Client.Documents.Commands;

namespace Area52.Shared;

internal static class FormatingExtensions
{
    private static readonly NumberFormatInfo customLongNumberInfo = CreateCustomLongNumberInfo();

    public static string FormatLargeNumber(this long value)
    {
        return value.ToString("n", customLongNumberInfo);
    }

    private static NumberFormatInfo CreateCustomLongNumberInfo()
    {
        NumberFormatInfo nfi = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
        nfi.NumberGroupSeparator = "\u2009";
        nfi.NumberDecimalDigits = 0;

        return nfi;
    }
}
