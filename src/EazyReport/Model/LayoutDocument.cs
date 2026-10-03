namespace EazyReport.Model;

public class LayoutPage
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 8);
    public string Name { get; set; } = "Page1";
    public string Size { get; set; } = "A4";
    public string Orientation { get; set; } = "portrait"; // portrait, landscape
    public double Width { get; set; } = 210.0;
    public double Height { get; set; } = 297.0;
    public PageMargins Margins { get; set; } = new();
    public double PrintOffsetX { get; set; } = 0.0;
    public double PrintOffsetY { get; set; } = 0.0;
    public Watermark Watermark { get; set; } = new();

    public Band? ReportTitle { get; set; }
    public Band? ReportSummary { get; set; }
    public Band? PageHeader { get; set; }
    public Band? PageFooter { get; set; }
    public Band? ColumnHeader { get; set; }
    public Band? ColumnFooter { get; set; }
    public Band? Header { get; set; }
    public Band? Footer { get; set; }
    public Band? Overlay { get; set; }

    public List<DataBand> DataBands { get; set; } = new();
}

public class LayoutDocument
{
    public string Format { get; set; } = "report-designer-layout";
    public int Version { get; set; } = 2;
    public string Title { get; set; } = "Report";
    public string Author { get; set; } = "";
    public string Description { get; set; } = "";
    public List<LayoutPage> Pages { get; set; } = new();
    public Dictionary<string, object?> Datasets { get; set; } = new();
    public Dictionary<string, object?> Parameters { get; set; } = new();
    public Dictionary<string, object?> Styles { get; set; } = new();
}
