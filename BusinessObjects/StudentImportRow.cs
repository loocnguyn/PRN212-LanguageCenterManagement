namespace BusinessObjects;

/// <summary>One parsed row from a student import Excel file, before it is saved to the DB.</summary>
public class StudentImportRow
{
    public int RowNumber { get; set; }
    public string FullName { get; set; } = "";
    public DateOnly? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public List<string> Errors { get; set; } = new();
    public bool IsValid => Errors.Count == 0;
}
