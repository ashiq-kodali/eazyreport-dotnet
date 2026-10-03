using System.Text;
using EazyReport.Model;
using Xunit;

namespace EazyReport.Tests;

public class BuilderTests
{
    private string GetTestRtpl()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "test_invoice.rtpl");
        if (!File.Exists(path))
        {
            path = Path.Combine(Directory.GetCurrentDirectory(), "Fixtures", "test_invoice.rtpl");
        }
        return File.ReadAllText(path);
    }

    private Dictionary<string, object?> CreateInvoiceData()
    {
        return new Dictionary<string, object?>
        {
            ["invoice"] = new Dictionary<string, object?>
            {
                ["number"] = "INV-2026-9901",
                ["date"] = "2026-10-03",
                ["dueDate"] = "2026-11-03",
                ["isPaid"] = true,
                ["taxRate"] = 0.18,
                ["subtotal"] = 2500.0,
                ["tax"] = 450.0,
                ["total"] = 2950.0
            },
            ["customer"] = new Dictionary<string, object?>
            {
                ["name"] = "Starlight Technologies",
                ["company"] = "Starlight Innovations LLC",
                ["email"] = "accounts@starlight.io",
                ["address"] = "742 Evergreen Terrace, Springfield, OR"
            },
            ["items"] = new List<object?>
            {
                new Dictionary<string, object?>
                {
                    ["description"] = "Cloud Infrastructure Consulting & Setup",
                    ["qty"] = 40,
                    ["price"] = 50.0,
                    ["total"] = 2000.0
                },
                new Dictionary<string, object?>
                {
                    ["description"] = "Enterprise EazyReport License (Annual)",
                    ["qty"] = 1,
                    ["price"] = 500.0,
                    ["total"] = 500.0
                }
            }
        };
    }

    [Fact]
    public void TemplateLoader_LoadsValidLayoutDocument()
    {
        string rtpl = GetTestRtpl();
        var doc = TemplateLoader.FromJson(rtpl);

        Assert.NotNull(doc);
        Assert.Equal("report-designer-layout", doc.Format);
        Assert.Equal(2, doc.Version);
        Assert.NotEmpty(doc.Pages);

        var page = doc.Pages[0];
        Assert.Equal(210.0, page.Width);
        Assert.Equal(297.0, page.Height);
        Assert.NotNull(page.ReportTitle);
        Assert.NotEmpty(page.DataBands);
    }

    [Fact]
    public void ReportBuilder_GeneratesHtmlAndPdf()
    {
        string rtpl = GetTestRtpl();
        var data = CreateInvoiceData();

        var builder = ReportBuilder.FromTemplate(rtpl)
            .Data(data)
            .Param("currency", "USD")
            .Title("Invoice Test");

        int pageCount = builder.PageCount();
        Assert.True(pageCount >= 1, $"Page count should be at least 1, but got {pageCount}");

        // 1. HTML generation
        string html = builder.ToHtml();
        Assert.NotNull(html);
        Assert.Contains("<!DOCTYPE html>", html);
        Assert.Contains("report-sheet", html);
        Assert.Contains("INV-2026-9901", html);
        Assert.Contains("Starlight Technologies", html);

        // 2. Vector PDF generation
        byte[] pdfBytes = builder.ToPdf();
        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 1000, $"PDF bytes length should be > 1000, got {pdfBytes.Length}");

        // Verify PDF Magic Bytes (%PDF)
        string pdfHeader = Encoding.ASCII.GetString(pdfBytes, 0, 4);
        Assert.Equal("%PDF", pdfHeader);
    }
}
