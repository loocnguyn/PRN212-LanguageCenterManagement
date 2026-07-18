using BusinessObjects;

namespace Services;

public interface IExcelImportService
{
    /// <summary>
    /// Parses an Excel file for bulk student import. Expected columns (row 1 = header):
    /// FullName | DateOfBirth (dd/MM/yyyy) | Gender | Phone | Email
    /// Each row is validated; invalid rows are returned with their Errors populated
    /// so the caller can show them to the user before saving anything to the DB.
    /// </summary>
    List<StudentImportRow> ImportStudentsFromExcel(string filePath);
}
