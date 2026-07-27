using BusinessObjects;

namespace Services;

// IStudentImportService — read a class list from a file and turn it into accounts.

public interface IStudentImportService
{
    /// <summary>
    /// Reads every row of a .csv or .xlsx file and checks it. The returned rows are
    /// ALL of them, valid and invalid — the caller shows them for review before
    /// anything is saved.
    /// </summary>
    List<StudentImportRow> ReadAndValidate(string filePath);

    /// <summary>
    /// Creates a User + Student for each valid row. Invalid rows are skipped, never
    /// guessed at. Returns how many students were created.
    /// </summary>
    int Import(List<StudentImportRow> rows, string defaultPassword);
}
