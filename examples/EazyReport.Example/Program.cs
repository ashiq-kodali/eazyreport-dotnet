using System.Text.Json;
using EazyReport;

Console.WriteLine("=================================================");
Console.WriteLine("⚡ EazyReport for .NET — Document Generation Demo");
Console.WriteLine("=================================================");

string templatePath = Path.Combine(AppContext.BaseDirectory, "test_invoice.rtpl");
if (!File.Exists(templatePath))
{
    templatePath = "test_invoice.rtpl";
}

Console.WriteLine($"Loading template: {templatePath}...");
string rtplString = File.ReadAllText(templatePath);

// Sample Invoice Dataset
var invoiceData = new Dictionary<string, object?>
{
    ["invoice"] = new Dictionary<string, object?>
    {
        ["number"] = "INV-2026-88219",
        ["date"] = "2026-10-03",
        ["dueDate"] = "2026-11-03",
        ["currency"] = "USD",
        ["taxRate"] = 0.18,
        ["subtotal"] = 3200.0,
        ["tax"] = 576.0,
        ["total"] = 3776.0,
        ["isPaid"] = true,
        ["paymentMethod"] = "Corporate ACH / Wire Transfer"
    },
    ["sender"] = new Dictionary<string, object?>
    {
        ["name"] = "EazyReport Cloud Services Ltd",
        ["taxId"] = "VAT-EU-98218731",
        ["email"] = "billing@eazyreport.in",
        ["phone"] = "+1 (800) 555-0199",
        ["address"] = "100 Innovation Way, Suite 400, Austin, TX"
    },
    ["customer"] = new Dictionary<string, object?>
    {
        ["name"] = "Eleanor Vance",
        ["company"] = "Starlight Innovations LLC",
        ["taxId"] = "US-EIN-12-3456789",
        ["email"] = "accounts@starlight.io",
        ["address"] = "742 Evergreen Terrace, Springfield, OR"
    },
    ["items"] = new List<object?>
    {
        new Dictionary<string, object?>
        {
            ["sku"] = "SRV-ARCH-01",
            ["description"] = "Cloud Architecture & Scalability Consulting",
            ["qty"] = 40,
            ["price"] = 55.0,
            ["tax"] = 396.0,
            ["total"] = 2200.0
        },
        new Dictionary<string, object?>
        {
            ["sku"] = "LIC-ENT-2026",
            ["description"] = "EazyReport Enterprise Annual Cluster License",
            ["qty"] = 1,
            ["price"] = 1000.0,
            ["tax"] = 180.0,
            ["total"] = 1000.0
        }
    }
};

// 1. Build the report
var builder = ReportBuilder.FromTemplate(rtplString)
    .Data(invoiceData)
    .Param("currency", "USD")
    .Title("Commercial Invoice - INV-2026-88219");

Console.WriteLine($"\nCalculating pagination...");
int totalPages = builder.PageCount();
Console.WriteLine($"✓ Generated {totalPages} page(s)");

// 2. Export HTML
string htmlPath = Path.Combine(Directory.GetCurrentDirectory(), "output_invoice.html");
builder.SaveHtml(htmlPath);
Console.WriteLine($"✓ Saved HTML Document: {htmlPath} ({new FileInfo(htmlPath).Length:N0} bytes)");

// 3. Export PDF
string pdfPath = Path.Combine(Directory.GetCurrentDirectory(), "output_invoice.pdf");
builder.SavePdf(pdfPath);
Console.WriteLine($"✓ Saved Vector PDF Document: {pdfPath} ({new FileInfo(pdfPath).Length:N0} bytes)");

Console.WriteLine("\n✨ EazyReport .NET generation completed successfully!");
