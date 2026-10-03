using System.Text.Json;
using EazyReport.Engine;
using EazyReport.Model;
using EazyReport.Renderers;

namespace EazyReport;

/// <summary>
/// Fluent report builder for binding datasets and parameters to an RTPL template.
/// </summary>
public class ReportBuilder
{
    private readonly LayoutDocument _document;
    private object? _data;
    private readonly Dictionary<string, object?> _params = new(StringComparer.OrdinalIgnoreCase);
    private string _title;
    private RenderOptions _options = new();
    private List<RenderedPage>? _cachedPages;

    public ReportBuilder(LayoutDocument document)
    {
        _document = document ?? throw new ArgumentNullException(nameof(document));
        _title = document.Title ?? "Report";
    }

    /// <summary>
    /// Creates a new ReportBuilder by loading an RTPL JSON string.
    /// </summary>
    public static ReportBuilder FromTemplate(string rtplJson)
    {
        var doc = TemplateLoader.FromJson(rtplJson);
        return new ReportBuilder(doc);
    }

    /// <summary>
    /// Creates a new ReportBuilder by loading an RTPL JSON stream.
    /// </summary>
    public static ReportBuilder FromTemplate(Stream stream)
    {
        var doc = TemplateLoader.FromStream(stream);
        return new ReportBuilder(doc);
    }

    /// <summary>
    /// Creates a new ReportBuilder from an already parsed LayoutDocument.
    /// </summary>
    public static ReportBuilder FromTemplate(LayoutDocument document)
    {
        return new ReportBuilder(document);
    }

    /// <summary>
    /// Set the primary dataset, dictionary, or list of records.
    /// </summary>
    public ReportBuilder Data(object? data)
    {
        _data = data;
        _cachedPages = null;
        return this;
    }

    /// <summary>
    /// Set data from a raw JSON string.
    /// </summary>
    public ReportBuilder DataJson(string json)
    {
        if (!string.IsNullOrWhiteSpace(json))
        {
            using var doc = JsonDocument.Parse(json);
            _data = doc.RootElement.Clone();
        }
        _cachedPages = null;
        return this;
    }

    /// <summary>
    /// Add or update a report runtime parameter.
    /// </summary>
    public ReportBuilder Param(string key, object? value)
    {
        _params[key] = value;
        _cachedPages = null;
        return this;
    }

    /// <summary>
    /// Add or update multiple report runtime parameters.
    /// </summary>
    public ReportBuilder Params(IDictionary<string, object?> parameters)
    {
        if (parameters != null)
        {
            foreach (var kvp in parameters)
            {
                _params[kvp.Key] = kvp.Value;
            }
        }
        _cachedPages = null;
        return this;
    }

    /// <summary>
    /// Sets the document/window title.
    /// </summary>
    public ReportBuilder Title(string title)
    {
        _title = title;
        return this;
    }

    /// <summary>
    /// Sets pagination options.
    /// </summary>
    public ReportBuilder Options(RenderOptions options)
    {
        _options = options ?? new RenderOptions();
        _cachedPages = null;
        return this;
    }

    /// <summary>
    /// Executes multi-pass pagination and returns the list of rendered pages.
    /// </summary>
    public List<RenderedPage> Pages()
    {
        if (_cachedPages == null)
        {
            _cachedPages = PaginationEngine.Paginate(_document, _data, _params, _options);
        }
        return _cachedPages;
    }

    /// <summary>
    /// Returns the exact total number of generated pages.
    /// </summary>
    public int PageCount()
    {
        return Pages().Count;
    }

    /// <summary>
    /// Renders a complete standalone printable HTML document with @page print CSS rules.
    /// </summary>
    public string ToHtml()
    {
        return HtmlRenderer.Render(Pages(), _title);
    }

    /// <summary>
    /// Renders high-precision vector PDF bytes.
    /// </summary>
    public byte[] ToPdf()
    {
        return PdfRenderer.Render(Pages(), _title);
    }

    /// <summary>
    /// Saves the rendered HTML document directly to disk.
    /// </summary>
    public void SaveHtml(string filePath)
    {
        File.WriteAllText(filePath, ToHtml(), System.Text.Encoding.UTF8);
    }

    /// <summary>
    /// Saves the rendered vector PDF directly to disk.
    /// </summary>
    public void SavePdf(string filePath)
    {
        File.WriteAllBytes(filePath, ToPdf());
    }
}
