using AvecADeskApi.Model.Invoice;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;
using System.Net;
using System.Text;

namespace AvecADeskApi.Services;


public class InvoiceDocumentService
{
  
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

    public string BuildHtml(InvoiceResponse invoice,string instituteName,IReadOnlyList<MonthlyPaidInstallmentRow> lines,DateTime invoiceDate)
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
            <p>Thank you for your time and consideration.<br/>Regards,</p>
            </body></html>
            """);

        return sb.ToString();
    }

    private const string PdfFont = "Times New Roman";  
    private const float BorderW = 0.85f;
    private const float SrColPt = 45f;       
    private const float AmountColPt = 159f;  
    private const float LogoWidthPt = 142f; 
    private const string BrandLogoUrl = "https://demoavecapi.apphub.co.in/uploads/Mailsend/avec-global-logo-full.png";
    private static readonly HttpClient LogoHttp = new() { Timeout = TimeSpan.FromSeconds(10) };
    private static byte[]? s_brandLogo;
    private static DateTime s_brandLogoLastFail = DateTime.MinValue;
   
    private static byte[]? GetBrandLogo()
    {
        if (s_brandLogo is not null) return s_brandLogo;
        if (DateTime.UtcNow - s_brandLogoLastFail < TimeSpan.FromMinutes(5)) return null;

        try
        {
            s_brandLogo = LogoHttp.GetByteArrayAsync(BrandLogoUrl).GetAwaiter().GetResult();
            return s_brandLogo;
        }
        catch (Exception ex)
        {
            s_brandLogoLastFail = DateTime.UtcNow;
            System.Diagnostics.Debug.WriteLine($"Invoice logo download failed ({BrandLogoUrl}): {ex.Message}");
            return null;
        }
    }
   
    private static string FirstProp(object? source, params string[] names)
    {
        if (source is null) return string.Empty;

        var type = source.GetType();
        foreach (var name in names)
        {
            var p = type.GetProperty(
                name,
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.IgnoreCase);

            var v = p?.GetValue(source)?.ToString()?.Trim();
            if (!string.IsNullOrEmpty(v) && v != "—") return v;
        }
        return string.Empty;
    }

    private static List<string> BuildAddressLines(object invoice,IReadOnlyList<MonthlyPaidInstallmentRow> lines,string? explicitAddress,IEnumerable<object>? addressSources = null)  
    {
        if (!string.IsNullOrWhiteSpace(explicitAddress))
        {
            return explicitAddress
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
        }

        var streetNames = new[] { "Address", "InstituteAddress", "Address1", "AddressLine1", "Street", "StreetAddress", "AddressLine", "Location" };
        var campusNames = new[] { "Campus", "CampusName" };
        var zipNames = new[] { "ZipCode", "Zip", "PostCode", "Postcode", "PostalCode", "PinCode" };

        var sources = new List<object?> { invoice };
        sources.AddRange(lines.Cast<object>());
        if (addressSources is not null) sources.AddRange(addressSources);

        var street = "";
        var campus = "";
        var zip = "";

        foreach (var s in sources)
        {
            if (street == "") street = FirstProp(s, streetNames);
            if (campus == "") campus = FirstProp(s, campusNames);
            if (zip == "") zip = FirstProp(s, zipNames);
            if (street != "" && campus != "" && zip != "") break;
        }

        street = street.TrimEnd(',', ' ');

        var result = new List<string>();
        if (street != "") result.Add(zip != "" ? street + "," : street);

        var campusAndZip = string.Join(", ", new[] { campus, zip }.Where(v => v != ""));
        if (campusAndZip != "") result.Add(campusAndZip);

        return result;
    }


    public byte[] BuildPdfBytes(InvoiceResponse invoice, string instituteName, IReadOnlyList<MonthlyPaidInstallmentRow> lines, DateTime invoiceDate, string? instituteAddress = null, IEnumerable<object>? addressSources = null)
    {
        var commissionTotal = lines.Sum(x => x.CommissionAmount);
        var bonusTotal = lines.Sum(x => x.BonusAmount);
        var gstTotal = lines.Sum(x => x.GSTAmount);
        var grandTotal = commissionTotal + bonusTotal + gstTotal;
        var inWords = AmountToWords(grandTotal);
        var dateText = invoiceDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        var addressLines = BuildAddressLines(invoice, lines, instituteAddress, addressSources);
        var logoBytes = GetBrandLogo() ?? _logoBytes;

        static bool IsPercentageType(MonthlyPaidInstallmentRow x) => (x.CommissionType ?? string.Empty).Trim().StartsWith("Percent", StringComparison.OrdinalIgnoreCase);

        var studentGroups = lines
            .GroupBy(x => new { x.StudentId, x.CourseId })
            .Select(g => new
            {
                Items = g.ToList(),
                First = g.First(),
                Commission = g.Sum(x => x.CommissionAmount)
            })
            .OrderByDescending(x => x.Commission)
            .ToList();

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontFamily(PdfFont).FontSize(12).FontColor(Colors.Black));

                page.Content().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        if (logoBytes is not null)
                            row.ConstantItem(LogoWidthPt).Image(logoBytes).FitWidth();
                        else
                            row.ConstantItem(LogoWidthPt);

                        row.RelativeItem().AlignMiddle().Column(company =>
                        {
                            company.Item().AlignRight().Text("AVEC GLOBAL GROUP PTY LTD").Bold().FontSize(10);
                            company.Item().AlignRight().Text("ABN: 79677235979").FontSize(10);
                            company.Item().AlignRight().Text("Email: account@avec-global.com").FontSize(10);
                            company.Item().AlignRight().Text("Phone: +61 3 9034 6484").FontSize(10);
                        });
                    });

                    col.Item().PaddingTop(18).PaddingBottom(8).AlignCenter().Text("TAX INVOICE").Bold().FontSize(14);

                    col.Item().Border(BorderW).BorderColor(Colors.Black).Background(BoxFill).Row(row =>
                    {
                        row.RelativeItem().Padding(7).Column(left =>
                        {
                            left.Item().Text("To,").FontSize(11);
                            left.Item().Text(instituteName).Bold().FontSize(11);

                            foreach (var l in addressLines)
                                left.Item().Text(l).FontSize(11);
                        });

                        row.ConstantItem(AmountColPt).BorderLeft(BorderW).BorderColor(Colors.Black).PaddingVertical(7).PaddingHorizontal(8).AlignRight().Row(r =>
                        {
                            r.AutoItem().Column(c =>
                            {
                                c.Item().Text("Date:").Bold().FontSize(11);
                                c.Item().Text("Invoice No:").Bold().FontSize(11);
                            });

                            r.AutoItem().PaddingLeft(6).Column(c =>
                            {
                                c.Item().Text(dateText).FontSize(11);
                                c.Item().Text(invoice.InvoiceNumber ?? string.Empty).FontSize(11);
                            });
                        });
                    });

                    col.Item().Height(8);

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(SrColPt);
                            columns.RelativeColumn();
                            columns.ConstantColumn(AmountColPt);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(c => GridCell(c, top: true, bottom: true)).PaddingVertical(5).AlignCenter().Text("Sr. No").Bold().FontSize(11);
                            header.Cell().Element(c => GridCell(c, top: true, bottom: true)).PaddingVertical(5).AlignCenter().Text("PARTICULARS").Bold().FontSize(11);
                            header.Cell().Element(c => GridCell(c, top: true, bottom: true, right: true)).PaddingVertical(5).AlignCenter().Text("AMOUNT (AUD$)").Bold().FontSize(11);
                        });

                        table.Cell().Element(c => GridCell(c)).Text(string.Empty);
                        table.Cell().Element(c => GridCell(c)).PaddingHorizontal(8).PaddingTop(8).PaddingBottom(14).Text("Education Commission");
                        table.Cell().Element(c => GridCell(c, right: true)).Text(string.Empty);

                        var sr = 1;

                        foreach (var group in studentGroups)
                        {
                            var first = group.First;
                            var rowAmount = group.Commission;
                            var percentageItems = group.Items.Where(IsPercentageType).ToList();
                            var percentageFees = percentageItems.Sum(x => x.FeesAmount);
                            var percentageCommission = percentageItems.Sum(x => x.CommissionAmount);
                            var pct = percentageFees > 0 ? Math.Round(percentageCommission / percentageFees * 100m, 2) : 0m;
                            var showFees = percentageItems.Count > 0 && percentageFees > 0;
                            var cricosCode = group.Items.Select(x => x.CricosCode?.Trim()).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

                            table.Cell().Element(c => GridCell(c)).PaddingVertical(4).AlignCenter().Text($"{sr++}.");

                            table.Cell().Element(c => GridCell(c)).PaddingHorizontal(8).PaddingVertical(4).Column(c =>
                            {
                                c.Item().Text($"Student Name: {first.FullName}");
                                c.Item().Text($"Student Id: {first.StudentCode}");

                                c.Item().Text(text =>
                                {
                                    text.Span($"Course Details: {first.CourseName}");

                                    if (!string.IsNullOrWhiteSpace(cricosCode))
                                        text.Span($" ({cricosCode})");
                                });

                                if (showFees)
                                    c.Item().Text($"Fees: $ {FormatMoney(percentageFees)} * {pct.ToString("0.##", CultureInfo.InvariantCulture)}/100");
                            });

                            table.Cell().Element(c => GridCell(c, right: true)).PaddingVertical(4).PaddingRight(8).AlignRight().Text(FormatMoney(rowAmount));
                        }
                    });

                    col.Item().ShowEntire().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(SrColPt);
                            columns.RelativeColumn();
                            columns.ConstantColumn(AmountColPt);
                        });

                        void TotalRow(string label, decimal value)
                        {
                            table.Cell().ColumnSpan(2).Element(c => GridCell(c, top: true)).PaddingVertical(4).PaddingRight(8).AlignRight().Text(label).Bold().FontSize(11);
                            table.Cell().Element(c => GridCell(c, top: true, right: true)).PaddingVertical(4).PaddingRight(8).AlignRight().Text($"AUD {FormatMoney(value)}").Bold().FontSize(11);
                        }

                        TotalRow("TOTAL", commissionTotal);

                        if (bonusTotal > 0)
                            TotalRow("BONUS", bonusTotal);

                        if (gstTotal > 0)
                            TotalRow("GST", gstTotal);

                        TotalRow("GRAND TOTAL", grandTotal);

                        table.Cell().ColumnSpan(3).Element(c => GridCell(c, top: true, bottom: true, right: true)).PaddingVertical(4).PaddingHorizontal(8).Text(t =>
                        {
                            t.Span("In Words: ").Bold();
                            t.Span($"{inWords} Only");
                        });
                    });

                    col.Item().PaddingTop(24).ShowEntire().Column(bank =>
                    {
                        bank.Spacing(2);
                        bank.Item().Text("Bank Details").Bold().Underline();
                        bank.Item().Text("Account Name: AVEC GLOBAL GROUP PTY LTD");
                        bank.Item().Text("BSB: 063-549");
                        bank.Item().Text("Account Number: 1081 0692");
                    });
                });
            });
        }).GeneratePdf();
    }

    private static IContainer GridCell(IContainer c,bool top = false,bool bottom = false,bool left = true,bool right = false)
    {
        if (left) c = c.BorderLeft(BorderW);
        if (right) c = c.BorderRight(BorderW);
        if (top) c = c.BorderTop(BorderW);
        if (bottom) c = c.BorderBottom(BorderW);
        return c.BorderColor(Colors.Black);
    }

    private static string FormatMoney(decimal value)
        => value.ToString("#,##0.00", CultureInfo.InvariantCulture);

    private static string AmountToWords(decimal amount)
    {
        var totalCents = (long)Math.Round(Math.Abs(amount) * 100m, MidpointRounding.AwayFromZero);
        var whole = totalCents / 100;
        var cents = (int)(totalCents % 100);

        var words = NumberToWords(whole);
        if (cents > 0)
            words += $" and {NumberToWords(cents)} Cents";
        return words;
    }

    private static string NumberToWords(long number)
    {
        if (number == 0) return "Zero";
        if (number < 0) return "Minus " + NumberToWords(Math.Abs(number));

        var units = new[]
        {
        "", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine",
        "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen",
        "Seventeen", "Eighteen", "Nineteen"
    };
        var tens = new[] { "", "", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety" };

        var words = "";
        if (number / 1_000_000_000 > 0)
        {
            words += NumberToWords(number / 1_000_000_000) + " Billion ";
            number %= 1_000_000_000;
        }
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
            if (number < 20)
            {
                words += units[number];
            }
            else
            {
                words += tens[number / 10];
                if (number % 10 > 0) words += " " + units[number % 10];
            }
        }

        return words.Trim();
    }

}