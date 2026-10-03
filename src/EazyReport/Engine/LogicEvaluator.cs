using EazyReport.Expressions;
using EazyReport.Model;

namespace EazyReport.Engine;

public class RenderCtx
{
    public object? Data { get; set; }
    public object? Item { get; set; }
    public Dictionary<string, object?> Params { get; set; } = new();
    public int PageNumber { get; set; } = 1;
    public int TotalPages { get; set; } = 1;

    public RenderCtx Clone()
    {
        return new RenderCtx
        {
            Data = this.Data,
            Item = this.Item,
            Params = new Dictionary<string, object?>(this.Params),
            PageNumber = this.PageNumber,
            TotalPages = this.TotalPages
        };
    }

    public object? ResolveValue(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;

        if (string.Equals(path, "Page", StringComparison.OrdinalIgnoreCase)) return PageNumber;
        if (string.Equals(path, "TotalPages", StringComparison.OrdinalIgnoreCase)) return TotalPages;

        if (path.StartsWith("params.", StringComparison.OrdinalIgnoreCase))
        {
            string pKey = path.Substring(7);
            return Params.GetValueOrDefault(pKey);
        }

        // Try item context first, then root data, then params
        if (Item != null)
        {
            var v = ExpressionEngine.GetPath(Item, path);
            if (v != null) return v;
        }

        if (Data != null)
        {
            var v = ExpressionEngine.GetPath(Data, path);
            if (v != null) return v;
        }

        if (Params.TryGetValue(path, out var pv)) return pv;

        return null;
    }
}

public static class LogicEvaluator
{
    public static void ApplyRules(LayoutElement el, RenderCtx rc)
    {
        if (el.Rules == null || el.Rules.Count == 0) return;

        foreach (var rule in el.Rules)
        {
            if (!rule.Enabled || string.IsNullOrWhiteSpace(rule.Condition)) continue;

            bool matches = ExpressionEngine.EvaluateCondition(rule.Condition, BandLayout.BuildContextObject(rc));
            if (!matches) continue;

            string prop = (rule.Property ?? "").Trim().ToLowerInvariant();
            string valStr = rule.Value?.ToString() ?? "";

            switch (prop)
            {
                case "color":
                case "fontcolor":
                case "textcolor":
                    el.Style.Color = valStr;
                    break;
                case "background":
                case "backgroundcolor":
                case "fill":
                    el.Box.Background = valStr;
                    break;
                case "bold":
                case "fontweight":
                    el.Style.Bold = ExpressionEngine.IsTruthy(rule.Value);
                    break;
                case "italic":
                    el.Style.Italic = ExpressionEngine.IsTruthy(rule.Value);
                    break;
                case "bordercolor":
                    el.Box.BorderColor = valStr;
                    break;
                case "borderwidth":
                    el.Box.BorderWidth = Formatters.ToDouble(rule.Value);
                    break;
                case "visible":
                    if (!ExpressionEngine.IsTruthy(rule.Value))
                    {
                        el.Opacity = 0.0;
                    }
                    break;
            }
        }
    }
}
