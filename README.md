<p align="center">
  <img src="https://raw.githubusercontent.com/ashiq-kodali/eazyreport/main/assets/eazyreport_banner.png" alt="EazyReport - Modern Banded Reporting & Document Generation for .NET" width="100%">
</p>

<p align="center">
  <h1 align="center">⚡ EazyReport for C# & .NET</h1>
  <p align="center">
    <strong>Ultra-fast banded document and invoice reporting engine for .NET.</strong><br>
    Render <code>.rtpl</code> templates with JSON datasets into native vector PDFs, printable HTML, and high-performance business reports.
  </p>
  <p align="center">
    <a href="https://www.nuget.org/packages/EazyReport"><img src="https://img.shields.io/nuget/v/EazyReport.svg?color=2563eb" alt="NuGet version"></a>
    <a href="https://www.nuget.org/packages/EazyReport"><img src="https://img.shields.io/nuget/dt/EazyReport.svg?color=16a34a" alt="NuGet downloads"></a>
    <a href="https://opensource.org/licenses/MIT"><img src="https://img.shields.io/badge/License-MIT-blue.svg" alt="License: MIT"></a>
    <a href="https://eazyreport.in"><img src="https://img.shields.io/badge/Designer-eazyreport.in-7c3aed.svg" alt="Web Designer"></a>
    <img src="https://img.shields.io/badge/.NET-9.0%20%7C%208.0%20%7C%20Standard%202.0-512bd4.svg" alt="Target Frameworks">
  </p>
</p>

---

## 🚀 Overview

**EazyReport** is an enterprise banded document and invoice generation engine for C# and .NET applications. It loads visual report templates (`.rtpl` JSON) and binds them with your application data to produce publication-grade documents.

- **Direct Vector PDF Rendering**: High-precision vector PDF generation directly in memory (`byte[]`).
- **Printable HTML Document Generation**: Standalone HTML documents equipped with millimeter-precise `@page` print CSS rules.
- **Universal Multiplatform**: Seamlessly supports **.NET 9**, **.NET 8**, **.NET Standard 2.0** (ASP.NET Core, Blazor, .NET MAUI, Windows, macOS, Linux, and Docker containers).

---

## 💼 Real-World Use Cases

| Use Case | Description | Highlights |
| :--- | :--- | :--- |
| 🧾 **1. Enterprise Commercial Invoicing** | B2B Commercial Invoices, Tax Invoices, Receipts, and Proforma Invoices. | Multi-currency billing with repeating line items, automatic tax & discount calculations, sum total aggregates, amount converted to English words (`numberToWords`), dynamic "PAID" status marks, and payment QR codes. |
| 📦 **2. Logistics & Warehouse Manifests** | Packing Slips, Bills of Lading, Dispatch Slips, and Carrier Waybills. | Multi-page shipping manifests with Code 128 carrier tracking barcodes, DataMatrix bin-location tags, item verification checkboxes, and official dispatch watermarks. |
| 📊 **3. Executive Sales & BI Dashboards** | Board Reviews, Monthly Financial Statements, P&L Summaries, and KPIs. | Executive reporting dashboards in landscape mode with Column & Doughnut charts, metric KPI highlight cards, and embedded tables with zebra-striping. |
| 🎓 **4. Academic & Professional Certificates** | Degrees, Diplomas, Awards, Training Accreditations, and Event Passes. | Formal landscape certificates featuring serif typography (`Playfair Display`, `Merriweather`), dual-nested decorative frames, gold seal emblems, instructor signature lines, and online verification QR codes. |

---

## 🎨 Visual Template Designer

Design and test your `.rtpl` templates visually with drag-and-drop millimeter-accurate precision:

