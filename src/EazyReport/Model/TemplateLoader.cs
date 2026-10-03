using System.Text.Json;

namespace EazyReport.Model;

public static class TemplateLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static LayoutDocument FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException("Template JSON cannot be empty", nameof(json));

        using var doc = JsonDocument.Parse(json);
        return FromJsonElement(doc.RootElement);
    }

    public static LayoutDocument FromStream(Stream stream)
    {
        using var doc = JsonDocument.Parse(stream);
        return FromJsonElement(doc.RootElement);
    }

    public static LayoutDocument FromJsonElement(JsonElement root)
    {
        var layoutDoc = new LayoutDocument
        {
            Format = GetString(root, "format", "report-designer-layout"),
            Version = GetInt(root, "version", 2),
            Title = GetString(root, "title", "Report"),
            Author = GetString(root, "author", ""),
            Description = GetString(root, "description", "")
        };

        if (root.TryGetProperty("pages", out var pagesProp) && pagesProp.ValueKind == JsonValueKind.Array)
        {
            foreach (var pageEl in pagesProp.EnumerateArray())
            {
                layoutDoc.Pages.Add(ParsePage(pageEl));
            }
        }

        if (layoutDoc.Pages.Count == 0)
        {
            layoutDoc.Pages.Add(new LayoutPage());
        }

        return layoutDoc;
    }

    private static LayoutPage ParsePage(JsonElement el)
    {
        var page = new LayoutPage
        {
            Id = GetString(el, "id", Guid.NewGuid().ToString("N").Substring(0, 8)),
            Name = GetString(el, "name", "Page1"),
            Size = GetString(el, "size", "A4"),
            Orientation = GetString(el, "orientation", "portrait"),
            PrintOffsetX = GetDouble(el, "printOffsetX", 0.0),
            PrintOffsetY = GetDouble(el, "printOffsetY", 0.0),
        };

        bool isPortrait = page.Orientation.Equals("portrait", StringComparison.OrdinalIgnoreCase);
        page.Width = GetDouble(el, "width", isPortrait ? 210.0 : 297.0);
        page.Height = GetDouble(el, "height", isPortrait ? 297.0 : 210.0);

        if (el.TryGetProperty("margins", out var marginsProp) && marginsProp.ValueKind == JsonValueKind.Object)
        {
            page.Margins = new PageMargins
            {
                Top = GetDouble(marginsProp, "top", 10.0),
                Right = GetDouble(marginsProp, "right", 10.0),
                Bottom = GetDouble(marginsProp, "bottom", 10.0),
                Left = GetDouble(marginsProp, "left", 10.0)
            };
        }

        if (el.TryGetProperty("watermark", out var wmProp) && wmProp.ValueKind == JsonValueKind.Object)
        {
            page.Watermark = new Watermark
            {
                Enabled = GetBool(wmProp, "enabled", false),
                Text = GetString(wmProp, "text", "DRAFT"),
                Color = GetString(wmProp, "color", "#94a3b8"),
                FontSize = GetDouble(wmProp, "fontSize", 90.0),
                Angle = GetDouble(wmProp, "angle", -45.0),
                Opacity = GetDouble(wmProp, "opacity", 0.18),
                Image = GetString(wmProp, "image", ""),
                OnTop = GetBool(wmProp, "onTop", false)
            };
        }

        // Standard Bands
        page.ReportTitle = ParseBand(el, "reportTitle");
        page.ReportSummary = ParseBand(el, "reportSummary");
        page.PageHeader = ParseBand(el, "pageHeader");
        page.PageFooter = ParseBand(el, "pageFooter");
        page.ColumnHeader = ParseBand(el, "columnHeader");
        page.ColumnFooter = ParseBand(el, "columnFooter");
        page.Header = ParseBand(el, "header");
        page.Footer = ParseBand(el, "footer");
        page.Overlay = ParseBand(el, "overlay");

        // DataBands (supports "dataBands" or "data")
        if (el.TryGetProperty("dataBands", out var dbProp) || el.TryGetProperty("data", out dbProp))
        {
            if (dbProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in dbProp.EnumerateArray())
                {
                    var db = ParseDataBand(item);
                    if (db != null) page.DataBands.Add(db);
                }
            }
            else if (dbProp.ValueKind == JsonValueKind.Object)
            {
                var db = ParseDataBand(dbProp);
                if (db != null) page.DataBands.Add(db);
            }
        }

        return page;
    }

    private static Band? ParseBand(JsonElement parent, string propName)
    {
        if (!parent.TryGetProperty(propName, out var el) || el.ValueKind != JsonValueKind.Object)
            return null;

        var band = new Band
        {
            Id = GetString(el, "id", Guid.NewGuid().ToString("N").Substring(0, 8)),
            Type = GetString(el, "type", propName),
            Name = GetString(el, "name", propName),
            Height = GetDouble(el, "height", 20.0),
            CanGrow = GetBool(el, "canGrow", false),
            CanShrink = GetBool(el, "canShrink", false),
            VisibleExpr = GetString(el, "visibleExpr", ""),
            Fill = GetString(el, "fill", ""),
            StartNewPage = GetBool(el, "startNewPage", false),
            PrintOnFirstPage = GetBool(el, "printOnFirstPage", true),
            PrintOnLastPage = GetBool(el, "printOnLastPage", true),
            RepeatOnEveryPage = GetBool(el, "repeatOnEveryPage", false),
        };

        ParseElements(el, band.Elements);
        return band;
    }

    private static DataBand? ParseDataBand(JsonElement el)
    {
        if (el.ValueKind != JsonValueKind.Object) return null;

        string dataPath = GetString(el, "dataPath", GetString(el, "dataset", GetString(el, "arrayPath", "")));

        var db = new DataBand
        {
            Id = GetString(el, "id", Guid.NewGuid().ToString("N").Substring(0, 8)),
            Type = GetString(el, "type", "data"),
            Name = GetString(el, "name", "DataBand"),
            Height = GetDouble(el, "height", 20.0),
            CanGrow = GetBool(el, "canGrow", false),
            CanShrink = GetBool(el, "canShrink", false),
            VisibleExpr = GetString(el, "visibleExpr", ""),
            Fill = GetString(el, "fill", ""),
            StartNewPage = GetBool(el, "startNewPage", false),
            PrintOnFirstPage = GetBool(el, "printOnFirstPage", true),
            PrintOnLastPage = GetBool(el, "printOnLastPage", true),
            RepeatOnEveryPage = GetBool(el, "repeatOnEveryPage", false),
            Dataset = dataPath,
            DataPath = dataPath,
            Alias = GetString(el, "alias", "item"),
            FilterExpr = GetString(el, "filterExpr", ""),
            SortField = GetString(el, "sortField", ""),
            SortDir = GetString(el, "sortDir", "asc"),
            KeepWithNext = GetBool(el, "keepWithNext", false),
            KeepTogether = GetBool(el, "keepTogether", false),
            FillUnusedSpace = GetBool(el, "fillUnusedSpace", false),
            ColumnCount = GetInt(el, "columnCount", 1),
            ColumnSpacing = GetDouble(el, "columnSpacing", 0.0),
            ColumnWidth = GetDouble(el, "columnWidth", 0.0),
            Header = ParseBand(el, "header"),
            Footer = ParseBand(el, "footer")
        };

        ParseElements(el, db.Elements);

        if (el.TryGetProperty("groupHeaders", out var ghProp) && ghProp.ValueKind == JsonValueKind.Array)
        {
            foreach (var g in ghProp.EnumerateArray())
            {
                var gb = ParseGroupBand(g);
                if (gb != null) db.GroupHeaders.Add(gb);
            }
        }

        if (el.TryGetProperty("groupFooters", out var gfProp) && gfProp.ValueKind == JsonValueKind.Array)
        {
            foreach (var g in gfProp.EnumerateArray())
            {
                var gb = ParseGroupBand(g);
                if (gb != null) db.GroupFooters.Add(gb);
            }
        }

        if ((el.TryGetProperty("childBands", out var cbProp) || el.TryGetProperty("children", out cbProp)) && cbProp.ValueKind == JsonValueKind.Array)
        {
            foreach (var c in cbProp.EnumerateArray())
            {
                var cb = ParseChildBand(c);
                if (cb != null) db.ChildBands.Add(cb);
            }
        }

        return db;
    }

    private static GroupBand? ParseGroupBand(JsonElement el)
    {
        if (el.ValueKind != JsonValueKind.Object) return null;
        var gb = new GroupBand
        {
            Id = GetString(el, "id", Guid.NewGuid().ToString("N").Substring(0, 8)),
            Type = GetString(el, "type", "groupHeader"),
            Name = GetString(el, "name", "Group"),
            Height = GetDouble(el, "height", 10.0),
            CanGrow = GetBool(el, "canGrow", false),
            CanShrink = GetBool(el, "canShrink", false),
            VisibleExpr = GetString(el, "visibleExpr", ""),
            Fill = GetString(el, "fill", ""),
            StartNewPage = GetBool(el, "startNewPage", false),
            PrintOnFirstPage = GetBool(el, "printOnFirstPage", true),
            PrintOnLastPage = GetBool(el, "printOnLastPage", true),
            RepeatOnEveryPage = GetBool(el, "repeatOnEveryPage", false),
            Expression = GetString(el, "expression", ""),
            ResetPageNumber = GetBool(el, "resetPageNumber", false),
            KeepTogether = GetBool(el, "keepTogether", false),
        };
        ParseElements(el, gb.Elements);
        return gb;
    }

    private static ChildBand? ParseChildBand(JsonElement el)
    {
        if (el.ValueKind != JsonValueKind.Object) return null;
        var cb = new ChildBand
        {
            Id = GetString(el, "id", Guid.NewGuid().ToString("N").Substring(0, 8)),
            Type = GetString(el, "type", "child"),
            Name = GetString(el, "name", "Child"),
            Height = GetDouble(el, "height", 10.0),
            CanGrow = GetBool(el, "canGrow", false),
            CanShrink = GetBool(el, "canShrink", false),
            VisibleExpr = GetString(el, "visibleExpr", ""),
            Fill = GetString(el, "fill", ""),
            StartNewPage = GetBool(el, "startNewPage", false),
            PrintOnFirstPage = GetBool(el, "printOnFirstPage", true),
            PrintOnLastPage = GetBool(el, "printOnLastPage", true),
            RepeatOnEveryPage = GetBool(el, "repeatOnEveryPage", false),
            KeepWithParent = GetBool(el, "keepWithParent", false),
            FillUnusedSpace = GetBool(el, "fillUnusedSpace", false),
        };
        ParseElements(el, cb.Elements);
        return cb;
    }

    private static void ParseElements(JsonElement parent, List<LayoutElement> targetList)
    {
        if (!parent.TryGetProperty("elements", out var elArray) || elArray.ValueKind != JsonValueKind.Array)
            return;

        foreach (var el in elArray.EnumerateArray())
        {
            if (el.ValueKind != JsonValueKind.Object) continue;

            var elem = new LayoutElement
            {
                Id = GetString(el, "id", Guid.NewGuid().ToString("N").Substring(0, 8)),
                Type = GetString(el, "type", "text"),
                Name = GetString(el, "name", ""),
                X = GetDouble(el, "x", 0.0),
                Y = GetDouble(el, "y", 0.0),
                W = GetDouble(el, "w", 20.0),
                H = GetDouble(el, "h", 10.0),
                Rotate = GetDouble(el, "rotate", 0.0),
                Opacity = GetDouble(el, "opacity", 1.0),
                Content = GetString(el, "content", GetString(el, "text", "")),
                Field = GetString(el, "path", GetString(el, "field", GetString(el, "binding", ""))),
                Binding = GetString(el, "binding", ""),
                AutoHeight = GetBool(el, "autoHeight", false),
                VisibleExpr = GetString(el, "visibleExpr", ""),

                // Barcode
                Symbology = GetString(el, "symbology", "qrcode"),
                BarcodeValue = GetString(el, "value", GetString(el, "barcodeValue", GetString(el, "content", ""))),
                ShowText = GetBool(el, "showText", false),
                BarColor = GetString(el, "barColor", "#000000"),
                BarcodeBackground = GetString(el, "barcodeBackground", "#ffffff"),

                // Chart
                ChartType = GetString(el, "chartType", "column"),
                Dataset = GetString(el, "dataset", ""),
                LabelField = GetString(el, "labelField", ""),
                ValueField = GetString(el, "valueField", ""),
                Title = GetString(el, "title", ""),
                ShowLegend = GetBool(el, "showLegend", true),
                ShowValues = GetBool(el, "showValues", true),
                ShowGrid = GetBool(el, "showGrid", true),

                // Shape
                ShapeType = GetString(el, "shapeType", "rectangle"),
                Fill = GetString(el, "fill", ""),
                Stroke = GetString(el, "stroke", "#000000"),
                StrokeWidth = GetDouble(el, "strokeWidth", 1.0),
                StrokeDash = GetString(el, "strokeDash", ""),

                // Image
                Src = GetString(el, "src", ""),
                Fit = GetString(el, "fit", "contain"),

                // Checkbox
                CheckedExpr = GetString(el, "checkedExpr", ""),
                BoxSize = GetDouble(el, "boxSize", 4.0),
                CheckType = GetString(el, "checkType", "check"),
            };

            // Style
            if (el.TryGetProperty("style", out var st) && st.ValueKind == JsonValueKind.Object)
            {
                elem.Style = new TextStyle
                {
                    FontFamily = GetString(st, "fontFamily", "Inter"),
                    FontSize = GetDouble(st, "fontSize", 10.0),
                    Bold = GetBool(st, "bold", false),
                    Italic = GetBool(st, "italic", false),
                    Underline = GetBool(st, "underline", false),
                    Color = GetString(st, "color", "#000000"),
                    Align = GetString(st, "align", "left"),
                    VerticalAlign = GetString(st, "verticalAlign", "top"),
                    LineHeight = GetDouble(st, "lineHeight", 1.2)
                };
            }

            // Box
            if (el.TryGetProperty("box", out var bx) && bx.ValueKind == JsonValueKind.Object)
            {
                elem.Box = new BoxStyle
                {
                    Background = GetString(bx, "background", ""),
                    BorderWidth = GetDouble(bx, "borderWidth", 0.0),
                    BorderColor = GetString(bx, "borderColor", "#000000"),
                    BorderStyle = GetString(bx, "borderStyle", "solid"),
                    BorderRadius = GetDouble(bx, "borderRadius", 0.0),
                    Padding = GetDouble(bx, "padding", 0.0)
                };
                if (bx.TryGetProperty("paddingTop", out var pt)) elem.Box.PaddingTop = pt.GetDouble();
                if (bx.TryGetProperty("paddingRight", out var pr)) elem.Box.PaddingRight = pr.GetDouble();
                if (bx.TryGetProperty("paddingBottom", out var pb)) elem.Box.PaddingBottom = pb.GetDouble();
                if (bx.TryGetProperty("paddingLeft", out var pl)) elem.Box.PaddingLeft = pl.GetDouble();
            }

            // Format
            if ((el.TryGetProperty("valueFormat", out var fmt) || el.TryGetProperty("format", out fmt)) && fmt.ValueKind == JsonValueKind.Object)
            {
                elem.Format = new ValueFormat
                {
                    Type = GetString(fmt, "format", GetString(fmt, "type", "text")),
                    Pattern = GetString(fmt, "datePattern", GetString(fmt, "pattern", "")),
                    Currency = GetString(fmt, "currency", "USD"),
                    Decimals = GetInt(fmt, "decimals", 2),
                    Thousands = GetBool(fmt, "thousands", true)
                };
            }

            // Rules
            if (el.TryGetProperty("rules", out var rls) && rls.ValueKind == JsonValueKind.Array)
            {
                foreach (var r in rls.EnumerateArray())
                {
                    if (r.ValueKind != JsonValueKind.Object) continue;
                    elem.Rules.Add(new LogicRule
                    {
                        Condition = GetString(r, "condition", ""),
                        Property = GetString(r, "property", ""),
                        Value = GetObject(r, "value"),
                        Enabled = GetBool(r, "enabled", true)
                    });
                }
            }

            targetList.Add(elem);
        }
    }

    private static string GetString(JsonElement el, string name, string fallback)
    {
        if (el.TryGetProperty(name, out var val) && val.ValueKind == JsonValueKind.String)
            return val.GetString() ?? fallback;
        return fallback;
    }

    private static double GetDouble(JsonElement el, string name, double fallback)
    {
        if (el.TryGetProperty(name, out var val))
        {
            if (val.ValueKind == JsonValueKind.Number && val.TryGetDouble(out var d))
                return d;
            if (val.ValueKind == JsonValueKind.String && double.TryParse(val.GetString(), out var parsed))
                return parsed;
        }
        return fallback;
    }

    private static int GetInt(JsonElement el, string name, int fallback)
    {
        if (el.TryGetProperty(name, out var val))
        {
            if (val.ValueKind == JsonValueKind.Number && val.TryGetInt32(out var i))
                return i;
            if (val.ValueKind == JsonValueKind.String && int.TryParse(val.GetString(), out var parsed))
                return parsed;
        }
        return fallback;
    }

    private static bool GetBool(JsonElement el, string name, bool fallback)
    {
        if (el.TryGetProperty(name, out var val))
        {
            if (val.ValueKind == JsonValueKind.True) return true;
            if (val.ValueKind == JsonValueKind.False) return false;
            if (val.ValueKind == JsonValueKind.String)
            {
                var s = val.GetString();
                if (bool.TryParse(s, out var parsed)) return parsed;
                if (string.Equals(s, "1", StringComparison.OrdinalIgnoreCase)) return true;
                if (string.Equals(s, "0", StringComparison.OrdinalIgnoreCase)) return false;
            }
        }
        return fallback;
    }

    private static object? GetObject(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var val)) return null;
        return val.ValueKind switch
        {
            JsonValueKind.String => val.GetString(),
            JsonValueKind.Number => val.TryGetInt64(out var l) ? l : val.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => val.ToString()
        };
    }
}
