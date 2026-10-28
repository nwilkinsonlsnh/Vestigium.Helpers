using System.Text;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Vestigium.Helpers.Pdf;

internal static class PdfPages
{
    private const string Ink = "#1B3A4B";
    private const string Rule = "#D5DEE3";
    private const string Paper = "#F4F7F8";

    public static byte[] RenderBody(PdfDocument document)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        return Document.Create(container => container.Page(page => Body(page, document))).GeneratePdf();
    }

    public static byte[] Render(PdfDocument document, string bodyHash, string seal)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        return Document.Create(container =>
        {
            container.Page(page => Body(page, document));
            if (document.Seal)
                container.Page(page => Trailer(page, document, bodyHash, seal));
        }).WithMetadata(new DocumentMetadata
        {
            Title = document.Title,
            Author = document.Identity.User,
            Subject = document.Subtitle,
            Creator = "Vestigium.Helpers.Pdf " + PdfEvidence.Version,
            Keywords = document.Seal
                ? "body=" + bodyHash + ";seal=" + seal + ";canon=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(document.Identity.Canonical()))
                : ""
        }).GeneratePdf();
    }

    public static byte[] RenderBody(PdfReport report) => RenderBody(From(report));

    public static byte[] Render(PdfReport report, string bodyHash, string seal)
        => Render(From(report), bodyHash, seal);

    private static void Body(PageDescriptor page, PdfDocument document)
    {
        Frame(page, document);
        page.Content().Padding(24).Column(col =>
        {
            col.Spacing(6);
            foreach (var section in document.Sections)
            {
                Heading(col, section.Heading);
                if (section.Fields is { Count: > 0 })
                    Fields(col, section.Fields);
                if (section.Table is { } table)
                    Table(col, table);
                col.Item().PaddingTop(10);
            }
        });
    }

    private static void Frame(PageDescriptor page, PdfDocument document)
    {
        page.Size(PageSizes.A4);
        page.Margin(0);
        page.DefaultTextStyle(style => style.FontSize(9).FontColor(Ink));
        page.Header().Background(document.Banner).Padding(18).Row(row =>
        {
            row.RelativeItem().Text(document.Title).FontSize(18).SemiBold().FontColor(document.BannerText);
            row.ConstantItem(120).AlignRight().AlignMiddle().Text(document.Subtitle).FontColor(document.BannerText);
        });
        page.Footer().Background(Paper).Padding(10).Row(row =>
        {
            row.RelativeItem().Text("Vestigium evidence").FontSize(8).FontColor(Colors.Grey.Darken1);
            row.ConstantItem(80).AlignRight().Text(text =>
            {
                text.DefaultTextStyle(style => style.FontSize(8).FontColor(Colors.Grey.Darken1));
                text.Span("Page ");
                text.CurrentPageNumber();
            });
        });
    }

    private static void Fields(ColumnDescriptor col, IReadOnlyList<PdfField> fields)
    {
        col.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(110);
                columns.RelativeColumn();
            });
            foreach (var field in fields)
                Line(table, field.Name, field.Value);
        });
    }

    private static void Table(ColumnDescriptor col, PdfTable data)
    {
        col.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                foreach (var _ in data.Columns)
                    columns.RelativeColumn();
            });
            table.Header(header =>
            {
                foreach (var column in data.Columns)
                    header.Cell().Background(Paper).Padding(4).Text(column).SemiBold();
            });
            foreach (var row in data.Rows)
                for (var i = 0; i < data.Columns.Count; i++)
                    table.Cell().BorderBottom(1).BorderColor(Rule).Padding(3).Text(i < row.Length ? row[i] : "");
        });
    }

    private static PdfDocument From(PdfReport report)
    {
        var document = new PdfDocument
        {
            Title = "Vestigium (FXplorerIQ)",
            Subtitle = "Summary",
            Seal = true,
            Identity = report.Identity
        };
        document.AddFields("Summary",
            new PdfField { Name = "Job", Value = report.JobId },
            new PdfField { Name = "Verb", Value = report.Verb },
            new PdfField { Name = "Source", Value = report.Source },
            new PdfField { Name = "Destination", Value = report.Destination },
            new PdfField { Name = "Algorithm", Value = report.Algorithm },
            new PdfField { Name = "Matched", Value = report.Matched.ToString() },
            new PdfField { Name = "Failed", Value = report.Failed.ToString() },
            new PdfField { Name = "Result", Value = report.Failed == 0 ? "Passed" : "Failed" });
        var failed = report.Rows.Where(row => !row.Equal).Select(Cells).ToArray();
        var matched = report.Rows.Where(row => row.Equal).Select(Cells).ToArray();
        if (failed.Length > 0)
            document.AddTable("Failures  (" + failed.Length + ")", ["File", "Result", "Source digest"], failed);
        document.AddTable("Matches  (" + matched.Length + ")", ["File", "Result", "Source digest"], matched);
        return document;
    }

    private static string[] Cells(PdfRow row)
        => [row.Relative, row.Equal ? "Match" : "Fail", Short(row.SourceDigest)];

    private static void Body(PageDescriptor page, PdfReport report) => Body(page, From(report));

    private static void Files(ColumnDescriptor col, PdfRow[] rows)
    {
        col.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(4);
                columns.ConstantColumn(60);
                columns.RelativeColumn(3);
            });
            table.Header(header =>
            {
                header.Cell().Background(Paper).Padding(4).Text("File").SemiBold();
                header.Cell().Background(Paper).Padding(4).Text("Result").SemiBold();
                header.Cell().Background(Paper).Padding(4).Text("Source digest").SemiBold();
            });
            foreach (var row in rows)
            {
                table.Cell().BorderBottom(1).BorderColor(Rule).Padding(3).Text(row.Relative);
                table.Cell().BorderBottom(1).BorderColor(Rule).Padding(3).Text(row.Equal ? "Match" : "Fail")
                    .FontColor(row.Equal ? "#2E7D4F" : "#B42318");
                table.Cell().BorderBottom(1).BorderColor(Rule).Padding(3).Text(Short(row.SourceDigest));
            }
        });
    }

    private static void Trailer(PageDescriptor page, PdfDocument document, string bodyHash, string seal)
    {
        var id = document.Identity;
        Frame(page, document);
        page.Content().Padding(24).Column(col =>
        {
            col.Spacing(4);
            Heading(col, "Trailer");
            col.Item().Text("This trailer is sealed with this report so a later check can tell whether the file still matches what was sealed. It does not mean an edit was found. It is not a certificate signature, and it does not prove who made the file.")
                .FontSize(9);
            col.Item().PaddingTop(10).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(110);
                    columns.RelativeColumn();
                });
                Line(table, "Format", "1");
                Line(table, "User", id.User);
                Line(table, "SID", id.Sid ?? "");
                Line(table, "Machine", id.Machine);
                Line(table, "Domain", id.Domain ?? "");
                Line(table, "UTC", id.Utc.ToString("o"));
                Line(table, "Version", id.Version);
                var summary = document.Sections.FirstOrDefault(section => section.Fields is not null);
                if (summary?.Fields is not null)
                    foreach (var field in summary.Fields)
                        Line(table, field.Name, field.Value);
            });
            col.Item().PaddingTop(12);
            Heading(col, "Adapters");
            foreach (var adapter in id.Adapters)
                col.Item().Text(adapter.Name + "    " + adapter.Address + "    " + adapter.Mac);
            col.Item().PaddingTop(12);
            Heading(col, "Seal");
            col.Item().Text("Body").SemiBold();
            col.Item().Text(bodyHash).FontSize(8);
            col.Item().PaddingTop(4).Text("Seal").SemiBold();
            col.Item().Text(seal).FontSize(8);
        });
    }

    private static void Heading(ColumnDescriptor col, string text)
    {
        col.Item().Text(text).FontSize(13).SemiBold().FontColor(Ink);
        col.Item().PaddingBottom(4).LineHorizontal(1).LineColor(Ink);
    }

    private static void Line(TableDescriptor table, string label, string value)
    {
        table.Cell().BorderBottom(1).BorderColor(Rule).Padding(3).Text(label).SemiBold();
        table.Cell().BorderBottom(1).BorderColor(Rule).Padding(3).Text(value);
    }

    private static string Short(string? digest)
        => string.IsNullOrEmpty(digest) || digest.Length <= 24 ? digest ?? "" : digest[..24] + "…";
}
