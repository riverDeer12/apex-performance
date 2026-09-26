using ClosedXML.Excel;

namespace ApexPerformance.API.Utilities;

public sealed class ExcelRow
{
    public int RowNumber { get; init; }
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public string VideoUrl { get; init; } = "";
    public List<string> WorkoutTypes { get; init; } = new();
}

public static class ExcelLoader
{
    /// <summary>
    /// Loads rows from an Excel file (.xlsx). The sheet is expected to have a header row with columns:
    /// Name, Description, VideoUrl, WorkoutTypes.
    /// WorkoutTypes are comma-separated strings and will be split into a list.
    /// Blank rows are skipped, rows after them are still loaded.
    /// </summary>
    public static List<ExcelRow> Load(Stream excelStream, string? sheetName = null)
    {
        if (excelStream == null)
            throw new ArgumentNullException(nameof(excelStream));

        if (!excelStream.CanRead)
            throw new ArgumentException("Excel stream must be readable.", nameof(excelStream));

        using var workbook = new XLWorkbook(EnsureSeekable(excelStream));
        var worksheet = string.IsNullOrWhiteSpace(sheetName)
            ? workbook.Worksheets.First()
            : workbook.Worksheet(sheetName);

        // Map headers (case-insensitive, trimmed)
        var headerRow = worksheet.FirstRowUsed()
                        ?? throw new InvalidOperationException("No rows found in worksheet.");

        var headers = headerRow.CellsUsed()
            .ToDictionary(
                c => (c.GetString() ?? string.Empty).Trim(),
                c => c.Address.ColumnNumber,
                StringComparer.OrdinalIgnoreCase
            );

        int Col(string header)
            => headers.TryGetValue(header, out var idx)
                ? idx
                : throw new InvalidOperationException($"Missing required column header: '{header}'");

        var nameCol = Col("Name");
        var descCol = Col("Description");
        var videoCol = Col("VideoUrl");
        var catsCol = Col("WorkoutTypes");

        var rows = new List<ExcelRow>();
        var firstDataRow = headerRow.RowNumber() + 1;
        var lastDataRow = worksheet.LastRowUsed()?.RowNumber() ?? headerRow.RowNumber();

        for (var r = firstDataRow; r <= lastDataRow; r++)
        {
            var row = worksheet.Row(r);

            var allBlank =
                IsBlank(row.Cell(nameCol)) &&
                IsBlank(row.Cell(descCol)) &&
                IsBlank(row.Cell(videoCol)) &&
                IsBlank(row.Cell(catsCol));

            if (allBlank)
                continue;

            rows.Add(new ExcelRow
            {
                RowNumber = r,
                Name = GetCellString(row.Cell(nameCol)),
                Description = GetCellString(row.Cell(descCol)),
                VideoUrl = GetCellString(row.Cell(videoCol)),
                WorkoutTypes = SplitCategories(GetCellString(row.Cell(catsCol)))
            });
        }

        return rows;
    }


    private static bool IsBlank(IXLCell cell)
        => string.IsNullOrWhiteSpace(cell.GetString());

    private static string GetCellString(IXLCell cell)
        => (cell.GetString() ?? "").Trim();

    private static List<string> SplitCategories(string raw)
        => raw.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    private static Stream EnsureSeekable(Stream stream)
    {
        if (stream.CanSeek)
        {
            stream.Position = 0;
            return stream;
        }

        var ms = new MemoryStream();
        stream.CopyTo(ms);
        ms.Position = 0;
        return ms;
    }
}