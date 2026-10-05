using ApexPerformance.API.Features.Workouts;
using ClosedXML.Excel;

namespace ApexPerformance.API.Utilities;

public static class WorkoutsImportExcel
{
    /// <summary>
    /// Create Excel file with rows that were not imported.
    /// Columns are same as in import template (with extra
    /// Error column), so file can be fixed and imported again.
    /// </summary>
    public static MemoryStream CreateFailedRowsFile(List<WorkoutsImportRowIssue> failedRows)
    {
        using var workbook = new XLWorkbook();

        var worksheet = workbook.Worksheets.Add("Vjezbe");

        string[] headers = ["Name", "Description", "VideoUrl", "WorkoutTypes", "Language", "Greska", "RedakUDatoteci"];

        for (var i = 0; i < headers.Length; i++)
            worksheet.Cell(1, i + 1).Value = headers[i];

        worksheet.Row(1).Style.Font.Bold = true;

        var rowNumber = 2;

        foreach (var failedRow in failedRows)
        {
            worksheet.Cell(rowNumber, 1).Value = failedRow.Name;
            worksheet.Cell(rowNumber, 2).Value = failedRow.Description;
            worksheet.Cell(rowNumber, 3).Value = failedRow.VideoUrl;
            worksheet.Cell(rowNumber, 4).Value = failedRow.WorkoutTypes;
            worksheet.Cell(rowNumber, 5).Value = failedRow.Language;
            worksheet.Cell(rowNumber, 6).Value = failedRow.Reason;
            worksheet.Cell(rowNumber, 7).Value = failedRow.RowNumber;
            rowNumber++;
        }

        worksheet.Columns().AdjustToContents(1, 100, 10, 60);

        var stream = new MemoryStream();

        workbook.SaveAs(stream);

        stream.Position = 0;

        return stream;
    }
}
