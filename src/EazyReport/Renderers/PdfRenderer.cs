using System.Globalization;
using EazyReport.Engine;
using EazyReport.Expressions;
using EazyReport.Model;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace EazyReport.Renderers;

public static class PdfRenderer
{
    private const double MmToPt = 72.0 / 25.4; // 1 mm ~ 2.834645669 pt

    public static byte[] Render(List<RenderedPage> pages, string title = "Report")
    {
        using var doc = new PdfDocument();
        doc.Info.Title = title;
        doc.Info.Author = "EazyReport .NET";

        if (pages.Count == 0)
        {
            var emptyPage = doc.AddPage();
            emptyPage.Width = XUnit.FromMillimeter(210);
            emptyPage.Height = XUnit.FromMillimeter(297);
            using var emptyGfx = XGraphics.FromPdfPage(emptyPage);
            using var msEmpty = new MemoryStream();
            doc.Save(msEmpty, false);
            return msEmpty.ToArray();
        }

        foreach (var page in pages)
        {
            var pdfPage = doc.AddPage();
            pdfPage.Width = XUnit.FromMillimeter(page.Width);
            pdfPage.Height = XUnit.FromMillimeter(page.Height);

            using var gfx = XGraphics.FromPdfPage(pdfPage);

            // 1. Watermark (under content if not onTop)
            if (page.Watermark.Enabled && !string.IsNullOrEmpty(page.Watermark.Text) && !page.Watermark.OnTop)
            {
                DrawWatermark(gfx, page.Watermark, page.Width, page.Height);
            }

            // 2. Placed Bands & Elements
            foreach (var pb in page.PlacedBands)
            {
                // Draw band fill background if present
                var bandFillColor = ParseColor(pb.Fill);
                if (bandFillColor.HasValue)
                {
                    double bx = pb.X * MmToPt;
                    double by = pb.Y * MmToPt;
                    double bw = pb.Width * MmToPt;
                    double bh = pb.Height * MmToPt;
                    gfx.DrawRectangle(new XSolidBrush(bandFillColor.Value), bx, by, bw, bh);
                }

                // Draw Elements inside Band
                foreach (var pe in pb.Elements)
                {
                    double elX = (pb.X + pe.X) * MmToPt;
                    double elY = (pb.Y + pe.Y) * MmToPt;
                    double elW = pe.W * MmToPt;
                    double elH = pe.H * MmToPt;

                    DrawElement(gfx, pe.Element, pe.Text, pe.Context, elX, elY, elW, elH);
                }
            }

            // 3. Watermark (if onTop)
            if (page.Watermark.Enabled && !string.IsNullOrEmpty(page.Watermark.Text) && page.Watermark.OnTop)
            {
                DrawWatermark(gfx, page.Watermark, page.Width, page.Height);
            }
        }

        using var ms = new MemoryStream();
        doc.Save(ms, false);
        return ms.ToArray();
    }

    private static void DrawElement(XGraphics gfx, LayoutElement el, string text, RenderCtx rc, double x, double y, double w, double h)
    {
        string type = (el.Type ?? "text").ToLowerInvariant();

        switch (type)
        {
            case "barcode":
                DrawBarcode(gfx, el, text, x, y, w, h);
                break;
            case "shape":
            case "line":
                DrawShape(gfx, el, x, y, w, h);
                break;
            case "image":
                DrawImage(gfx, el, x, y, w, h);
                break;
            case "checkbox":
                DrawCheckbox(gfx, el, rc, x, y, w, h);
                break;
            default:
                DrawText(gfx, el, text, x, y, w, h);
                break;
        }
    }

