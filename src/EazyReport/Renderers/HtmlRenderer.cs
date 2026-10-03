using System.Text;
using EazyReport.Engine;
using EazyReport.Expressions;
using EazyReport.Model;

namespace EazyReport.Renderers;

public static class HtmlRenderer
{
    private const string FontLink =
        "https://fonts.googleapis.com/css2?" +
        "family=Inter:wght@400;500;600;700&" +
        "family=Merriweather:wght@400;700&" +
        "family=Playfair+Display:wght@600;700&" +
        "family=Roboto+Mono:wght@400;500&display=swap";

    public static string Render(List<RenderedPage> pages, string title = "Report")
    {
        if (pages.Count == 0)
        {
            return $"<!DOCTYPE html><html><head><meta charset=\"utf-8\"><title>{Formatters.EscapeHtml(title)}</title></head><body></body></html>";
        }

        var first = pages[0];
        var sb = new StringBuilder();

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"utf-8\">");
        sb.AppendLine($"  <title>{Formatters.EscapeHtml(title)}</title>");
        sb.AppendLine($"  <link rel=\"stylesheet\" href=\"{FontLink}\">");
        sb.AppendLine("  <style>");
        sb.AppendLine($"    @page {{ size: {first.Width:F2}mm {first.Height:F2}mm; margin: 0; }}");
        sb.AppendLine("    *, *::before, *::after { box-sizing: border-box; }");
        sb.AppendLine("    body { margin: 0; padding: 0; background-color: #f1f5f9; font-family: 'Inter', sans-serif; -webkit-print-color-adjust: exact; print-color-adjust: exact; }");
        sb.AppendLine("    .report-sheet { background-color: #ffffff; margin: 20px auto; box-shadow: 0 4px 12px rgba(0,0,0,0.12); position: relative; overflow: hidden; page-break-after: always; break-after: page; }");
        sb.AppendLine("    @media print {");
        sb.AppendLine("      body { background: transparent !important; }");
        sb.AppendLine("      .report-sheet { margin: 0 !important; box-shadow: none !important; width: 100% !important; height: 100% !important; }");
        sb.AppendLine("    }");
        sb.AppendLine("  </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");

        foreach (var page in pages)
        {
            sb.AppendLine($"  <div class=\"report-sheet\" style=\"width:{page.Width:F2}mm;height:{page.Height:F2}mm;\">");

            // Watermark
            if (page.Watermark.Enabled && !string.IsNullOrEmpty(page.Watermark.Text))
            {
                sb.AppendLine("    <div style=\"position:absolute;left:0;top:0;width:100%;height:100%;display:flex;align-items:center;justify-content:center;pointer-events:none;z-index:0;\">");
                sb.AppendLine($"      <span style=\"font-size:{page.Watermark.FontSize:F1}pt;color:{page.Watermark.Color};opacity:{page.Watermark.Opacity:F2};transform:rotate({page.Watermark.Angle:F1}deg);font-weight:700;user-select:none;\">{Formatters.EscapeHtml(page.Watermark.Text)}</span>");
                sb.AppendLine("    </div>");
            }

            // Page content
            sb.AppendLine(page.Html);

            sb.AppendLine("  </div>");
        }

        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }
}
