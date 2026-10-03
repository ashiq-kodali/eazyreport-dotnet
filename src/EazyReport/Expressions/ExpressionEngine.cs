using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace EazyReport.Expressions;

public static class ExpressionEngine
{
    private static readonly Dictionary<string, Func<List<object?>, object?>> Helpers = new(StringComparer.OrdinalIgnoreCase);

    static ExpressionEngine()
    {
        RegisterDefaultHelpers();
    }

    public static void RegisterHelper(string name, Func<List<object?>, object?> func)
    {
        Helpers[name] = func;
    }

    public static object? GetPath(object? root, string? path)
    {
        if (root == null || string.IsNullOrWhiteSpace(path)) return root;

        string[] parts = path!.Split('.');
        object? current = root;

        foreach (var part in parts)
        {
            if (current == null) return null;

            if (current is JsonElement je)
            {
                if (je.ValueKind == JsonValueKind.Object)
                {
                    if (je.TryGetProperty(part, out var nextProp))
                    {
                        current = nextProp;
                    }
                    else
                    {
                        // Case-insensitive lookup
                        bool found = false;
                        foreach (var prop in je.EnumerateObject())
                        {
                            if (string.Equals(prop.Name, part, StringComparison.OrdinalIgnoreCase))
                            {
                                current = prop.Value;
                                found = true;
                                break;
                            }
                        }
                        if (!found) return null;
                    }
                }
                else if (je.ValueKind == JsonValueKind.Array && int.TryParse(part, out int arrIdx))
                {
                    int i = 0;
                    bool found = false;
                    foreach (var item in je.EnumerateArray())
                    {
                        if (i == arrIdx)
                        {
                            current = item;
                            found = true;
                            break;
                        }
                        i++;
                    }
                    if (!found) return null;
                }
                else
                {
                    return null;
                }
                continue;
            }

            if (current is IDictionary dict)
            {
                if (dict.Contains(part))
                {
                    current = dict[part];
                }
                else
                {
                    // Case-insensitive lookup
                    bool found = false;
                    foreach (var key in dict.Keys)
                    {
                        if (key != null && string.Equals(key.ToString(), part, StringComparison.OrdinalIgnoreCase))
                        {
                            current = dict[key];
                            found = true;
                            break;
                        }
                    }
                    if (!found) return null;
                }
                continue;
            }

            if (current is IList list && int.TryParse(part, out int idx))
            {
                if (idx >= 0 && idx < list.Count)
                {
                    current = list[idx];
                }
                else
                {
                    return null;
                }
                continue;
            }

            // Reflection lookup
            var propInfo = current.GetType().GetProperty(part, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (propInfo != null)
            {
                current = propInfo.GetValue(current);
            }
            else
            {
                var fieldInfo = current.GetType().GetField(part, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (fieldInfo != null)
                {
                    current = fieldInfo.GetValue(current);
                }
                else
                {
                    return null;
                }
            }
        }

        // Unbox JsonElement primitives to C# native types
        if (current is JsonElement finalJe)
        {
            return finalJe.ValueKind switch
            {
                JsonValueKind.String => finalJe.GetString(),
                JsonValueKind.Number => finalJe.TryGetInt64(out var l) ? l : finalJe.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                _ => finalJe
            };
        }

        return current;
    }

    public static object? EvaluateExpression(string expr, object? ctx)
    {
        if (string.IsNullOrWhiteSpace(expr)) return null;

        var tokens = Tokenize(expr.Trim());
        if (tokens.Count == 0) return null;

        string first = tokens[0].Text;

        if (Helpers.TryGetValue(first, out var helper))
        {
            var args = new List<object?>();
            for (int i = 1; i < tokens.Count; i++)
            {
                args.Add(EvaluateToken(tokens[i], ctx));
            }
            return helper(args);
        }

        return EvaluateToken(tokens[0], ctx);
    }

    public static bool EvaluateCondition(string? expr, object? ctx)
    {
        if (string.IsNullOrWhiteSpace(expr)) return true;
        var res = EvaluateExpression(expr!, ctx);
        return IsTruthy(res);
    }

    public static string EvaluateTemplate(string template, object? ctx)
    {
        if (string.IsNullOrEmpty(template)) return "";

        // Process {{#if ...}} ... {{else}} ... {{/if}} blocks
        string processed = ProcessIfBlocks(template, ctx);

        // Process {{#each ...}} ... {{/each}} blocks
        processed = ProcessEachBlocks(processed, ctx);

        // Process {{expression}} and {{{expression}}}
        var regex = new Regex(@"\{\{\{?([^\}]+)\}?\}\}");
        return regex.Replace(processed, match =>
        {
            string inner = match.Groups[1].Value.Trim();
            var val = EvaluateExpression(inner, ctx);
            return val?.ToString() ?? "";
        });
    }

    private static string ProcessIfBlocks(string text, object? ctx)
    {
        var ifRegex = new Regex(@"\{\{#if\s+([^\}]+)\}\}(.*?)(?:\{\{else\}\}(.*?))?\{\{/if\}\}", RegexOptions.Singleline);
        return ifRegex.Replace(text, match =>
        {
            string condExpr = match.Groups[1].Value.Trim();
            string thenBlock = match.Groups[2].Value;
            string elseBlock = match.Groups[3].Success ? match.Groups[3].Value : "";

            bool conditionMet = EvaluateCondition(condExpr, ctx);
            string selected = conditionMet ? thenBlock : elseBlock;
            return ProcessIfBlocks(selected, ctx);
        });
    }

    private static string ProcessEachBlocks(string text, object? ctx)
    {
        var eachRegex = new Regex(@"\{\{#each\s+([^\}]+)\}\}(.*?)\{\{/each\}\}", RegexOptions.Singleline);
        return eachRegex.Replace(text, match =>
        {
            string listExpr = match.Groups[1].Value.Trim();
            string body = match.Groups[2].Value;

            object? listObj = EvaluateExpression(listExpr, ctx);
            if (listObj == null) return "";

            var sb = new StringBuilder();
            IEnumerable? enumerable = null;

            if (listObj is JsonElement je && je.ValueKind == JsonValueKind.Array)
            {
                enumerable = je.EnumerateArray();
            }
            else if (listObj is IEnumerable ie && !(listObj is string))
            {
                enumerable = ie;
            }

            if (enumerable != null)
            {
                int index = 0;
                foreach (var item in enumerable)
                {
                    var itemCtx = new Dictionary<string, object?>
                    {
                        ["this"] = item,
                        ["item"] = item,
                        ["@index"] = index,
                        ["@first"] = index == 0
                    };

                    if (ctx is IDictionary<string, object?> parentDict)
                    {
                        foreach (var kvp in parentDict)
                        {
                            if (!itemCtx.ContainsKey(kvp.Key))
                                itemCtx[kvp.Key] = kvp.Value;
                        }
                    }

                    sb.Append(EvaluateTemplate(body, itemCtx));
                    index++;
                }
            }

            return sb.ToString();
        });
    }

    private static object? EvaluateToken(Token token, object? ctx)
    {
        if (token.IsSubExpression)
        {
            return EvaluateExpression(token.Text, ctx);
        }

        string raw = token.Text;

        if ((raw.StartsWith("\"") && raw.EndsWith("\"")) || (raw.StartsWith("'") && raw.EndsWith("'")))
        {
            return raw.Length >= 2 ? raw.Substring(1, raw.Length - 2) : "";
        }

        if (bool.TryParse(raw, out bool b)) return b;
        if (long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out long l)) return l;
        if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double d)) return d;
        if (string.Equals(raw, "null", StringComparison.OrdinalIgnoreCase)) return null;

