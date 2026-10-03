using EazyReport.Model;

namespace EazyReport;

/// <summary>
/// Static entry point for quick rendering with EazyReport.
/// </summary>
public static class EazyReportApi
{
    public static string RenderHtml(string rtplJson, object? data, Dictionary<string, object?>? parameters = null, string title = "Report")
    {
        var builder = ReportBuilder.FromTemplate(rtplJson).Data(data).Title(title);
        if (parameters != null) builder.Params(parameters);
        return builder.ToHtml();
    }

    public static byte[] RenderPdf(string rtplJson, object? data, Dictionary<string, object?>? parameters = null, string title = "Report")
    {
        var builder = ReportBuilder.FromTemplate(rtplJson).Data(data).Title(title);
        if (parameters != null) builder.Params(parameters);
        return builder.ToPdf();
    }

    public static int PageCount(string rtplJson, object? data, Dictionary<string, object?>? parameters = null)
    {
        var builder = ReportBuilder.FromTemplate(rtplJson).Data(data);
        if (parameters != null) builder.Params(parameters);
        return builder.PageCount();
    }
}
