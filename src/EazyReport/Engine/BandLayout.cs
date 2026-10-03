using System.Globalization;
using System.Text;
using EazyReport.Expressions;
using EazyReport.Model;

namespace EazyReport.Engine;

public class PlacedElementRecord
{
    public LayoutElement Element { get; set; } = new();
    public double X { get; set; }
    public double Y { get; set; }
    public double W { get; set; }
    public double H { get; set; }
    public string Text { get; set; } = "";
    public RenderCtx Context { get; set; } = new();
}

public class PlacedBandRecord
{
    public Band Band { get; set; } = new();
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public string Fill { get; set; } = "";
    public List<PlacedElementRecord> Elements { get; set; } = new();
}

public class LaidOutBand
{
    public string Html { get; set; } = "";
    public double Height { get; set; }
    public List<PlacedElementRecord> Elements { get; set; } = new();
}

public static class BandLayout
{
    public static string FieldText(LayoutElement el, RenderCtx rc)
    {
        string raw = !string.IsNullOrEmpty(el.Field) ? el.Field : (!string.IsNullOrEmpty(el.Binding) ? el.Binding : el.Content);
        if (string.IsNullOrEmpty(raw)) return "";

        // Check if raw contains Handlebars tags
        if (raw.Contains("{{"))
        {
            var ctxObj = BuildContextObject(rc);
            string evaluated = ExpressionEngine.EvaluateTemplate(raw, ctxObj);
            return evaluated;
        }

        // Direct path or expression evaluation
        object? val = rc.ResolveValue(raw);
        if (val == null && raw.Contains(" "))
        {
            val = ExpressionEngine.EvaluateExpression(raw, BuildContextObject(rc));
        }

        if (val == null) return raw;

        // Apply formatting
        var fmt = el.Format;
        if (fmt != null && !string.IsNullOrEmpty(fmt.Type) && fmt.Type != "text")
        {
            string fmtType = fmt.Type.ToLowerInvariant();
            if (fmtType == "currency")
                return Formatters.FormatCurrency(val, fmt.Currency, fmt.Decimals);
            if (fmtType == "number")
                return Formatters.FormatNumber(val, fmt.Decimals, fmt.Thousands);
            if (fmtType == "date")
            {
                var dt = Formatters.ToDate(val);
                return dt.HasValue ? Formatters.FormatDatePattern(dt.Value, fmt.Pattern) : val.ToString() ?? "";
            }
            if (fmtType == "percent")
            {
                double d = Formatters.ToDouble(val);
                return $"{d.ToString($"F{fmt.Decimals}", CultureInfo.InvariantCulture)}%";
            }
        }

        return val.ToString() ?? "";
    }

    public static double MeasureElementHeight(LayoutElement el, string text)
    {
        if (!el.AutoHeight) return el.H;
        if (string.IsNullOrEmpty(text)) return el.H;

        double pt = 0.3528; // pt in mm
        double lineH = el.Style.FontSize * el.Style.LineHeight * pt;
        double charW = el.Style.FontSize * 0.52 * pt;
        double padding = el.Box.GetPaddingTop() + el.Box.GetPaddingBottom();

        double availW = Math.Max(1.0, el.W - el.Box.GetPaddingLeft() - el.Box.GetPaddingRight());
        int charsPerLine = Math.Max(1, (int)(availW / charW));

        int lines = 0;
        foreach (var line in text.Split('\n'))
        {
            lines += Math.Max(1, (int)Math.Ceiling((double)line.Length / charsPerLine));
        }

        double neededH = lines * lineH + padding;
        return Math.Max(el.H, neededH);
    }

