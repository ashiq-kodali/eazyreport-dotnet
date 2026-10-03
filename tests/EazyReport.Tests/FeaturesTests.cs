using EazyReport.Engine;
using EazyReport.Model;
using Xunit;

namespace EazyReport.Tests;

public class FeaturesTests
{
    [Fact]
    public void BarcodeGenerator_GeneratesCode128AndQrCode()
    {
        string svg128 = BarcodeGenerator.GenerateSvg("code128", "INV-88219", "#000000", "#ffffff", true);
        Assert.Contains("<svg", svg128);
        Assert.Contains("INV-88219", svg128);

        string svgQr = BarcodeGenerator.GenerateSvg("qrcode", "https://eazyreport.in", "#1e293b", "#ffffff", false);
        Assert.Contains("<svg", svgQr);
        Assert.Contains("viewBox=\"0 0", svgQr);
    }

    [Fact]
    public void ChartGenerator_GeneratesColumnAndPieCharts()
    {
        var elColumn = new LayoutElement
        {
            ChartType = "column",
            W = 80,
            H = 50,
            Title = "Sales Revenue"
        };
        var labels = new List<string> { "Q1", "Q2", "Q3", "Q4" };
        var values = new List<double> { 12000, 18500, 14200, 26000 };

        string colSvg = ChartGenerator.GenerateSvg(elColumn, labels, values);
        Assert.Contains("<svg", colSvg);
        Assert.Contains("Sales Revenue", colSvg);
        Assert.Contains("Q1", colSvg);

        var elPie = new LayoutElement
        {
            ChartType = "pie",
            W = 60,
            H = 60,
            Title = "Product Share"
        };
        string pieSvg = ChartGenerator.GenerateSvg(elPie, labels, values);
        Assert.Contains("<svg", pieSvg);
        Assert.Contains("Product Share", pieSvg);
    }

    [Fact]
    public void LogicEvaluator_AppliesRuleWhenConditionIsTrue()
    {
        var el = new LayoutElement
        {
            Rules = new List<LogicRule>
            {
                new LogicRule
                {
                    Condition = "gt item.qty 10",
                    Property = "color",
                    Value = "#ef4444"
                }
            }
        };

        var rc = new RenderCtx
        {
            Item = new Dictionary<string, object?> { ["qty"] = 25 }
        };

        LogicEvaluator.ApplyRules(el, rc);
        Assert.Equal("#ef4444", el.Style.Color);
    }

    [Fact]
    public void EazyReportApi_StaticMethodsWorkSeamlessly()
    {
        string rtpl = @"{
            ""format"": ""report-designer-layout"",
            ""version"": 2,
            ""pages"": [{
                ""width"": 210,
                ""height"": 297,
                ""reportTitle"": {
                    ""height"": 30,
                    ""elements"": [{
                        ""type"": ""text"",
                        ""content"": ""Simple Receipt #{{receiptNumber}}"",
                        ""x"": 10, ""y"": 10, ""w"": 100, ""h"": 10
                    }]
                }
            }]
        }";

        var data = new Dictionary<string, object?> { ["receiptNumber"] = "REC-7731" };

        string html = EazyReportApi.RenderHtml(rtpl, data);
        Assert.Contains("Simple Receipt #REC-7731", html);

        byte[] pdf = EazyReportApi.RenderPdf(rtpl, data);
        Assert.True(pdf.Length > 500);

        int count = EazyReportApi.PageCount(rtpl, data);
        Assert.Equal(1, count);
    }

    [Fact]
    public void PaginationEngine_HandlesMultiPageSpill()
    {
        // 100 items will spill across multiple A4 pages
        var items = new List<object?>();
        for (int i = 1; i <= 100; i++)
        {
            items.Add(new Dictionary<string, object?>
            {
                ["sku"] = $"SKU-{i:D4}",
                ["description"] = $"Line Item Description #{i}",
                ["price"] = i * 1.5
            });
        }

        string rtpl = @"{
            ""format"": ""report-designer-layout"",
            ""version"": 2,
            ""pages"": [{
                ""width"": 210,
                ""height"": 297,
                ""margins"": { ""top"": 15, ""bottom"": 15, ""left"": 15, ""right"": 15 },
                ""dataBands"": [{
                    ""name"": ""ItemsData"",
                    ""dataPath"": ""items"",
                    ""height"": 12,
                    ""elements"": [{
                        ""type"": ""text"",
                        ""content"": ""{{item.sku}} - {{item.description}}"",
                        ""x"": 0, ""y"": 0, ""w"": 120, ""h"": 10
                    }]
                }],
                ""pageFooter"": {
                    ""height"": 15,
                    ""elements"": [{
                        ""type"": ""text"",
                        ""content"": ""Page {{Page}} of {{TotalPages}}"",
                        ""x"": 0, ""y"": 0, ""w"": 50, ""h"": 10
                    }]
                }
            }]
        }";

        var data = new Dictionary<string, object?> { ["items"] = items };
        var builder = ReportBuilder.FromTemplate(rtpl).Data(data);

        int totalPages = builder.PageCount();
        Assert.True(totalPages > 1, $"Expected multi-page report, but got {totalPages} pages");

        string html = builder.ToHtml();
        Assert.Contains($"Page 1 of {totalPages}", html);
        Assert.Contains($"Page {totalPages} of {totalPages}", html);
    }
}
