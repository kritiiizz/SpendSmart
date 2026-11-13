using System.Globalization;

namespace SpendSmart.Web;

public static class Formatting
{
    /// <summary>
    /// Formats an amount as "$1,234.50". Done by hand rather than with the "C" format so output doesn't
    /// depend on the server's culture (Linux containers often run with invariant globalization).
    /// </summary>
    public static string ToMoney(this decimal value) =>
        (value < 0 ? "-$" : "$") + Math.Abs(value).ToString("N2", CultureInfo.InvariantCulture);
}
