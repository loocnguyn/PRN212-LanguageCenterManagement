using BusinessObjects;

namespace Services;

// IStudentFileReader — reads an import file into rows. No rules, no database.

public interface IStudentFileReader
{
    /// <summary>
    /// Every data row of a .csv or .xlsx student list, in file order. Only parse
    /// failures (a date that is not dd/MM/yyyy) are recorded on the row; whether the
    /// row is acceptable is <see cref="IStudentImportService"/>'s job.
    /// </summary>
    List<StudentImportRow> Read(string filePath);
}
