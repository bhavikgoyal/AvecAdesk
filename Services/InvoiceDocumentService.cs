using System.Globalization;
using System.Net;
using System.Text;
using AvecADeskApi.Model.Invoice;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AvecADeskApi.Services;

/// <summary>
/// Builds a Tax Invoice PDF/HTML matching the branded invoice template
/// (letterhead, Education Commission particulars, GST, bank details).
/// </summary>
public class InvoiceDocumentService
{
    // Brand colors (match the frontend invoice template)
    private const string CompanyBlue = "#1E56A8";
    private const string LightBlueText = "#1E64C8";
    private const string BoxFill = "#DEEBF8";
    private const string BorderGrey = "#333333";

    private readonly byte[]? _logoBytes;
    private readonly string? _logoBase64;

    static InvoiceDocumentService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public InvoiceDocumentService(IWebHostEnvironment env)
    {
        try
        {
            var webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
            // Using the existing brand logo already stored here:
            var logoPath = Path.Combine(webRoot, "uploads", "vendor-student", "logo-1.png");
            if (File.Exists(logoPath))
            {
                _logoBytes = File.ReadAllBytes(logoPath);
                _logoBase64 = Convert.ToBase64String(_logoBytes);
            }
        }
        catch
        {
            _logoBytes = null;
            _logoBase64 = null;
        }
    }

    public string BuildHtml(
        InvoiceResponse invoice,
        string instituteName,
        IReadOnlyList<MonthlyPaidInstallmentRow> lines,
        DateTime invoiceDate)
    {
        var sb = new StringBuilder();
        sb.Append($$"""
            <!DOCTYPE html><html><head><meta charset="utf-8"/>
            <style>
              body{font-family:Calibri,Arial,sans-serif;color:#111;margin:24px;line-height:1.6}
              .company-name{color:{{CompanyBlue}};font-weight:bold;font-size:14px;margin:0 0 4px}
              .company-info{color:{{LightBlueText}};font-size:11px;line-height:1.5;margin-bottom:24px}
              p{margin:0 0 12px}
            </style></head><body>
            """);

        sb.Append("<div>")
          .Append("<div class='company-name'>AVEC GLOBAL GROUP PTY LTD</div>")
          .Append("<div class='company-info'>ABN Number: 79677235979<br/>")
          .Append("Unit 3, 380 Clayton Road, Clayton, Victoria<br/>")
          .Append("E-mail: account@avec-global.com<br/>")
          .Append("Phone: +61 432 301 842</div>")
          .Append("</div>");
        sb.Append("""
            <p>Dear Team,</p>
            <p>Please find the attached tax invoice for your reference.</p>
            <p>Kindly review the attached document and let us know if any clarification is required.</p>
            <p>Regards,<br/>AVEC Global</p>
            </body></html>
            """);

        return sb.ToString();
    }

    public byte[] BuildPdfBytes(
        InvoiceResponse invoice,
        string instituteName,
        IReadOnlyList<MonthlyPaidInstallmentRow> lines,
        DateTime invoiceDate)
    {
        var gstPct = lines.FirstOrDefault()?.GSTPercentage ?? 10m;
        var commissionTotal = lines.Sum(x => x.CommissionAmount + x.BonusAmount);
        var gstTotal = lines.Sum(x => x.GSTAmount);
        var grandTotal = lines.Sum(x => x.InvoiceAmount);
        var inWords = AmountToWords(grandTotal);
        var dateText = invoiceDate.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(x => x.FontSize(11).FontColor(Colors.Black));

                page.Content().Column(col =>
                {
                    col.Spacing(10);

                    // --- Letterhead: logo + company details ---
                    col.Item().Row(row =>
                    {
                        if (_logoBytes is not null)
                        {
                            row.ConstantItem(70).Image(_logoBytes).FitWidth();
                        }
                        row.RelativeItem().PaddingLeft(_logoBytes is not null ? 10 : 0).Column(company =>
                        {
                            company.Item().Text("AVEC GLOBAL GROUP PTY LTD")
                                .Bold().FontSize(13).FontColor(CompanyBlue);
                            company.Item().Text("ABN Number: 79677235979").FontSize(9).FontColor(LightBlueText);
                            company.Item().Text("Unit 3, 380 Clayton Road, Clayton, Victoria").FontSize(9).FontColor(LightBlueText);
                            company.Item().Text("E-mail: account@avec-global.com").FontSize(9).FontColor(LightBlueText);
                            company.Item().Text("Phone: +61 432 301 842").FontSize(9).FontColor(LightBlueText);
                        });
                    });

                    col.Item().PaddingTop(6).AlignCenter()
                        .Text("TAX INVOICE").Bold().FontSize(18).LetterSpacing(0.5f);

                    // --- To / Date / Invoice No box ---
                    col.Item()
                        .Border(1).BorderColor(Colors.Black)
                        .Background(BoxFill)
                        .Padding(10)
                        .Row(row =>
                        {
                            row.RelativeItem(3).Column(left =>
                            {
                                left.Item().Text("To,").Bold();
                                left.Item().Text(instituteName);
                            });
                            row.RelativeItem(2).AlignRight().Column(right =>
                            {
                                right.Item().Text(t =>
                                {
                                    t.Span("Date: ").Bold();
                                    t.Span(dateText);
                                });
                                right.Item().Text(t =>
                                {
                                    t.Span("Invoice No: ").Bold();
                                    t.Span(invoice.InvoiceNumber);
                                });
                            });
                        });

                    // --- Items table ---
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(50);
                            columns.RelativeColumn();
                            columns.ConstantColumn(110);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCell).Text("Sr. No");
                            header.Cell().Element(HeaderCell).Text("PARTICULARS");
                            header.Cell().Element(HeaderCell).AlignRight().Text("AMOUNT (AUD$)");
                        });

