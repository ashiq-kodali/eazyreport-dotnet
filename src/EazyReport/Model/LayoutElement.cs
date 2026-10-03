namespace EazyReport.Model;

public class LayoutElement
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 8);
    public string Type { get; set; } = "text"; // text, field, shape, line, image, barcode, table, chart, checkbox, subreport
    public string Name { get; set; } = "";
    public double X { get; set; } = 0.0;
    public double Y { get; set; } = 0.0;
    public double W { get; set; } = 20.0;
    public double H { get; set; } = 10.0;
    public double Rotate { get; set; } = 0.0;
    public double Opacity { get; set; } = 1.0;

    // Content & Binding
    public string Content { get; set; } = "";
    public string Field { get; set; } = "";
    public string Binding { get; set; } = "";
    public bool AutoHeight { get; set; } = false;
    public string VisibleExpr { get; set; } = "";

    // Styles & Rules
    public TextStyle Style { get; set; } = new();
    public BoxStyle Box { get; set; } = new();
    public ValueFormat Format { get; set; } = new();
    public List<LogicRule> Rules { get; set; } = new();

    // Barcode specific
    public string Symbology { get; set; } = "qrcode";
    public string BarcodeValue { get; set; } = "";
    public bool ShowText { get; set; } = false;
    public string BarColor { get; set; } = "#000000";
    public string BarcodeBackground { get; set; } = "#ffffff";

    // Chart specific
    public string ChartType { get; set; } = "column"; // column, bar, line, area, pie, doughnut
    public string Dataset { get; set; } = "";
    public string LabelField { get; set; } = "";
    public string ValueField { get; set; } = "";
    public string Title { get; set; } = "";
    public bool ShowLegend { get; set; } = true;
    public bool ShowValues { get; set; } = true;
    public bool ShowGrid { get; set; } = true;
    public List<string> Colors { get; set; } = new() { "#3b82f6", "#10b981", "#f59e0b", "#ef4444", "#8b5cf6" };

    // Shape / Line specific
    public string ShapeType { get; set; } = "rectangle"; // rectangle, rounded_rect, circle, ellipse, line, triangle
    public string Fill { get; set; } = "";
    public string Stroke { get; set; } = "#000000";
    public double StrokeWidth { get; set; } = 1.0;
    public string StrokeDash { get; set; } = ""; // solid, dashed, dotted

    // Image specific
    public string Src { get; set; } = "";
    public string Fit { get; set; } = "contain"; // contain, cover, fill

    // Checkbox specific
    public string CheckedExpr { get; set; } = "";
    public double BoxSize { get; set; } = 4.0;
    public string CheckType { get; set; } = "check"; // check, cross, fill
}
