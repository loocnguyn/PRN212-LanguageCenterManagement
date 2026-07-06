using ClosedXML.Excel;

namespace Services;

public class ExcelExportService : IExcelExportService
{
    public void ExportToExcel(string filePath, string sheetName, IEnumerable<string> headers, IEnumerable<object?[]> rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(sheetName);

        var headerList = headers.ToList();
        for (int col = 0; col < headerList.Count; col++)
        {
            var cell = sheet.Cell(1, col + 1);
            cell.Value = headerList[col];
            cell.Style.Font.Bold = true;
        }

        int rowIndex = 2;
        foreach (var row in rows)
        {
            for (int col = 0; col < row.Length; col++)
            {
                sheet.Cell(rowIndex, col + 1).Value = XLCellValue.FromObject(row[col]);
            }
            rowIndex++;
        }

        sheet.Columns().AdjustToContents();
        workbook.SaveAs(filePath);
    }
}