                        var sr = 1;
                        foreach (var line in lines)
                        {
                            table.Cell().Element(BodyCell).Text($"{sr++}.");
                            table.Cell().Element(BodyCell).Column(c =>
                            {
                                c.Item().Text("Education Commission").Bold();
                                c.Item().Text($"Student Name: {line.FullName}");
                                c.Item().Text($"Student Id: {line.StudentCode}");
                                c.Item().Text($"Course: {line.CourseName}");
                                if (!string.IsNullOrWhiteSpace(line.Campus))
                                    c.Item().Text($"Campus: {line.Campus}");
                                c.Item().Text($"Fees: {FormatMoney(line.FeesAmount)}");
                            });
                            table.Cell().Element(BodyCell).AlignRight().Text(FormatMoney(line.InvoiceAmount));
                        }
                    });

                    // --- Totals (bordered grid, matches template) ---
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(50);
                            columns.RelativeColumn();
                            columns.ConstantColumn(110);
                        });

                        table.Cell().Element(TotalsCell).Text("");
                        table.Cell().Element(TotalsCell).AlignRight().Text("TOTAL").Bold();
                        table.Cell().Element(TotalsCell).AlignRight().Text($"AUD {FormatMoney(commissionTotal)}").Bold();

                        table.Cell().Element(TotalsCell).Text("");
                        table.Cell().Element(TotalsCell).AlignRight()
                            .Text($"GST {gstPct.ToString("0.##", CultureInfo.InvariantCulture)}%");
                        table.Cell().Element(TotalsCell).AlignRight().Text($"AUD {FormatMoney(gstTotal)}");

                        table.Cell().Element(TotalsCell).Text("");
                        table.Cell().Element(TotalsCell).AlignRight().Text("TOTAL").Bold();
                        table.Cell().Element(TotalsCell).AlignRight().Text($"AUD {FormatMoney(grandTotal)}").Bold();

                        table.Cell().ColumnSpan(2).Element(TotalsCell).Text(t =>
                        {
                            t.Span("In Words: ").Bold();
                            t.Span($"{inWords} Only");
                        });
                        table.Cell().Element(TotalsCell).Text("");
                    });

                    // --- Bank details ---
                    col.Item().PaddingTop(16).Column(bank =>
                    {
                        bank.Spacing(2);
                        bank.Item().Text("Bank Details").Bold();
                        bank.Item().Text("Account Name: AVEC GLOBAL GROUP PTY LTD");
                        bank.Item().Text("BSB: 063-549");
                        bank.Item().Text("Account Number: 1081 0692");
                        bank.Item().Text("Address: Unit 3, 380 Clayton Road, Clayton, Vic: 3168");
                    });
                });
            });
        }).GeneratePdf();
    }

    private static IContainer HeaderCell(IContainer container) =>
        container
            .Border(1)
            .BorderColor(Colors.Grey.Darken2)
            .Background(Colors.Grey.Lighten3)
            .Padding(6);

    private static IContainer BodyCell(IContainer container) =>
        container
            .Border(1)
            .BorderColor(Colors.Grey.Darken2)
            .Padding(6);

    private static IContainer TotalsCell(IContainer container) =>
        container
            .Border(1)
            .BorderColor(Colors.Grey.Darken2)
            .Padding(6);

    private static string FormatMoney(decimal value)
        => value.ToString("#,##0.00", CultureInfo.InvariantCulture);

    private static string AmountToWords(decimal amount)
    {
        var whole = (long)Math.Floor(Math.Abs(amount));
        var cents = (int)Math.Round((Math.Abs(amount) - whole) * 100);
        var words = $"{NumberToWords(whole)} Dollars";
        if (cents > 0)
            words += $" and {NumberToWords(cents)} Cents";
        return words;
    }

    private static string NumberToWords(long number)
    {
        if (number == 0) return "Zero";
        if (number < 0) return "Minus " + NumberToWords(Math.Abs(number));

        string[] units =
        [
            "", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine",
            "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen",
            "Seventeen", "Eighteen", "Nineteen"
        ];
        string[] tens = ["", "", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety"];

        var words = "";
        if (number / 1_000_000 > 0)
        {
            words += NumberToWords(number / 1_000_000) + " Million ";
            number %= 1_000_000;
        }
        if (number / 1000 > 0)
        {
            words += NumberToWords(number / 1000) + " Thousand ";
            number %= 1000;
        }
        if (number / 100 > 0)
        {
            words += NumberToWords(number / 100) + " Hundred ";
            number %= 100;
        }
        if (number > 0)
        {
            if (words != "") words += "and ";
            if (number < 20) words += units[number];
            else
            {
                words += tens[number / 10];
                if (number % 10 > 0) words += "-" + units[number % 10];
            }
        }

        return words.Trim();
    }
}