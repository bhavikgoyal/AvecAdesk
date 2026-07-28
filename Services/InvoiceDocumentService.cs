using System.Globalization;
using System.Net;
using System.Text;
using AvecADeskApi.Model.Invoice;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AvecADeskApi.Services;

/// <summary>
/// Builds a Tax Invoice PDF matching the Invoice.docx layout
/// (Education Commission, student particulars, GST, bank details).
/// </summary>
public class InvoiceDocumentService
{
    static InvoiceDocumentService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public string BuildHtml(
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

        var sb = new StringBuilder();
        sb.Append("""
            <!DOCTYPE html><html><head><meta charset="utf-8"/>
            <style>
              body{font-family:Calibri,Arial,sans-serif;color:#111;margin:24px}
              h1{text-align:center;letter-spacing:1px;margin:0 0 18px}
              .meta{width:100%;margin-bottom:16px}
              .meta td{vertical-align:top;padding:4px 0}
              table.items{width:100%;border-collapse:collapse;margin-top:8px}
              table.items th,table.items td{border:1px solid #333;padding:8px;vertical-align:top}
              table.items th{background:#f3f3f3;text-align:left}
              .amt{text-align:right;white-space:nowrap}
              .totals{width:100%;margin-top:12px}
              .totals td{padding:4px 0}
              .bank{margin-top:28px;font-size:13px;line-height:1.45}
            </style></head><body>
            """);
        sb.Append("<h1>TAX INVOICE</h1>");
        sb.Append("<table class='meta'><tr>");
        sb.Append("<td style='width:60%'><strong>To,</strong><br/>")
          .Append(WebUtility.HtmlEncode(instituteName))
          .Append("</td>");
        sb.Append("<td style='width:40%'><strong>Date:</strong> ")
          .Append(invoiceDate.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture))
          .Append("<br/><strong>Invoice No:</strong> ")
          .Append(WebUtility.HtmlEncode(invoice.InvoiceNumber))
          .Append("</td></tr></table>");

        sb.Append("""
            <table class="items">
              <thead><tr><th style="width:8%">Sr. No</th><th>PARTICULARS</th><th style="width:18%">AMOUNT (AUD$)</th></tr></thead>
              <tbody>
            """);

        var sr = 1;
        foreach (var line in lines)
        {
            sb.Append("<tr><td>").Append(sr++).Append(".</td><td>");
            sb.Append("<strong>Education Commission</strong><br/>");
            sb.Append("Student Name: ").Append(WebUtility.HtmlEncode(line.FullName)).Append("<br/>");
            sb.Append("Student Id: ").Append(WebUtility.HtmlEncode(line.StudentCode)).Append("<br/>");
            sb.Append("Course: ").Append(WebUtility.HtmlEncode(line.CourseName)).Append("<br/>");
            sb.Append("Fees: ").Append(FormatMoney(line.FeesAmount));
            sb.Append("</td><td class='amt'>").Append(FormatMoney(line.InvoiceAmount)).Append("</td></tr>");
        }

        sb.Append("</tbody></table>");

        sb.Append("<table class='totals'>");
        sb.Append("<tr><td><strong>TOTAL</strong></td><td class='amt'><strong>AUD ")
          .Append(FormatMoney(commissionTotal)).Append("</strong></td></tr>");
        sb.Append("<tr><td>GST ").Append(gstPct.ToString("0.##", CultureInfo.InvariantCulture))
          .Append("%</td><td class='amt'>AUD ").Append(FormatMoney(gstTotal)).Append("</td></tr>");
        sb.Append("<tr><td><strong>In Words:</strong> ").Append(WebUtility.HtmlEncode(inWords))
          .Append(" Only</td><td></td></tr>");
        sb.Append("<tr><td><strong>TOTAL</strong></td><td class='amt'><strong>AUD ")
          .Append(FormatMoney(grandTotal)).Append("</strong></td></tr>");
        sb.Append("</table>");

        sb.Append("""
            <div class="bank">
              <strong>Bank Details</strong><br/>
              Account Name: AVEC GLOBAL GROUP PTY LTD<br/>
              BSB: 063-549<br/>
              Account Number: 1081 0692<br/>
              Address: Unit 3, 380 Clayton Road, Clayton, Vic: 3168
            </div>
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
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(11).FontColor(Colors.Black));

                page.Content().Column(col =>
                {
                    col.Spacing(14);

                    col.Item().AlignCenter().Text("TAX INVOICE").Bold().FontSize(18).LetterSpacing(0.5f);

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            left.Item().Text("To,").Bold();
                            left.Item().Text(instituteName);
                        });
                        row.RelativeItem().AlignRight().Column(right =>
                        {
                            right.Item().Text($"Date: {dateText}");
                            right.Item().Text($"Invoice No: {invoice.InvoiceNumber}");
                        });
                    });

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
                                c.Item().Text($"Fees: {FormatMoney(line.FeesAmount)}");
                            });
                            table.Cell().Element(BodyCell).AlignRight().Text(FormatMoney(line.InvoiceAmount));
                        }
                    });

                    col.Item().PaddingTop(8).Column(totals =>
                    {
                        totals.Spacing(4);
                        totals.Item().Row(r =>
                        {
                            r.RelativeItem().Text("TOTAL").Bold();
                            r.ConstantItem(140).AlignRight().Text($"AUD {FormatMoney(commissionTotal)}").Bold();
                        });
                        totals.Item().Row(r =>
                        {
                            r.RelativeItem().Text($"GST {gstPct.ToString("0.##", CultureInfo.InvariantCulture)}%");
                            r.ConstantItem(140).AlignRight().Text($"AUD {FormatMoney(gstTotal)}");
                        });
                        totals.Item().Text($"In Words: {inWords} Only").Bold();
                        totals.Item().Row(r =>
                        {
                            r.RelativeItem().Text("TOTAL").Bold();
                            r.ConstantItem(140).AlignRight().Text($"AUD {FormatMoney(grandTotal)}").Bold();
                        });
                    });

                    col.Item().PaddingTop(20).Column(bank =>
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
