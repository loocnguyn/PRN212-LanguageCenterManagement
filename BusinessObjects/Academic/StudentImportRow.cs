namespace BusinessObjects;

/// <summary>
/// One row read from a student import file, before anything is written to the database.
/// The import shows every row back to the user — valid or not — so <see cref="Errors"/>
/// carries the reasons a row will be skipped instead of the import failing as a whole.
/// </summary>
public class StudentImportRow
{
    /// <summary>Line/row number in the source file, so the user can go and fix it.</summary>
    public int RowNumber { get; set; }

    public string FullName { get; set; } = "";
    public DateOnly? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Phone { get; set; }

    /// <summary>Becomes the account's login, so it is required and must be unique.</summary>
    public string Email { get; set; } = "";

    public string? Address { get; set; }

    public List<string> Errors { get; set; } = new();

    public bool IsValid => Errors.Count == 0;

    /// <summary>"OK" or every reason this row is being skipped, for the preview grid.</summary>
    public string Status => IsValid ? "OK" : string.Join("; ", Errors);
}
