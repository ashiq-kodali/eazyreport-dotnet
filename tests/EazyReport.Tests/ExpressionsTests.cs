using EazyReport.Expressions;
using Xunit;

namespace EazyReport.Tests;

public class ExpressionsTests
{
    [Fact]
    public void NumberToWords_ConvertsProperly()
    {
        string wordsUsd = NumberToWords.Convert(1250.50, "USD");
        Assert.Contains("One Thousand Two Hundred Fifty Dollars", wordsUsd);
        Assert.Contains("Fifty Cents", wordsUsd);

        string wordsInr = NumberToWords.Convert(500, "INR");
        Assert.Contains("Five Hundred Rupees Only", wordsInr);

        string wordsNoCur = NumberToWords.Convert(42.75);
        Assert.Contains("Forty Two and 75/100", wordsNoCur);
    }

    [Fact]
    public void Formatters_FormatsCurrencyAndNumbers()
    {
        string cur = Formatters.FormatCurrency(1234.56, "USD", 2);
        Assert.Equal("$1,234.56", cur);

        string num = Formatters.FormatNumber(9876543.21, 1, true);
        Assert.Equal("9,876,543.2", num);

        var dt = new DateTime(2026, 10, 3);
        string formattedDate = Formatters.FormatDatePattern(dt, "yyyy-MM-dd");
        Assert.Equal("2026-10-03", formattedDate);
    }

    [Fact]
    public void ExpressionEngine_EvaluatesMathAndLogic()
    {
        var ctx = new Dictionary<string, object?>
        {
            ["qty"] = 15,
            ["price"] = 20.0
        };

        var mulRes = ExpressionEngine.EvaluateExpression("mul qty price", ctx);
        Assert.Equal(300.0, mulRes);

        var gtRes = ExpressionEngine.EvaluateExpression("gt qty 10", ctx);
        Assert.Equal(true, gtRes);

        var ifRes = ExpressionEngine.EvaluateExpression("if (gt qty 10) \"Bulk\" \"Retail\"", ctx);
        Assert.Equal("Bulk", ifRes);
    }

    [Fact]
    public void ExpressionEngine_EvaluatesTemplatesWithBlocks()
    {
        var ctx = new Dictionary<string, object?>
        {
            ["customer"] = new Dictionary<string, object?> { ["name"] = "Alice" },
            ["items"] = new List<object?>
            {
                new Dictionary<string, object?> { ["name"] = "Widget A", ["qty"] = 2 },
                new Dictionary<string, object?> { ["name"] = "Widget B", ["qty"] = 5 }
            }
        };

        string template = "Hello {{customer.name}}! Total items: {{count items}}, Total qty: {{sum items \"qty\"}}.";
        string rendered = ExpressionEngine.EvaluateTemplate(template, ctx);

        Assert.Equal("Hello Alice! Total items: 2, Total qty: 7.", rendered);
    }
}
