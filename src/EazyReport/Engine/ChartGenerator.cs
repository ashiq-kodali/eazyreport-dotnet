using System.Globalization;
using System.Text;
using EazyReport.Expressions;
using EazyReport.Model;

namespace EazyReport.Engine;

public static class ChartGenerator
{
    public static string GenerateSvg(LayoutElement el, List<string> labels, List<double> values)
    {
        double w = el.W * 10.0;
        double h = el.H * 10.0;
        double pt = 3.528;
        double fs = el.Style.FontSize * pt;
        string font = $"font-family=\"{Formatters.EscapeHtml(el.Style.FontFamily)}, sans-serif\" fill=\"{el.Style.Color}\"";

        var colors = el.Colors != null && el.Colors.Count > 0 ? el.Colors : new List<string> { "#3b82f6", "#10b981", "#f59e0b", "#ef4444" };
        string Color(int i) => colors[i % colors.Count];

        var parts = new List<string>();
        double top = 4.0;

        if (!string.IsNullOrWhiteSpace(el.Title))
        {
            parts.Add($"<text x=\"{w / 2}\" y=\"{top + fs * 1.1}\" text-anchor=\"middle\" font-size=\"{fs * 1.15}\" font-weight=\"700\" {font}>{Formatters.EscapeHtml(el.Title)}</text>");
            top += fs * 1.6;
        }

        if (values.Count == 0)
        {
            parts.Add($"<text x=\"{w / 2}\" y=\"{h / 2}\" text-anchor=\"middle\" font-size=\"{fs}\" {font}>No data</text>");
            return Wrap(w, h, parts);
        }

        string chartType = (el.ChartType ?? "column").ToLowerInvariant();

        if (chartType is "pie" or "doughnut")
        {
            double legendW = el.ShowLegend ? Math.Min(w * 0.4, 400.0) : 0.0;
            double cx = (w - legendW) / 2.0;
            double cy = top + (h - top) / 2.0;
            double r = Math.Max(10.0, Math.Min((w - legendW) / 2.0, (h - top) / 2.0) - 8.0);
            double total = values.Sum(v => Math.Max(0.0, v));
            if (total == 0) total = 1.0;

            double a0 = -Math.PI / 2.0;
            for (int i = 0; i < values.Count; i++)
            {
                double v = values[i];
                double a1 = a0 + (Math.Max(0.0, v) / total) * Math.PI * 2.0;
                int large = (a1 - a0 > Math.PI) ? 1 : 0;

                string P(double a, double rr) => $"{cx + rr * Math.Cos(a):F2},{cy + rr * Math.Sin(a):F2}";

                if (values.Count == 1)
                {
                    parts.Add($"<circle cx=\"{cx:F2}\" cy=\"{cy:F2}\" r=\"{r:F2}\" fill=\"{Color(i)}\"/>");
                }
                else
                {
                    parts.Add($"<path d=\"M{cx:F2},{cy:F2} L{P(a0, r)} A{r:F2},{r:F2} 0 {large} 1 {P(a1, r)} Z\" fill=\"{Color(i)}\" stroke=\"#fff\" stroke-width=\"3\"/>");
                }

                if (el.ShowValues && v > 0)
                {
                    double am = (a0 + a1) / 2.0;
                    double lr = chartType == "doughnut" ? r * 0.78 : r * 0.62;
                    int pct = (int)Math.Round((v / total) * 100.0);
                    parts.Add($"<text x=\"{cx + lr * Math.Cos(am):F2}\" y=\"{cy + lr * Math.Sin(am) + fs / 3.0:F2}\" text-anchor=\"middle\" font-size=\"{fs * 0.9:F2}\" font-family=\"sans-serif\" fill=\"#fff\" font-weight=\"700\">{pct}%</text>");
                }

                a0 = a1;
            }

            if (chartType == "doughnut")
            {
                parts.Add($"<circle cx=\"{cx:F2}\" cy=\"{cy:F2}\" r=\"{r * 0.55:F2}\" fill=\"#fff\"/>");
            }

            if (el.ShowLegend)
            {
                DrawLegend(parts, labels, w - legendW + 10.0, top + 10.0, fs, font, Color);
            }

            return Wrap(w, h, parts);
        }

        // Bar / Column / Line / Area
        bool horizontal = chartType == "bar";
        double maxVal = values.Max();
        double minVal = values.Min();
        double chartMax = Math.Max(0.0, maxVal);
        double chartMin = Math.Min(0.0, minVal);
        double valRange = (chartMax - chartMin != 0) ? (chartMax - chartMin) : 1.0;

        int maxLabelLen = labels.Count > 0 ? labels.Max(l => l.Length) : 0;
        double labelSpace = horizontal ? Math.Min(w * 0.3, maxLabelLen * fs * 0.55 + 10.0) : fs * 1.8;
        double valueSpace = horizontal ? fs * 5.0 : fs * 5.5;

        double plotX = horizontal ? labelSpace : valueSpace;
        double plotY = horizontal ? (top + 6.0) : (top + fs);
        double plotW = horizontal ? (w - labelSpace - fs * 4.0) : (w - valueSpace - 10.0);
        double plotH = horizontal ? (h - top - 12.0) : (h - top - fs - labelSpace);

        double Scale(double v) => ((v - chartMin) / valRange) * (horizontal ? plotW : plotH);

        if (el.ShowGrid)
        {
            for (int i = 0; i < 5; i++)
            {
                double v = chartMin + (valRange * i) / 4.0;
                string vStr = Formatters.FormatNumber(v, 0);

                if (horizontal)
                {
                    double gx = plotX + Scale(v);
                    parts.Add($"<line x1=\"{gx:F2}\" y1=\"{plotY:F2}\" x2=\"{gx:F2}\" y2=\"{plotY + plotH:F2}\" stroke=\"#e5e7eb\" stroke-width=\"1\"/>");
                    parts.Add($"<text x=\"{gx:F2}\" y=\"{plotY + plotH + fs * 1.1:F2}\" text-anchor=\"middle\" font-size=\"{fs * 0.8:F2}\" {font}>{vStr}</text>");
                }
                else
                {
                    double gy = plotY + plotH - Scale(v);
                    parts.Add($"<line x1=\"{plotX:F2}\" y1=\"{gy:F2}\" x2=\"{plotX + plotW:F2}\" y2=\"{gy:F2}\" stroke=\"#e5e7eb\" stroke-width=\"1\"/>");
                    parts.Add($"<text x=\"{plotX - 6.0:F2}\" y=\"{gy + fs * 0.35:F2}\" text-anchor=\"end\" font-size=\"{fs * 0.8:F2}\" {font}>{vStr}</text>");
                }
            }
        }

        int count = values.Count;
        double step = (horizontal ? plotH : plotW) / Math.Max(1, count);
        double barThickness = step * 0.65;

        for (int i = 0; i < count; i++)
        {
            double v = values[i];
            string lbl = i < labels.Count ? labels[i] : "";
            string barColor = Color(i);

            if (horizontal)
            {
                double by = plotY + i * step + (step - barThickness) / 2.0;
                double bw = Math.Max(2.0, Scale(v) - Scale(0.0));
                double bx = plotX + Scale(0.0);

                parts.Add($"<rect x=\"{bx:F2}\" y=\"{by:F2}\" width=\"{bw:F2}\" height=\"{barThickness:F2}\" rx=\"3\" fill=\"{barColor}\"/>");
                parts.Add($"<text x=\"{plotX - 6.0:F2}\" y=\"{by + barThickness / 2.0 + fs * 0.35:F2}\" text-anchor=\"end\" font-size=\"{fs * 0.85:F2}\" {font}>{Formatters.EscapeHtml(lbl)}</text>");

                if (el.ShowValues)
                {
                    parts.Add($"<text x=\"{bx + bw + 4.0:F2}\" y=\"{by + barThickness / 2.0 + fs * 0.35:F2}\" text-anchor=\"start\" font-size=\"{fs * 0.8:F2}\" font-weight=\"600\" {font}>{Formatters.FormatNumber(v, 0)}</text>");
                }
            }
            else
            {
                double bx = plotX + i * step + (step - barThickness) / 2.0;
                double bh = Math.Max(2.0, Scale(v) - Scale(0.0));
                double by = plotY + plotH - bh;

                parts.Add($"<rect x=\"{bx:F2}\" y=\"{by:F2}\" width=\"{barThickness:F2}\" height=\"{bh:F2}\" rx=\"3\" fill=\"{barColor}\"/>");
                parts.Add($"<text x=\"{bx + barThickness / 2.0:F2}\" y=\"{plotY + plotH + fs * 1.1:F2}\" text-anchor=\"middle\" font-size=\"{fs * 0.85:F2}\" {font}>{Formatters.EscapeHtml(lbl)}</text>");

                if (el.ShowValues)
                {
                    parts.Add($"<text x=\"{bx + barThickness / 2.0:F2}\" y=\"{by - 4.0:F2}\" text-anchor=\"middle\" font-size=\"{fs * 0.8:F2}\" font-weight=\"600\" {font}>{Formatters.FormatNumber(v, 0)}</text>");
                }
            }
        }

        return Wrap(w, h, parts);
    }

    private static void DrawLegend(List<string> parts, List<string> labels, double x, double y, double fs, string font, Func<int, string> color)
    {
        double cy = y;
        for (int i = 0; i < labels.Count; i++)
        {
            parts.Add($"<rect x=\"{x:F2}\" y=\"{cy:F2}\" width=\"{fs * 0.8:F2}\" height=\"{fs * 0.8:F2}\" rx=\"2\" fill=\"{color(i)}\"/>");
            parts.Add($"<text x=\"{x + fs * 1.2:F2}\" y=\"{cy + fs * 0.7:F2}\" font-size=\"{fs * 0.8:F2}\" {font}>{Formatters.EscapeHtml(labels[i])}</text>");
            cy += fs * 1.5;
        }
    }

    private static string Wrap(double w, double h, List<string> parts)
    {
        var sb = new StringBuilder();
        sb.Append($"<svg viewBox=\"0 0 {w:F2} {h:F2}\" preserveAspectRatio=\"xMidYMid meet\" style=\"display:block;width:100%;height:100%\" xmlns=\"http://www.w3.org/2000/svg\">");
        foreach (var p in parts) sb.Append(p);
        sb.Append("</svg>");
        return sb.ToString();
    }
}