    public static LaidOutBand? Layout(Band band, RenderCtx rc, double width, string fill = "", double? fixedHeight = null)
    {
        if (!string.IsNullOrWhiteSpace(band.VisibleExpr))
        {
            var ctxObj = BuildContextObject(rc);
            if (!ExpressionEngine.EvaluateCondition(band.VisibleExpr, ctxObj))
                return null;
        }

        var placedElements = new List<PlacedElementRecord>();
        double maxBottom = fixedHeight ?? band.Height;
        var elemHeights = new Dictionary<string, double>();

        foreach (var el in band.Elements)
        {
            string txt = FieldText(el, rc);
            double eh = MeasureElementHeight(el, txt);
            elemHeights[el.Id] = eh;

            if (band.CanGrow && (el.Y + eh > maxBottom))
            {
                maxBottom = el.Y + eh;
            }
        }

        var sb = new StringBuilder();
        string bandFill = !string.IsNullOrEmpty(fill) ? fill : band.Fill;
        string bgStyle = !string.IsNullOrEmpty(bandFill) ? $"background-color:{bandFill};" : "";

        sb.Append($"<div class=\"report-band\" style=\"position:relative;width:{width:F2}mm;height:{maxBottom:F2}mm;{bgStyle}box-sizing:border-box;\">");

        foreach (var el in band.Elements)
        {
            var elementClone = CloneElement(el);
            LogicEvaluator.ApplyRules(elementClone, rc);

            if (elementClone.Opacity <= 0.0) continue;

            string txt = FieldText(elementClone, rc);
            double eh = elemHeights.GetValueOrDefault(el.Id, elementClone.H);

            placedElements.Add(new PlacedElementRecord
            {
                Element = elementClone,
                X = elementClone.X,
                Y = elementClone.Y,
                W = elementClone.W,
                H = eh,
                Text = txt,
                Context = rc.Clone()
            });

            string elHtml = RenderElementHtml(elementClone, txt, eh, rc);
            sb.Append($"<div style=\"position:absolute;left:{elementClone.X:F2}mm;top:{elementClone.Y:F2}mm;width:{elementClone.W:F2}mm;height:{eh:F2}mm;box-sizing:border-box;\">{elHtml}</div>");
        }

        sb.Append("</div>");

        return new LaidOutBand
        {
            Html = sb.ToString(),
            Height = maxBottom,
            Elements = placedElements
        };
    }

    public static string RenderElementHtml(LayoutElement el, string text, double actualH, RenderCtx rc)
    {
        string type = (el.Type ?? "text").ToLowerInvariant();

        return type switch
        {
            "barcode" => BarcodeGenerator.GenerateSvg(el.Symbology, !string.IsNullOrEmpty(el.BarcodeValue) ? el.BarcodeValue : text, el.BarColor, el.BarcodeBackground, el.ShowText),
            "chart" => RenderChartHtml(el, rc),
            "shape" or "line" => RenderShapeHtml(el),
            "image" => RenderImageHtml(el),
            "checkbox" => RenderCheckboxHtml(el, rc),
            _ => RenderTextHtml(el, text, actualH)
        };
    }

    private static string RenderTextHtml(LayoutElement el, string text, double actualH)
    {
        var s = el.Style;
        var b = el.Box;

        var css = new StringBuilder();
        css.Append($"font-family:'{s.FontFamily}', sans-serif;");
        css.Append($"font-size:{s.FontSize:F1}pt;");
        css.Append($"color:{s.Color};");
        css.Append($"text-align:{s.Align};");
        css.Append($"line-height:{s.LineHeight:F1};");
        if (s.Bold) css.Append("font-weight:700;");
        if (s.Italic) css.Append("font-style:italic;");
        if (s.Underline) css.Append("text-decoration:underline;");

        if (!string.IsNullOrEmpty(b.Background)) css.Append($"background-color:{b.Background};");
        if (b.BorderWidth > 0 && !string.IsNullOrEmpty(b.BorderColor))
        {
            css.Append($"border:{b.BorderWidth:F1}mm {b.BorderStyle} {b.BorderColor};");
        }
        if (b.BorderRadius > 0) css.Append($"border-radius:{b.BorderRadius:F1}mm;");

        css.Append($"padding:{b.GetPaddingTop():F1}mm {b.GetPaddingRight():F1}mm {b.GetPaddingBottom():F1}mm {b.GetPaddingLeft():F1}mm;");
        css.Append("box-sizing:border-box;display:flex;width:100%;height:100%;overflow:hidden;word-break:break-word;");

        string justify = s.Align switch
        {
            "center" => "center",
            "right" => "flex-end",
            _ => "flex-start"
        };
        string align = s.VerticalAlign switch
        {
            "middle" => "center",
            "bottom" => "flex-end",
            _ => "flex-start"
        };
        css.Append($"justify-content:{justify};align-items:{align};");

        return $"<div style=\"{css}\">{Formatters.EscapeHtml(text).Replace("\n", "<br/>")}</div>";
    }

