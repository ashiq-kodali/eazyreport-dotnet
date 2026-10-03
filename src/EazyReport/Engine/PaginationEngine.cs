using System.Collections;
using System.Text;
using System.Text.Json;
using EazyReport.Expressions;
using EazyReport.Model;

namespace EazyReport.Engine;

public class RenderedPage
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 8);
    public double Width { get; set; } = 210.0;
    public double Height { get; set; } = 297.0;
    public string Html { get; set; } = "";
    public List<PlacedBandRecord> PlacedBands { get; set; } = new();
    public Watermark Watermark { get; set; } = new();
    public PageMargins Margins { get; set; } = new();
}

public class RenderOptions
{
    public Dictionary<string, object?>? Params { get; set; }
    public bool ApplyOffset { get; set; } = true;
    public int MaxPages { get; set; } = 2000;
}

public static class PaginationEngine
{
    public static List<RenderedPage> Paginate(
        LayoutDocument doc,
        object? data,
        Dictionary<string, object?>? parameters = null,
        RenderOptions? options = null)
    {
        options ??= new RenderOptions();
        var allParams = new Dictionary<string, object?>(doc.Parameters ?? new Dictionary<string, object?>());
        if (parameters != null)
        {
            foreach (var kvp in parameters) allParams[kvp.Key] = kvp.Value;
        }
        if (options.Params != null)
        {
            foreach (var kvp in options.Params) allParams[kvp.Key] = kvp.Value;
        }

        // Pass 1: compute page count
        var pass1 = ExecutePaginationPass(doc, data, allParams, 1, options.MaxPages);
        int totalPages = Math.Max(1, pass1.Count);

        // Pass 2: layout with accurate total pages
        var pass2 = ExecutePaginationPass(doc, data, allParams, totalPages, options.MaxPages);
        return pass2;
    }

