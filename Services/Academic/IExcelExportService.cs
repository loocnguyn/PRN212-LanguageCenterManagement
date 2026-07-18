namespace Services;

public interface IExcelExportService
{
    /// <summary>Generic export: writes a list of rows (each an array of cell values) to an .xlsx file.</summary>
    void ExportToExcel(string filePath, string sheetName, IEnumerable<string> headers, IEnumerable<object?[]> rows);
}
