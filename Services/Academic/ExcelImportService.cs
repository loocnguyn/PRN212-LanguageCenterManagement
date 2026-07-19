using BusinessObjects;
using ClosedXML.Excel;

namespace Services;

// ExcelImportService — business-logic entry point for ExcelImport (mostly delegates to the repository).

public class ExcelImportService : IExcelImportService
{
    public List<StudentImportRow> ImportStudentsFromExcel(string filePath)
    {
        var results = new List<StudentImportRow>();

        using var workbook = new XLWorkbook(filePath);
        var sheet = workbook.Worksheet(1);
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;

        for (int rowNum = 2; rowNum <= lastRow; rowNum++)
        {
            var row = sheet.Row(rowNum);
            if (row.IsEmpty()) continue;

            var item = new StudentImportRow
            {
                RowNumber = rowNum,
                FullName = row.Cell(1).GetString().Trim(),
                Gender = NullIfEmpty(row.Cell(3).GetString().Trim()),
                Phone = NullIfEmpty(row.Cell(4).GetString().Trim()),
                Email = NullIfEmpty(row.Cell(5).GetString().Trim())
            };

            var dobText = row.Cell(2).GetString().Trim();
            if (!string.IsNullOrEmpty(dobText))
            {
                if (DateOnly.TryParseExact(dobText, "dd/MM/yyyy", out var dob))
                    item.DateOfBirth = dob;
                else
                    item.Errors.Add($"Invalid DateOfBirth format '{dobText}', expected dd/MM/yyyy.");
            }

            if (string.IsNullOrWhiteSpace(item.FullName))
                item.Errors.Add("FullName is required.");

            if (item.Phone != null && !System.Text.RegularExpressions.Regex.IsMatch(item.Phone, @"^0\d{9}$"))
                item.Errors.Add("Phone must be 10 digits starting with 0.");

            if (item.Email != null && !System.Text.RegularExpressions.Regex.IsMatch(item.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                item.Errors.Add("Invalid email format.");

            results.Add(item);
        }

        return results;
    }

    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