    private static List<RenderedPage> ExecutePaginationPass(
        LayoutDocument doc,
        object? data,
        Dictionary<string, object?> parameters,
        int totalPages,
        int maxPages)
    {
        var pages = new List<RenderedPage>();
        if (doc.Pages.Count == 0) return pages;

        var pageTpl = doc.Pages[0];
        double pw = pageTpl.Width;
        double ph = pageTpl.Height;
        var margins = pageTpl.Margins;
        double bodyW = pw - margins.Left - margins.Right;
        double bodyH = ph - margins.Top - margins.Bottom;

        double footerH = pageTpl.PageFooter != null ? pageTpl.PageFooter.Height : 0.0;
        double maxBodyY = margins.Top + bodyH - footerH;

        int pageNum = 1;
        double currentY = margins.Top;
        var currentPlacedBands = new List<PlacedBandRecord>();
        var currentBandHtml = new StringBuilder();

        void FlushPage()
        {
            // Render PageFooter at bottom
            if (pageTpl.PageFooter != null)
            {
                var footerRc = new RenderCtx
                {
                    Data = data,
                    Params = parameters,
                    PageNumber = pageNum,
                    TotalPages = totalPages
                };
                double fy = ph - margins.Bottom - pageTpl.PageFooter.Height;
                var laidFooter = BandLayout.Layout(pageTpl.PageFooter, footerRc, bodyW);
                if (laidFooter != null)
                {
                    currentPlacedBands.Add(new PlacedBandRecord
                    {
                        Band = pageTpl.PageFooter,
                        X = margins.Left,
                        Y = fy,
                        Width = bodyW,
                        Height = laidFooter.Height,
                        Fill = pageTpl.PageFooter.Fill,
                        Elements = laidFooter.Elements
                    });
                    currentBandHtml.Append($"<div style=\"position:absolute;left:{margins.Left:F2}mm;top:{fy:F2}mm;width:{bodyW:F2}mm;height:{laidFooter.Height:F2}mm;\">{laidFooter.Html}</div>");
                }
            }

            pages.Add(new RenderedPage
            {
                Id = $"page-{pageNum}",
                Width = pw,
                Height = ph,
                Html = currentBandHtml.ToString(),
                PlacedBands = new List<PlacedBandRecord>(currentPlacedBands),
                Watermark = pageTpl.Watermark,
                Margins = margins
            });

            currentPlacedBands.Clear();
            currentBandHtml.Clear();
            pageNum++;
            currentY = margins.Top;
        }

        void StartNewPage()
        {
            FlushPage();

            // Render PageHeader on new page
            if (pageTpl.PageHeader != null)
            {
                var headerRc = new RenderCtx
                {
                    Data = data,
                    Params = parameters,
                    PageNumber = pageNum,
                    TotalPages = totalPages
                };
                var laidHeader = BandLayout.Layout(pageTpl.PageHeader, headerRc, bodyW);
                if (laidHeader != null)
                {
                    currentPlacedBands.Add(new PlacedBandRecord
                    {
                        Band = pageTpl.PageHeader,
                        X = margins.Left,
                        Y = currentY,
                        Width = bodyW,
                        Height = laidHeader.Height,
                        Fill = pageTpl.PageHeader.Fill,
                        Elements = laidHeader.Elements
                    });
                    currentBandHtml.Append($"<div style=\"position:absolute;left:{margins.Left:F2}mm;top:{currentY:F2}mm;width:{bodyW:F2}mm;height:{laidHeader.Height:F2}mm;\">{laidHeader.Html}</div>");
                    currentY += laidHeader.Height;
                }
            }
        }

        // 1. ReportTitle (on first page)
        if (pageTpl.ReportTitle != null && pageTpl.ReportTitle.PrintOnFirstPage)
        {
            var titleRc = new RenderCtx
            {
                Data = data,
                Params = parameters,
                PageNumber = pageNum,
                TotalPages = totalPages
            };
            var laidTitle = BandLayout.Layout(pageTpl.ReportTitle, titleRc, bodyW);
            if (laidTitle != null)
            {
                currentPlacedBands.Add(new PlacedBandRecord
                {
                    Band = pageTpl.ReportTitle,
                    X = margins.Left,
                    Y = currentY,
                    Width = bodyW,
                    Height = laidTitle.Height,
                    Fill = pageTpl.ReportTitle.Fill,
                    Elements = laidTitle.Elements
                });
                currentBandHtml.Append($"<div style=\"position:absolute;left:{margins.Left:F2}mm;top:{currentY:F2}mm;width:{bodyW:F2}mm;height:{laidTitle.Height:F2}mm;\">{laidTitle.Html}</div>");
                currentY += laidTitle.Height;
            }
        }

        // 2. PageHeader (if first page and not skipped)
        if (pageTpl.PageHeader != null && pageTpl.PageHeader.PrintOnFirstPage)
        {
            var headerRc = new RenderCtx
            {
                Data = data,
                Params = parameters,
                PageNumber = pageNum,
                TotalPages = totalPages
            };
            var laidHeader = BandLayout.Layout(pageTpl.PageHeader, headerRc, bodyW);
            if (laidHeader != null)
            {
                currentPlacedBands.Add(new PlacedBandRecord
                {
                    Band = pageTpl.PageHeader,
                    X = margins.Left,
                    Y = currentY,
                    Width = bodyW,
                    Height = laidHeader.Height,
                    Fill = pageTpl.PageHeader.Fill,
                    Elements = laidHeader.Elements
                });
                currentBandHtml.Append($"<div style=\"position:absolute;left:{margins.Left:F2}mm;top:{currentY:F2}mm;width:{bodyW:F2}mm;height:{laidHeader.Height:F2}mm;\">{laidHeader.Html}</div>");
                currentY += laidHeader.Height;
            }
        }

        // 3. Process DataBands
        foreach (var dataBand in pageTpl.DataBands)
        {
            var records = ResolveRecords(dataBand, data);

            // DataHeader
            if (dataBand.Header != null)
            {
                var dhRc = new RenderCtx
                {
                    Data = data,
                    Params = parameters,
                    PageNumber = pageNum,
                    TotalPages = totalPages
                };
                var laidDh = BandLayout.Layout(dataBand.Header, dhRc, bodyW);
                if (laidDh != null)
                {
                    if (currentY + laidDh.Height > maxBodyY)
                    {
                        StartNewPage();
                    }

                    currentPlacedBands.Add(new PlacedBandRecord
                    {
                        Band = dataBand.Header,
                        X = margins.Left,
                        Y = currentY,
                        Width = bodyW,
                        Height = laidDh.Height,
                        Fill = dataBand.Header.Fill,
                        Elements = laidDh.Elements
                    });
                    currentBandHtml.Append($"<div style=\"position:absolute;left:{margins.Left:F2}mm;top:{currentY:F2}mm;width:{bodyW:F2}mm;height:{laidDh.Height:F2}mm;\">{laidDh.Html}</div>");
                    currentY += laidDh.Height;
                }
            }

            // Records
            foreach (var item in records)
            {
                var itemRc = new RenderCtx
                {
                    Data = data,
                    Item = item,
                    Params = parameters,
                    PageNumber = pageNum,
                    TotalPages = totalPages
                };

                var laidItem = BandLayout.Layout(dataBand, itemRc, bodyW);
                if (laidItem != null)
                {
                    if (currentY + laidItem.Height > maxBodyY)
                    {
                        StartNewPage();
                    }

                    currentPlacedBands.Add(new PlacedBandRecord
                    {
                        Band = dataBand,
                        X = margins.Left,
                        Y = currentY,
                        Width = bodyW,
                        Height = laidItem.Height,
                        Fill = dataBand.Fill,
                        Elements = laidItem.Elements
                    });
                    currentBandHtml.Append($"<div style=\"position:absolute;left:{margins.Left:F2}mm;top:{currentY:F2}mm;width:{bodyW:F2}mm;height:{laidItem.Height:F2}mm;\">{laidItem.Html}</div>");
                    currentY += laidItem.Height;
                }

                // ChildBands (non-filling)
                foreach (var cb in dataBand.ChildBands.Where(c => !c.FillUnusedSpace))
                {
                    var laidCb = BandLayout.Layout(cb, itemRc, bodyW);
                    if (laidCb != null)
                    {
                        if (currentY + laidCb.Height > maxBodyY)
                        {
                            StartNewPage();
                        }

                        currentPlacedBands.Add(new PlacedBandRecord
                        {
                            Band = cb,
                            X = margins.Left,
                            Y = currentY,
                            Width = bodyW,
                            Height = laidCb.Height,
                            Fill = cb.Fill,
                            Elements = laidCb.Elements
                        });
                        currentBandHtml.Append($"<div style=\"position:absolute;left:{margins.Left:F2}mm;top:{currentY:F2}mm;width:{bodyW:F2}mm;height:{laidCb.Height:F2}mm;\">{laidCb.Html}</div>");
                        currentY += laidCb.Height;
                    }
                }
            }

            // FillUnusedSpace ChildBands (repeat rows until footer)
            var fillChild = dataBand.ChildBands.FirstOrDefault(c => c.FillUnusedSpace);
            if (fillChild != null && fillChild.Height > 0)
            {
                var blankRc = new RenderCtx
                {
                    Data = data,
                    Params = parameters,
                    PageNumber = pageNum,
                    TotalPages = totalPages
                };

                while (currentY + fillChild.Height <= maxBodyY)
                {
                    var laidFill = BandLayout.Layout(fillChild, blankRc, bodyW);
                    if (laidFill == null) break;

                    currentPlacedBands.Add(new PlacedBandRecord
                    {
                        Band = fillChild,
                        X = margins.Left,
                        Y = currentY,
                        Width = bodyW,
                        Height = laidFill.Height,
                        Fill = fillChild.Fill,
                        Elements = laidFill.Elements
                    });
                    currentBandHtml.Append($"<div style=\"position:absolute;left:{margins.Left:F2}mm;top:{currentY:F2}mm;width:{bodyW:F2}mm;height:{laidFill.Height:F2}mm;\">{laidFill.Html}</div>");
                    currentY += laidFill.Height;
                }
            }

            // DataFooter
            if (dataBand.Footer != null)
            {
                var dfRc = new RenderCtx
                {
                    Data = data,
                    Params = parameters,
                    PageNumber = pageNum,
                    TotalPages = totalPages
                };
                var laidDf = BandLayout.Layout(dataBand.Footer, dfRc, bodyW);
                if (laidDf != null)
                {
                    if (currentY + laidDf.Height > maxBodyY)
                    {
                        StartNewPage();
                    }

                    currentPlacedBands.Add(new PlacedBandRecord
                    {
                        Band = dataBand.Footer,
                        X = margins.Left,
                        Y = currentY,
                        Width = bodyW,
                        Height = laidDf.Height,
                        Fill = dataBand.Footer.Fill,
                        Elements = laidDf.Elements
                    });
                    currentBandHtml.Append($"<div style=\"position:absolute;left:{margins.Left:F2}mm;top:{currentY:F2}mm;width:{bodyW:F2}mm;height:{laidDf.Height:F2}mm;\">{laidDf.Html}</div>");
                    currentY += laidDf.Height;
                }
            }
        }

        // 4. ReportSummary
        if (pageTpl.ReportSummary != null)
        {
            var sumRc = new RenderCtx
            {
                Data = data,
                Params = parameters,
                PageNumber = pageNum,
                TotalPages = totalPages
            };
            var laidSum = BandLayout.Layout(pageTpl.ReportSummary, sumRc, bodyW);
            if (laidSum != null)
            {
                if (currentY + laidSum.Height > maxBodyY)
                {
                    StartNewPage();
                }

                currentPlacedBands.Add(new PlacedBandRecord
                {
                    Band = pageTpl.ReportSummary,
                    X = margins.Left,
                    Y = currentY,
                    Width = bodyW,
                    Height = laidSum.Height,
                    Fill = pageTpl.ReportSummary.Fill,
                    Elements = laidSum.Elements
                });
                currentBandHtml.Append($"<div style=\"position:absolute;left:{margins.Left:F2}mm;top:{currentY:F2}mm;width:{bodyW:F2}mm;height:{laidSum.Height:F2}mm;\">{laidSum.Html}</div>");
                currentY += laidSum.Height;
            }
        }

        // Flush final page
        FlushPage();

        return pages;
    }

    private static List<object?> ResolveRecords(DataBand band, object? data)
    {
        var res = new List<object?>();
        if (data == null) return res;

        string path = !string.IsNullOrEmpty(band.DataPath) ? band.DataPath : band.Dataset;
        object? listObj = !string.IsNullOrEmpty(path) ? ExpressionEngine.GetPath(data, path) : data;

        if (listObj is JsonElement je && je.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in je.EnumerateArray()) res.Add(item);
            return res;
        }

        if (listObj is IEnumerable ie && !(listObj is string))
        {
            foreach (var item in ie) res.Add(item);
            return res;
        }

        if (listObj != null)
        {
            res.Add(listObj);
        }

        return res;
    }
}