    private static string RenderShapeHtml(LayoutElement el)
    {
        string fill = !string.IsNullOrEmpty(el.Fill) ? el.Fill : (!string.IsNullOrEmpty(el.Box.Background) ? el.Box.Background : "transparent");
        string stroke = !string.IsNullOrEmpty(el.Stroke) ? el.Stroke : (!string.IsNullOrEmpty(el.Box.BorderColor) ? el.Box.BorderColor : "#000000");
        double strokeW = el.StrokeWidth > 0 ? el.StrokeWidth : (el.Box.BorderWidth > 0 ? el.Box.BorderWidth : 1.0);

        string shape = (el.ShapeType ?? "rectangle").ToLowerInvariant();
        var sb = new StringBuilder();
        sb.Append($"<svg viewBox=\"0 0 {el.W:F2} {el.H:F2}\" style=\"width:100%;height:100%;display:block;\" xmlns=\"http://www.w3.org/2000/svg\">");

        if (shape is "rectangle" or "rounded_rect")
        {
            double rx = shape == "rounded_rect" ? (el.Box.BorderRadius > 0 ? el.Box.BorderRadius : 3.0) : 0.0;
            sb.Append($"<rect x=\"{strokeW / 2:F2}\" y=\"{strokeW / 2:F2}\" width=\"{el.W - strokeW:F2}\" height=\"{el.H - strokeW:F2}\" rx=\"{rx:F2}\" fill=\"{fill}\" stroke=\"{stroke}\" stroke-width=\"{strokeW:F2}\"/>");
        }
        else if (shape is "circle" or "ellipse")
        {
            sb.Append($"<ellipse cx=\"{el.W / 2:F2}\" cy=\"{el.H / 2:F2}\" rx=\"{(el.W - strokeW) / 2:F2}\" ry=\"{(el.H - strokeW) / 2:F2}\" fill=\"{fill}\" stroke=\"{stroke}\" stroke-width=\"{strokeW:F2}\"/>");
        }
        else if (shape == "line")
        {
            sb.Append($"<line x1=\"0\" y1=\"{el.H / 2:F2}\" x2=\"{el.W:F2}\" y2=\"{el.H / 2:F2}\" stroke=\"{stroke}\" stroke-width=\"{strokeW:F2}\"/>");
        }

        sb.Append("</svg>");
        return sb.ToString();
    }

    private static string RenderImageHtml(LayoutElement el)
    {
        if (string.IsNullOrEmpty(el.Src)) return "";
        string fit = el.Fit switch
        {
            "cover" => "object-fit:cover;",
            "fill" => "object-fit:fill;",
            _ => "object-fit:contain;"
        };
        return $"<img src=\"{el.Src}\" style=\"width:100%;height:100%;{fit}display:block;\" />";
    }

    private static string RenderCheckboxHtml(LayoutElement el, RenderCtx rc)
    {
        bool isChecked = false;
        if (!string.IsNullOrWhiteSpace(el.CheckedExpr))
        {
            var ctxObj = BuildContextObject(rc);
            isChecked = ExpressionEngine.EvaluateCondition(el.CheckedExpr, ctxObj);
        }

        string checkChar = isChecked ? "✓" : "";
        string border = el.Box.BorderWidth > 0 ? $"{el.Box.BorderWidth:F1}mm solid {el.Box.BorderColor}" : "1.5px solid #000";
        string bg = !string.IsNullOrEmpty(el.Box.Background) ? el.Box.Background : "#ffffff";

        return $"<div style=\"width:100%;height:100%;display:flex;align-items:center;justify-content:center;border:{border};background-color:{bg};font-weight:bold;font-size:10pt;\">{checkChar}</div>";
    }

    private static string RenderChartHtml(LayoutElement el, RenderCtx rc)
    {
        var labels = new List<string>();
        var values = new List<double>();

        object? listObj = !string.IsNullOrEmpty(el.Dataset) ? rc.ResolveValue(el.Dataset) : rc.Data;
        if (listObj is System.Collections.IEnumerable ie && !(listObj is string))
        {
            foreach (var item in ie)
            {
                object? lVal = !string.IsNullOrEmpty(el.LabelField) ? ExpressionEngine.GetPath(item, el.LabelField) : null;
                object? vVal = !string.IsNullOrEmpty(el.ValueField) ? ExpressionEngine.GetPath(item, el.ValueField) : null;

                labels.Add(lVal?.ToString() ?? "");
                values.Add(Formatters.ToDouble(vVal));
            }
        }

        return ChartGenerator.GenerateSvg(el, labels, values);
    }