    private static void DrawText(XGraphics gfx, LayoutElement el, string text, double x, double y, double w, double h)
    {
        // 1. Box background & borders
        var bgColor = ParseColor(el.Box.Background);
        var borderColor = ParseColor(el.Box.BorderColor);
        double borderWidthPt = el.Box.BorderWidth * MmToPt;

        XBrush? bgBrush = bgColor.HasValue ? new XSolidBrush(bgColor.Value) : null;
        XPen? borderPen = (borderWidthPt > 0 && borderColor.HasValue) ? new XPen(borderColor.Value, borderWidthPt) : null;

        if (bgBrush != null || borderPen != null)
        {
            if (el.Box.BorderRadius > 0)
            {
                double rPt = el.Box.BorderRadius * MmToPt;
                gfx.DrawRoundedRectangle(borderPen, bgBrush, x, y, w, h, rPt, rPt);
            }
            else
            {
                gfx.DrawRectangle(borderPen, bgBrush, x, y, w, h);
            }
        }

        if (string.IsNullOrEmpty(text)) return;

        // 2. Text layout & drawing
        var font = ResolveFont(el.Style.FontFamily, el.Style.FontSize, el.Style.Bold, el.Style.Italic);
        var textColor = ParseColor(el.Style.Color) ?? XColors.Black;
        var textBrush = new XSolidBrush(textColor);

        double padLeft = el.Box.GetPaddingLeft() * MmToPt;
        double padRight = el.Box.GetPaddingRight() * MmToPt;
        double padTop = el.Box.GetPaddingTop() * MmToPt;
        double padBottom = el.Box.GetPaddingBottom() * MmToPt;

        var textRect = new XRect(x + padLeft, y + padTop, Math.Max(1, w - padLeft - padRight), Math.Max(1, h - padTop - padBottom));

        var format = new XStringFormat
        {
            Alignment = el.Style.Align switch
            {
                "center" => XStringAlignment.Center,
                "right" => XStringAlignment.Far,
                _ => XStringAlignment.Near
            },
            LineAlignment = el.Style.VerticalAlign switch
            {
                "middle" => XLineAlignment.Center,
                "bottom" => XLineAlignment.Far,
                _ => XLineAlignment.Near
            }
        };

        gfx.DrawString(text, font, textBrush, textRect, format);
    }

