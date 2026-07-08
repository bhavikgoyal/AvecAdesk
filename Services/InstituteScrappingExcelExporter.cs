using AvecADeskApi.Model.InstituteScrapping;
using ClosedXML.Excel;

namespace AvecADeskApi.Services
{
  public static class InstituteScrappingExcelExporter
  {
    private static readonly (string Header, Func<InstituteScrappingResponse, string?> Value)[] Columns =
    [
        ("Scrapping ID", r => r.ScrappingId.ToString()),
        ("Institute name", r => r.InstituteName),
        ("Website URL", r => r.WebsiteURL),
        ("Campus", r => r.Campus),
        ("State", r => r.State),
        ("Program name", r => r.ProgramName),
        ("Level", r => r.Level),
        ("Program link", r => r.ProgramLink),
        ("CRICOS code", r => r.CricosCode),
        ("Duration", r => r.Duration),
        ("Intake", r => r.Intake),
        ("Fees yearly", r => r.FeesYearly),
        ("English requirement", r => r.EnglishReq),
        ("Name", r => r.Name),
        ("Logo URL", r => r.Logo),
        ("Country", r => r.Country),
        ("City", r => r.City),
        ("Description", r => r.Description),
        ("Country ranking", r => r.CountryRanking),
        ("Scholarships details", r => r.ScholarshipsDetails),
        ("Program description", r => r.ProgramDescription),
        ("Program logo", r => r.ProgramLogo),
        ("Admission requirements", r => r.AddmissionRequirements),
        ("Created at", r => r.CreatedAt?.ToString("yyyy-MM-dd HH:mm:ss")),
    ];

    public static byte[] BuildWorkbook(IReadOnlyList<InstituteScrappingResponse> rows)
    {
      using var workbook = new XLWorkbook();
      var worksheet = workbook.Worksheets.Add("Institute Scrap");

      worksheet.Cell(1, 1).Value = "S No";
      for (var columnIndex = 0; columnIndex < Columns.Length; columnIndex++)
      {
        worksheet.Cell(1, columnIndex + 2).Value = Columns[columnIndex].Header;
      }

      var headerRow = worksheet.Range(1, 1, 1, Columns.Length + 1);
      headerRow.Style.Font.Bold = true;
      headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#F3F4F6");

      for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
      {
        var row = rows[rowIndex];
        var excelRow = rowIndex + 2;
        worksheet.Cell(excelRow, 1).Value = rowIndex + 1;

        for (var columnIndex = 0; columnIndex < Columns.Length; columnIndex++)
        {
          worksheet.Cell(excelRow, columnIndex + 2).Value = Columns[columnIndex].Value(row) ?? string.Empty;
        }
      }

      worksheet.Columns().AdjustToContents();

      using var stream = new MemoryStream();
      workbook.SaveAs(stream);
      return stream.ToArray();
    }
  }
}