    public static Dictionary<string, object?> BuildContextObject(RenderCtx rc)
    {
        var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Page"] = rc.PageNumber,
            ["TotalPages"] = rc.TotalPages,
            ["pageNumber"] = rc.PageNumber,
            ["totalPages"] = rc.TotalPages
        };

        if (rc.Data != null)
        {
            dict["data"] = rc.Data;
            dict["root"] = rc.Data;
            if (rc.Data is System.Collections.IDictionary dataDict)
            {
                foreach (var key in dataDict.Keys)
                {
                    if (key != null) dict[key.ToString()!] = dataDict[key];
                }
            }
            else if (rc.Data is System.Text.Json.JsonElement jeData && jeData.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                foreach (var prop in jeData.EnumerateObject())
                {
                    dict[prop.Name] = prop.Value;
                }
            }
        }

        if (rc.Item != null)
        {
            dict["item"] = rc.Item;
            dict["this"] = rc.Item;
            if (rc.Item is System.Collections.IDictionary itemDict)
            {
                foreach (var key in itemDict.Keys)
                {
                    if (key != null) dict[key.ToString()!] = itemDict[key];
                }
            }
            else if (rc.Item is System.Text.Json.JsonElement jeItem && jeItem.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                foreach (var prop in jeItem.EnumerateObject())
                {
                    dict[prop.Name] = prop.Value;
                }
            }
        }

        foreach (var kvp in rc.Params)
        {
            dict[kvp.Key] = kvp.Value;
        }

        return dict;
    }

    private static LayoutElement CloneElement(LayoutElement src)
    {
        return new LayoutElement
        {
            Id = src.Id,
            Type = src.Type,
            Name = src.Name,
            X = src.X,
            Y = src.Y,
            W = src.W,
            H = src.H,
            Rotate = src.Rotate,
            Opacity = src.Opacity,
            Content = src.Content,
            Field = src.Field,
            Binding = src.Binding,
            AutoHeight = src.AutoHeight,
            VisibleExpr = src.VisibleExpr,
            Style = new TextStyle
            {
                FontFamily = src.Style.FontFamily,
                FontSize = src.Style.FontSize,
                Bold = src.Style.Bold,
                Italic = src.Style.Italic,
                Underline = src.Style.Underline,
                Color = src.Style.Color,
                Align = src.Style.Align,
                VerticalAlign = src.Style.VerticalAlign,
                LineHeight = src.Style.LineHeight
            },
            Box = new BoxStyle
            {
                Background = src.Box.Background,
                BorderWidth = src.Box.BorderWidth,
                BorderColor = src.Box.BorderColor,
                BorderStyle = src.Box.BorderStyle,
                BorderRadius = src.Box.BorderRadius,
                Padding = src.Box.Padding,
                PaddingTop = src.Box.PaddingTop,
                PaddingRight = src.Box.PaddingRight,
                PaddingBottom = src.Box.PaddingBottom,
                PaddingLeft = src.Box.PaddingLeft
            },
            Format = new ValueFormat
            {
                Type = src.Format.Type,
                Pattern = src.Format.Pattern,
                Currency = src.Format.Currency,
                Decimals = src.Format.Decimals,
                Thousands = src.Format.Thousands
            },
            Rules = src.Rules.ToList(),
            Symbology = src.Symbology,
            BarcodeValue = src.BarcodeValue,
            ShowText = src.ShowText,
            BarColor = src.BarColor,
            BarcodeBackground = src.BarcodeBackground,
            ChartType = src.ChartType,
            Dataset = src.Dataset,
            LabelField = src.LabelField,
            ValueField = src.ValueField,
            Title = src.Title,
            ShowLegend = src.ShowLegend,
            ShowValues = src.ShowValues,
            ShowGrid = src.ShowGrid,
            Colors = src.Colors.ToList(),
            ShapeType = src.ShapeType,
            Fill = src.Fill,
            Stroke = src.Stroke,
            StrokeWidth = src.StrokeWidth,
            StrokeDash = src.StrokeDash,
            Src = src.Src,
            Fit = src.Fit,
            CheckedExpr = src.CheckedExpr,
            BoxSize = src.BoxSize,
            CheckType = src.CheckType
        };
    }
}