    private static void DrawBarcode(XGraphics gfx, LayoutElement el, string text, double x, double y, double w, double h)
    {
        string val = !string.IsNullOrEmpty(el.BarcodeValue) ? el.BarcodeValue : text;
        if (string.IsNullOrEmpty(val)) val = "12345678";

        var barColor = ParseColor(el.BarColor) ?? XColors.Black;
        var bgColor = ParseColor(el.BarcodeBackground);

        if (bgColor.HasValue)
        {
            gfx.DrawRectangle(new XSolidBrush(bgColor.Value), x, y, w, h);
        }

        var barBrush = new XSolidBrush(barColor);

        if (BarcodeGenerator.IsTwoDimensional(el.Symbology))
        {
            var matrix = BarcodeGenerator.GetQrMatrix(val);
            if (matrix != null)
            {
                int rows = matrix.GetLength(0);
                int cols = matrix.GetLength(1);
                double cellW = w / cols;
                double cellH = h / rows;

                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        if (matrix[r, c])
                        {
                            gfx.DrawRectangle(barBrush, x + c * cellW, y + r * cellH, cellW, cellH);
                        }
                    }
                }
            }
        }
        else
        {
            // 1D Barcode (Code 128)
            string pattern = BarcodeGenerator.EncodeCode128B(val);
            int totalModules = 0;
            foreach (char ch in pattern) totalModules += (ch - '0');
            if (totalModules <= 0) totalModules = 1;

            double modW = w / totalModules;
            double barH = el.ShowText ? h * 0.78 : h;
            double currX = x;
            bool isBar = true;

            foreach (char digit in pattern)
            {
                double blockW = (digit - '0') * modW;
                if (isBar)
                {
                    gfx.DrawRectangle(barBrush, currX, y, blockW, barH);
                }
                currX += blockW;
                isBar = !isBar;
            }

            if (el.ShowText)
            {
                var font = new XFont("Courier", 7, XFontStyle.Regular);
                var textRect = new XRect(x, y + barH, w, h - barH);
                var format = new XStringFormat { Alignment = XStringAlignment.Center, LineAlignment = XLineAlignment.Center };
                gfx.DrawString(val, font, barBrush, textRect, format);
            }
        }
    }

    private static void DrawShape(XGraphics gfx, LayoutElement el, double x, double y, double w, double h)
    {
        string fillStr = !string.IsNullOrEmpty(el.Fill) ? el.Fill : el.Box.Background;
        string strokeStr = !string.IsNullOrEmpty(el.Stroke) ? el.Stroke : el.Box.BorderColor;
        double strokeWPt = (el.StrokeWidth > 0 ? el.StrokeWidth : el.Box.BorderWidth) * MmToPt;

        var fillColor = ParseColor(fillStr);
        var strokeColor = ParseColor(strokeStr);

        XBrush? fillBrush = fillColor.HasValue ? new XSolidBrush(fillColor.Value) : null;
        XPen? strokePen = (strokeWPt > 0 && strokeColor.HasValue) ? new XPen(strokeColor.Value, strokeWPt) : null;

        string shape = (el.ShapeType ?? "rectangle").ToLowerInvariant();

        if (shape is "rectangle" or "rounded_rect")
        {
            if (shape == "rounded_rect" || el.Box.BorderRadius > 0)
            {
                double rPt = (el.Box.BorderRadius > 0 ? el.Box.BorderRadius : 3.0) * MmToPt;
                gfx.DrawRoundedRectangle(strokePen, fillBrush, x, y, w, h, rPt, rPt);
            }
            else
            {
                gfx.DrawRectangle(strokePen, fillBrush, x, y, w, h);
            }
        }
        else if (shape is "circle" or "ellipse")
        {
            gfx.DrawEllipse(strokePen, fillBrush, x, y, w, h);
        }
        else if (shape == "line")
        {
            var linePen = strokePen ?? new XPen(XColors.Black, 1.0);
            gfx.DrawLine(linePen, x, y + h / 2.0, x + w, y + h / 2.0);
        }
    }

    private static void DrawImage(XGraphics gfx, LayoutElement el, double x, double y, double w, double h)
    {
        if (string.IsNullOrEmpty(el.Src)) return;

        try
        {
            XImage? img = null;
            if (el.Src.StartsWith("data:image", StringComparison.OrdinalIgnoreCase))
            {
                int b64Idx = el.Src.IndexOf("base64,", StringComparison.OrdinalIgnoreCase);
                if (b64Idx != -1)
                {
                    string b64 = el.Src.Substring(b64Idx + 7);
                    byte[] bytes = Convert.FromBase64String(b64);
                    img = XImage.FromStream(() => new MemoryStream(bytes));
                }
            }
            else if (File.Exists(el.Src))
            {
                img = XImage.FromFile(el.Src);
            }

            if (img != null)
            {
                gfx.DrawImage(img, x, y, w, h);
            }
        }
        catch
        {
            // Silently skip unrenderable image
        }
    }

    private static void DrawCheckbox(XGraphics gfx, LayoutElement el, RenderCtx rc, double x, double y, double w, double h)
    {
        bool isChecked = false;
        if (!string.IsNullOrWhiteSpace(el.CheckedExpr))
        {
            var ctxObj = new Dictionary<string, object?> { ["item"] = rc.Item, ["data"] = rc.Data };
            isChecked = ExpressionEngine.EvaluateCondition(el.CheckedExpr, ctxObj);
        }

        var borderColor = ParseColor(el.Box.BorderColor) ?? XColors.Black;
        var bgColor = ParseColor(el.Box.Background) ?? XColors.White;
        double bwPt = (el.Box.BorderWidth > 0 ? el.Box.BorderWidth : 0.5) * MmToPt;

        gfx.DrawRectangle(new XPen(borderColor, bwPt), new XSolidBrush(bgColor), x, y, w, h);

        if (isChecked)
        {
            var checkFont = new XFont("Helvetica", Math.Min(w, h) * 0.75, XFontStyle.Bold);
            var format = new XStringFormat { Alignment = XStringAlignment.Center, LineAlignment = XLineAlignment.Center };
            gfx.DrawString("✓", checkFont, XBrushes.Black, new XRect(x, y, w, h), format);
        }
    }

    private static void DrawWatermark(XGraphics gfx, Watermark wm, double pwMm, double phMm)
    {
        double cx = (pwMm * MmToPt) / 2.0;
        double cy = (phMm * MmToPt) / 2.0;

        gfx.Save();
        gfx.TranslateTransform(cx, cy);
        gfx.RotateTransform(wm.Angle);

        var baseColor = ParseColor(wm.Color) ?? XColors.LightGray;
        byte alpha = (byte)Math.Max(10, Math.Min(255, (int)(wm.Opacity * 255)));
        var wmColor = XColor.FromArgb(alpha, baseColor.R, baseColor.G, baseColor.B);

        var font = new XFont("Helvetica", wm.FontSize, XFontStyle.Bold);
        var format = new XStringFormat { Alignment = XStringAlignment.Center, LineAlignment = XLineAlignment.Center };

        gfx.DrawString(wm.Text, font, new XSolidBrush(wmColor), new XPoint(0, 0), format);

        gfx.Restore();
    }

    private static XFont ResolveFont(string? family, double ptSize, bool bold, bool italic)
    {
        string fam = (family ?? "").ToLowerInvariant();
        string fontName = "Helvetica";

        if (fam.Contains("times") || fam.Contains("serif") || fam.Contains("merriweather") || fam.Contains("playfair"))
        {
            fontName = "Times-Roman";
        }
        else if (fam.Contains("mono") || fam.Contains("courier") || fam.Contains("code"))
        {
            fontName = "Courier";
        }

        XFontStyle style = XFontStyle.Regular;
        if (bold && italic) style = XFontStyle.BoldItalic;
        else if (bold) style = XFontStyle.Bold;
        else if (italic) style = XFontStyle.Italic;

        double size = Math.Max(4.0, ptSize);
        return new XFont(fontName, size, style);
    }

    private static XColor? ParseColor(string? hexStr)
    {
        if (string.IsNullOrWhiteSpace(hexStr)) return null;
        string trimmed = hexStr!.Trim();
        if (string.Equals(trimmed, "transparent", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(trimmed, "none", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        string s = trimmed;
        if (s.StartsWith("#")) s = s.Substring(1);

        try
        {
            if (s.Length == 3)
            {
                byte r = byte.Parse(string.Concat(s[0], s[0]), NumberStyles.HexNumber);
                byte g = byte.Parse(string.Concat(s[1], s[1]), NumberStyles.HexNumber);
                byte b = byte.Parse(string.Concat(s[2], s[2]), NumberStyles.HexNumber);
                return XColor.FromArgb(r, g, b);
            }
            if (s.Length == 6)
            {
                byte r = byte.Parse(s.Substring(0, 2), NumberStyles.HexNumber);
                byte g = byte.Parse(s.Substring(2, 2), NumberStyles.HexNumber);
                byte b = byte.Parse(s.Substring(4, 2), NumberStyles.HexNumber);
                return XColor.FromArgb(r, g, b);
            }
            if (s.Length == 8)
            {
                byte a = byte.Parse(s.Substring(0, 2), NumberStyles.HexNumber);
                byte r = byte.Parse(s.Substring(2, 2), NumberStyles.HexNumber);
                byte g = byte.Parse(s.Substring(4, 2), NumberStyles.HexNumber);
                byte b = byte.Parse(s.Substring(6, 2), NumberStyles.HexNumber);
                return XColor.FromArgb(a, r, g, b);
            }
        }
        catch
        {
            // fallback
        }

        return XColors.Black;
    }
}
