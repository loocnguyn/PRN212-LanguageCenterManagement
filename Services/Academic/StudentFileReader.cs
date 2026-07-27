using System.Text;
using BusinessObjects;
using ClosedXML.Excel;

namespace Services;

// ============================================================
//  StudentFileReader — turns an import file into rows. Nothing more.
//
//  It knows CSV and XLSX; it does NOT know what a valid student is, and it never
//  touches the database. StudentImportService owns those rules. Keeping the two
//  apart means the parsing can be tested on its own, and adding a third file
//  format later changes only this file.
//
//  Expected columns, in this order (row 1 is a header and is skipped):
//    1 FullName | 2 DateOfBirth (dd/MM/yyyy) | 3 Gender | 4 Phone | 5 Email | 6 Address
// ============================================================

public class StudentFileReader : IStudentFileReader
{
    private const int ColumnCount = 6;

    public List<StudentImportRow> Read(string filePath)
    {
        return filePath.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)
            ? ReadCsv(filePath)
            : ReadExcel(filePath);
    }

    private static List<StudentImportRow> ReadCsv(string filePath)
    {
        var rows = new List<StudentImportRow>();
        var lines = File.ReadAllLines(filePath, Encoding.UTF8);

        // Line 1 is the header, so data starts at line 2. RowNumber is the real line
        // number in the file, so an error message points at the line to go and fix.
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            rows.Add(BuildRow(i + 1, SplitCsvLine(lines[i])));
        }

        return rows;
    }

    private static List<StudentImportRow> ReadExcel(string filePath)
    {
        var rows = new List<StudentImportRow>();

        using var workbook = new XLWorkbook(filePath);
        var sheet = workbook.Worksheet(1);
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;

        for (int rowNum = 2; rowNum <= lastRow; rowNum++)
        {
            var row = sheet.Row(rowNum);
            if (row.IsEmpty()) continue;

            var cells = new string[ColumnCount];
            for (int c = 0; c < ColumnCount; c++)
                cells[c] = row.Cell(c + 1).GetString().Trim();

            rows.Add(BuildRow(rowNum, cells));
        }

        return rows;
    }

    /// <summary>
    /// Splits one CSV line, honouring "quoted, fields" so an address with a comma in
    /// it does not shift every later column one place to the left.
    /// </summary>
    private static string[] SplitCsvLine(string line)
    {
        var cells = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        foreach (var ch in line)
        {
            if (ch == '"') inQuotes = !inQuotes;
            else if (ch == ',' && !inQuotes) { cells.Add(current.ToString().Trim()); current.Clear(); }
            else current.Append(ch);
        }
        cells.Add(current.ToString().Trim());

        // A short row is fine — the missing columns are simply empty, and the
        // validation step decides whether that matters.
        while (cells.Count < ColumnCount) cells.Add("");
        return cells.ToArray();
    }

    /// <summary>Raw cells to a row. Only records what fails to PARSE; the rest is validation.</summary>
    private static StudentImportRow BuildRow(int rowNumber, string[] cells)
    {
        var row = new StudentImportRow
        {
            RowNumber = rowNumber,
            FullName = cells[0].Trim(),
            Gender = NullIfEmpty(cells[2]),
            Phone = NullIfEmpty(cells[3]),
            Email = cells[4].Trim().ToLower(),
            Address = NullIfEmpty(cells[5])
        };

        var dobText = cells[1].Trim();
        if (!string.IsNullOrEmpty(dobText))
        {
            if (DateOnly.TryParseExact(dobText, "dd/MM/yyyy", out var dob))
                row.DateOfBirth = dob;
            else
                row.Errors.Add($"Date of birth '{dobText}' is not dd/MM/yyyy");
        }

        return row;
    }

    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