👉 **[https://eazyreport.in/](https://eazyreport.in/)**

Export the `.rtpl` file with one click, embed it into your application resources or file system, and render it dynamically with real data.

---

## 📊 Features at a Glance

| Capability | EazyReport for .NET |
| :--- | :--- |
| **Pure Managed .NET** | ✅ 100% C# managed code |
| **Cross-Platform** | ✅ Runs on Linux, macOS, Windows, Docker & WebAssembly |
| **Native Binary PDF (`byte[]`)** | ✅ Fast, vector-crisp, in-memory PDF generation |
| **Banded Pagination & Groups** | ✅ Automatic two-pass pagination, headers, footers & child bands |
| **28+ Barcodes & 2D QR Codes** | ✅ Vector SVG and PDF vector drawing |
| **Vector SVG Charts** | ✅ Pure vector Column, Bar, Line, Area, Pie, Doughnut |
| **Offline & Cloud Ready** | ✅ Works completely offline without external internet calls |

---

## 🛠️ Architecture Workflow

```
 ┌──────────────────────┐      ┌──────────────────────┐
 │  .rtpl Template File │  +   │   JSON / C# Model    │
 │   (Visual Layout)    │      │         Data         │
 └──────────┬───────────┘      └──────────┬───────────┘
            │                             │
            └──────────────┬──────────────┘
                           │
                           ▼
            ┌─────────────────────────────┐
            │   EazyReport .NET Engine    │
            │  Pagination & Layout Engine │
            │ (Multi-pass, Groups, Math)  │
            └──────────────┬──────────────┘
                           │
         ┌─────────────────┴─────────────────┐
         ▼                                   ▼
  ┌──────────────┐                    ┌──────────────┐
  │  Native PDF  │                    │Formatted HTML│
  │   (byte[])   │                    │  (@page CSS  │
  │ for file/API │                    │  for browser/│
  │              │                    │   printing)  │
  └──────────────┘                    └──────────────┘
```

---

## 📦 Installation

Install `EazyReport` via the .NET CLI:

```bash
dotnet add package EazyReport
```

Or via Package Manager Console in Visual Studio:

```powershell
Install-Package EazyReport
```

---

## ⚡ Quick Start

### 1. Generate Vector PDF (`byte[]`) for Web API or Storage

```csharp
using EazyReport;

// 1. Load the .rtpl template
string rtplString = await File.ReadAllTextAsync("templates/invoice.rtpl");

// 2. Prepare your data (Dictionary, JsonElement, or C# POCO)
var invoiceData = new Dictionary<string, object?>
{
    ["invoice"] = new Dictionary<string, object?>
    {
        ["number"] = "INV-2026-88219",
        ["date"] = "2026-10-03",
        ["total"] = 3776.0,
        ["isPaid"] = true
    },
    ["customer"] = new Dictionary<string, object?>
    {
        ["name"] = "Eleanor Vance",
        ["company"] = "Starlight Innovations LLC"
    },
    ["items"] = new List<object?>
    {
        new Dictionary<string, object?>
        {
            ["description"] = "Cloud Architecture Consulting",
            ["qty"] = 40,
            ["price"] = 55.0,
            ["total"] = 2200.0
        },
        new Dictionary<string, object?>
        {
            ["description"] = "Enterprise License",
            ["qty"] = 1,
            ["price"] = 1000.0,
            ["total"] = 1000.0
        }
    }
};

// 3. Build report
var builder = ReportBuilder.FromTemplate(rtplString)
    .Data(invoiceData)
    .Param("currency", "USD")
    .Title("Commercial Invoice - INV-2026-88219");

// 4. Export binary vector PDF bytes
byte[] pdfBytes = builder.ToPdf();
await File.WriteAllBytesAsync("invoice.pdf", pdfBytes);

Console.WriteLine($"Generated invoice.pdf ({pdfBytes.Length:N0} bytes)");
```

---

### 2. ASP.NET Core Controller / Minimal API

Stream PDF documents directly to the client with appropriate HTTP response headers:

```csharp
app.MapGet("/api/invoices/{id}/pdf", async (string id, InvoiceService invoiceService) =>
{
    var invoice = await invoiceService.GetInvoiceAsync(id);
    string rtpl = await File.ReadAllTextAsync("templates/invoice.rtpl");

    var pdfBytes = ReportBuilder.FromTemplate(rtpl)
        .Data(invoice)
        .ToPdf();

    return Results.File(pdfBytes, "application/pdf", $"invoice-{id}.pdf");
});
```

---

### 3. Generate Formatted HTML

Generate standalone, responsive HTML ready for browser preview or `window.print()`:

```csharp
using EazyReport;

string html = ReportBuilder.FromTemplate(rtplString)
    .Data(invoiceData)
    .ToHtml();

await File.WriteAllTextAsync("invoice.html", html);
```

---

### 4. Template Validation & Exact Page Count

```csharp
var builder = ReportBuilder.FromTemplate(rtplString)
    .Data(invoiceData);

// Multi-pass exact page calculation
int totalPages = builder.PageCount();
Console.WriteLine($"Total pages: {totalPages}");
```

---

## 🧮 Handlebars Helpers Reference

| Helper | Syntax Example | Description |
| :--- | :--- | :--- |
| `formatCurrency` | `{{formatCurrency item.price "USD"}}` | Formats number to localized currency string |
| `formatNumber` | `{{formatNumber item.qty 2}}` | Formats number with decimal places and commas |
| `formatDate` | `{{formatDate order.date "dd/MM/yyyy"}}` | Formats ISO date to custom pattern |
| `inWords` | `{{inWords invoice.total}}` | Converts number to cheque currency words |
| `sum` | `{{sum items "price"}}` | Calculates total sum of an array column |
| `avg` | `{{avg items "rating"}}` | Calculates average value of an array column |
| `count` | `{{count items}}` | Counts number of items in array |
| `mul` | `{{mul item.qty item.price}}` | Multiplies numbers |
| `add` / `sub` / `div` | `{{add a b}}`, `{{sub a b}}`, `{{div a b}}` | Basic arithmetic calculations |
| `eq` / `gt` / `lt` | `{{#if (gt item.qty 10)}}...{{/if}}` | Conditional logic evaluations |

---

## 👥 Contributors

- **Ashiq Kodali** ([@ashiq-kodali](https://github.com/ashiq-kodali))
- **Thameem PK**

---

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

© Ashiq Kodali, Thameem PK & EazyReport Contributors.
