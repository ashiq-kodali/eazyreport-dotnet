using System.Globalization;
using System.Net;

namespace EazyReport.Expressions;

public static class Formatters
{
    private static readonly Dictionary<string, (string Symbol, string Culture)> CurrencyMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["USD"] = ("$", "en-US"),
        ["EUR"] = ("€", "de-DE"),
        ["GBP"] = ("£", "en-GB"),
        ["INR"] = ("₹", "en-IN"),
        ["JPY"] = ("¥", "ja-JP"),
        ["CNY"] = ("¥", "zh-CN"),
        ["CAD"] = ("CA$", "en-CA"),
        ["AUD"] = ("A$", "en-AU"),
        ["AED"] = ("AED ", "ar-AE"),
        ["SAR"] = ("SAR ", "ar-SA"),
    };

    public static string EscapeHtml(string? text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        return WebUtility.HtmlEncode(text);
    }

    public static double ToDouble(object? val)
    {
        if (val == null) return 0.0;
        if (val is double d) return d;
        if (val is float f) return f;
        if (val is int i) return i;
        if (val is long l) return l;
        if (val is decimal m) return (double)m;
        if (val is short s) return s;
        if (val is byte b) return b;
        if (val is System.Text.Json.JsonElement je)
        {
            if (je.ValueKind == System.Text.Json.JsonValueKind.Number) return je.GetDouble();
            if (je.ValueKind == System.Text.Json.JsonValueKind.String)
                return double.TryParse(je.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var pd) ? pd : 0.0;
        }

        string str = val.ToString()?.Replace(",", "").Trim() ?? "";
        return double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0.0;
    }

    public static string FormatCurrency(object? val, string currency = "USD", int decimals = 2)
    {
        double d = ToDouble(val);
        currency = string.IsNullOrWhiteSpace(currency) ? "USD" : currency.Trim().ToUpperInvariant();

        if (CurrencyMap.TryGetValue(currency, out var info))
        {
            var culture = new CultureInfo(info.Culture);
            string formattedNum = d.ToString($"N{decimals}", culture);
            return $"{info.Symbol}{formattedNum}";
        }

        return $"{currency} {d.ToString($"N{decimals}", CultureInfo.InvariantCulture)}";
    }

    public static string FormatNumber(object? val, int decimals = 2, bool thousands = true)
    {
        double d = ToDouble(val);
        string format = thousands ? $"N{decimals}" : $"F{decimals}";
        return d.ToString(format, CultureInfo.InvariantCulture);
    }

    public static DateTime? ToDate(object? val)
    {
        if (val == null) return null;
        if (val is DateTime dt) return dt;
        if (val is DateTimeOffset dto) return dto.DateTime;
        if (val is System.Text.Json.JsonElement je && je.ValueKind == System.Text.Json.JsonValueKind.String)
        {
            val = je.GetString();
        }

        string s = val?.ToString()?.Trim() ?? "";
        if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            return parsed;
        if (DateTime.TryParse(s, out parsed))
            return parsed;

        return null;
    }

    public static string FormatDatePattern(DateTime dt, string pattern = "dd/MM/yyyy")
    {
        if (string.IsNullOrWhiteSpace(pattern)) pattern = "dd/MM/yyyy";
        return dt.ToString(pattern, CultureInfo.InvariantCulture);
    }
}