        return GetPath(ctx, raw);
    }

    public static bool IsTruthy(object? v)
    {
        if (v == null) return false;
        if (v is bool b) return b;
        if (v is int i) return i != 0;
        if (v is long l) return l != 0;
        if (v is double d) return d != 0.0 && !double.IsNaN(d);
        if (v is float f) return f != 0.0f && !float.IsNaN(f);
        if (v is decimal m) return m != 0m;
        if (v is string s)
        {
            string trimmed = s.Trim().ToLowerInvariant();
            return !string.IsNullOrEmpty(trimmed) && trimmed != "false" && trimmed != "0" && trimmed != "null" && trimmed != "undefined";
        }
        if (v is ICollection col) return col.Count > 0;
        if (v is JsonElement je)
        {
            return je.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => false,
                JsonValueKind.Undefined => false,
                JsonValueKind.Number => je.GetDouble() != 0,
                JsonValueKind.String => !string.IsNullOrEmpty(je.GetString()),
                JsonValueKind.Array => je.GetArrayLength() > 0,
                _ => true
            };
        }
        return true;
    }

    private class Token
    {
        public string Text { get; }
        public bool IsSubExpression { get; }

        public Token(string text, bool isSubExpression = false)
        {
            Text = text;
            IsSubExpression = isSubExpression;
        }
    }

    private static List<Token> Tokenize(string input)
    {
        var tokens = new List<Token>();
        int i = 0;
        int n = input.Length;

        while (i < n)
        {
            while (i < n && char.IsWhiteSpace(input[i])) i++;
            if (i >= n) break;

            char ch = input[i];

            if (ch == '"' || ch == '\'')
            {
                char quote = ch;
                i++;
                var sb = new StringBuilder();
                while (i < n && input[i] != quote)
                {
                    if (input[i] == '\\' && i + 1 < n)
                    {
                        i++;
                        sb.Append(input[i]);
                    }
                    else
                    {
                        sb.Append(input[i]);
                    }
                    i++;
                }
                if (i < n) i++; // Closing quote
                tokens.Add(new Token($"\"{sb}\""));
                continue;
            }

            if (ch == '(')
            {
                i++;
                int depth = 1;
                var sb = new StringBuilder();
                while (i < n && depth > 0)
                {
                    if (input[i] == '(') depth++;
                    else if (input[i] == ')') depth--;

                    if (depth > 0) sb.Append(input[i]);
                    i++;
                }
                tokens.Add(new Token(sb.ToString().Trim(), isSubExpression: true));
                continue;
            }

            var word = new StringBuilder();
            while (i < n && !char.IsWhiteSpace(input[i]) && input[i] != ')' && input[i] != '"' && input[i] != '\'')
            {
                word.Append(input[i]);
                i++;
            }
            tokens.Add(new Token(word.ToString()));
        }

        return tokens;
    }

    private static void RegisterDefaultHelpers()
    {
        // Formatting
        RegisterHelper("formatCurrency", args =>
        {
            var val = args.Count > 0 ? args[0] : null;
            string cur = args.Count > 1 ? args[1]?.ToString() ?? "USD" : "USD";
            int dec = args.Count > 2 ? (int)Formatters.ToDouble(args[2]) : 2;
            return Formatters.FormatCurrency(val, cur, dec);
        });

        RegisterHelper("formatNumber", args =>
        {
            var val = args.Count > 0 ? args[0] : null;
            int dec = args.Count > 1 ? (int)Formatters.ToDouble(args[1]) : 2;
            bool thousands = args.Count <= 2 || IsTruthy(args[2]);
            return Formatters.FormatNumber(val, dec, thousands);
        });

        RegisterHelper("formatDate", args =>
        {
            var val = args.Count > 0 ? args[0] : null;
            string pattern = args.Count > 1 ? args[1]?.ToString() ?? "dd/MM/yyyy" : "dd/MM/yyyy";
            var dt = Formatters.ToDate(val);
            return dt.HasValue ? Formatters.FormatDatePattern(dt.Value, pattern) : (val?.ToString() ?? "");
        });

        RegisterHelper("formatPercent", args =>
        {
            var val = args.Count > 0 ? args[0] : null;
            int dec = args.Count > 1 ? (int)Formatters.ToDouble(args[1]) : 0;
            double d = Formatters.ToDouble(val);
            return $"{d.ToString($"F{dec}", CultureInfo.InvariantCulture)}%";
        });

        RegisterHelper("inWords", args =>
        {
            var val = args.Count > 0 ? args[0] : null;
            string cur = args.Count > 1 ? args[1]?.ToString() ?? "" : "";
            return NumberToWords.Convert(Formatters.ToDouble(val), cur);
        });

        RegisterHelper("numberToWords", args =>
        {
            var val = args.Count > 0 ? args[0] : null;
            string cur = args.Count > 1 ? args[1]?.ToString() ?? "" : "";
            return NumberToWords.Convert(Formatters.ToDouble(val), cur);
        });

        // Math
        RegisterHelper("add", args => args.Count < 2 ? 0.0 : Formatters.ToDouble(args[0]) + Formatters.ToDouble(args[1]));
        RegisterHelper("sub", args => args.Count < 2 ? 0.0 : Formatters.ToDouble(args[0]) - Formatters.ToDouble(args[1]));
        RegisterHelper("mul", args => args.Count < 2 ? 0.0 : Formatters.ToDouble(args[0]) * Formatters.ToDouble(args[1]));
        RegisterHelper("div", args =>
        {
            if (args.Count < 2) return 0.0;
            double d2 = Formatters.ToDouble(args[1]);
            return d2 == 0.0 ? 0.0 : Formatters.ToDouble(args[0]) / d2;
        });
        RegisterHelper("round", args =>
        {
            if (args.Count == 0) return 0.0;
            double val = Formatters.ToDouble(args[0]);
            int dec = args.Count > 1 ? (int)Formatters.ToDouble(args[1]) : 0;
            return Math.Round(val, dec);
        });
        RegisterHelper("abs", args => args.Count == 0 ? 0.0 : Math.Abs(Formatters.ToDouble(args[0])));

        // Logic
        RegisterHelper("eq", args => args.Count >= 2 && ValuesEqual(args[0], args[1]));
        RegisterHelper("neq", args => args.Count < 2 || !ValuesEqual(args[0], args[1]));
        RegisterHelper("gt", args => args.Count >= 2 && Formatters.ToDouble(args[0]) > Formatters.ToDouble(args[1]));
        RegisterHelper("gte", args => args.Count >= 2 && Formatters.ToDouble(args[0]) >= Formatters.ToDouble(args[1]));
        RegisterHelper("lt", args => args.Count >= 2 && Formatters.ToDouble(args[0]) < Formatters.ToDouble(args[1]));
        RegisterHelper("lte", args => args.Count >= 2 && Formatters.ToDouble(args[0]) <= Formatters.ToDouble(args[1]));
        RegisterHelper("and", args => args.All(IsTruthy));
        RegisterHelper("or", args => args.Any(IsTruthy));
        RegisterHelper("not", args => args.Count > 0 && !IsTruthy(args[0]));
        RegisterHelper("if", args =>
        {
            if (args.Count < 2) return null;
            return IsTruthy(args[0]) ? args[1] : (args.Count > 2 ? args[2] : null);
        });

        // Strings
        RegisterHelper("uppercase", args => args.Count > 0 ? args[0]?.ToString()?.ToUpperInvariant() ?? "" : "");
        RegisterHelper("lowercase", args => args.Count > 0 ? args[0]?.ToString()?.ToLowerInvariant() ?? "" : "");
        RegisterHelper("trim", args => args.Count > 0 ? args[0]?.ToString()?.Trim() ?? "" : "");
        RegisterHelper("concat", args => string.Concat(args.Select(a => a?.ToString() ?? "")));
        RegisterHelper("replace", args =>
        {
            if (args.Count < 3) return args.Count > 0 ? args[0]?.ToString() ?? "" : "";
            string s = args[0]?.ToString() ?? "";
            string from = args[1]?.ToString() ?? "";
            string to = args[2]?.ToString() ?? "";
            return s.Replace(from, to);
        });

        // Aggregates
        RegisterHelper("sum", args =>
        {
            if (args.Count == 0) return 0.0;
            var list = GetList(args[0]);
            string prop = args.Count > 1 ? args[1]?.ToString() ?? "" : "";
            double total = 0.0;
            foreach (var item in list)
            {
                object? v = string.IsNullOrEmpty(prop) ? item : GetPath(item, prop);
                total += Formatters.ToDouble(v);
            }
            return total;
        });

        RegisterHelper("avg", args =>
        {
            if (args.Count == 0) return 0.0;
            var list = GetList(args[0]);
            if (list.Count == 0) return 0.0;
            string prop = args.Count > 1 ? args[1]?.ToString() ?? "" : "";
            double total = 0.0;
            foreach (var item in list)
            {
                object? v = string.IsNullOrEmpty(prop) ? item : GetPath(item, prop);
                total += Formatters.ToDouble(v);
            }
            return total / list.Count;
        });

        RegisterHelper("count", args =>
        {
            if (args.Count == 0) return 0;
            var list = GetList(args[0]);
            return list.Count;
        });
    }

    private static bool ValuesEqual(object? a, object? b)
    {
        if (a == null && b == null) return true;
        if (a == null || b == null) return false;

        if (a is bool ba && b is bool bb) return ba == bb;

        if (IsNumeric(a) && IsNumeric(b))
        {
            return Math.Abs(Formatters.ToDouble(a) - Formatters.ToDouble(b)) < 0.000001;
        }

        return string.Equals(a.ToString(), b.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsNumeric(object? v)
    {
        return v is int or long or double or float or decimal or short or byte;
    }

    private static List<object?> GetList(object? obj)
    {
        var res = new List<object?>();
        if (obj == null) return res;

        if (obj is JsonElement je && je.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in je.EnumerateArray()) res.Add(item);
            return res;
        }

        if (obj is IEnumerable ie && !(obj is string))
        {
            foreach (var item in ie) res.Add(item);
            return res;
        }

        return res;
    }
}
