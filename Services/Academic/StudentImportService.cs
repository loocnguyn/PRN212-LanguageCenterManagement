using BusinessObjects;
using Repositories;

namespace Services;

// ============================================================
//  StudentImportService — bulk-create students from a class list.
//
//  Reading the file is StudentFileReader's job. This class only decides which
//  rows are acceptable and creates the accounts.
//
//  Email is required here even though the Students table would take a null one:
//  it is the account's login. That is also why there is no username to invent —
//  the file already carries a unique identity for each person.
//
//  CONTENTS:
//    1. ReadAndValidate — parse via the reader, then check every row
//    2. Validate        — per row, against the file, against the database
//    3. Import          — create User + Student for the valid rows
// ============================================================

public class StudentImportService : IStudentImportService
{
    private readonly IStudentFileReader _reader;
    private readonly IUserService _userService = new UserService();
    private readonly IStudentRepository _studentRepo = new StudentRepository();
    private readonly IUserRepository _userRepo = new UserRepository();

    public StudentImportService() : this(new StudentFileReader()) { }

    /// <summary>Lets a test supply rows without a file on disk.</summary>
    public StudentImportService(IStudentFileReader reader) => _reader = reader;

    // ---- 1. Read + validate ------------------------------------
    public List<StudentImportRow> ReadAndValidate(string filePath)
    {
        var rows = _reader.Read(filePath);
        Validate(rows);
        return rows;
    }

    // ---- 2. Validation -----------------------------------------
    /// <summary>
    /// Three kinds of check, in this order: is the row itself sane, does it clash
    /// with another row in the same file, and does it clash with what is already in
    /// the database. All three matter — a file can be internally consistent and
    /// still be a re-import of students who already exist.
    /// </summary>
    private void Validate(List<StudentImportRow> rows)
    {
        var existingStudents = _studentRepo.GetAll();

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.FullName))
                row.Errors.Add("Full name is required");

            ValidateEmail(row, rows);

            if (row.Phone != null && !IsValidPhone(row.Phone))
                row.Errors.Add("Phone must be 10 digits starting with 0");

            if (row.Gender != null && row.Gender != "Male" && row.Gender != "Female")
                row.Errors.Add("Gender must be Male or Female");

            if (row.DateOfBirth > DateOnly.FromDateTime(DateTime.Today))
                row.Errors.Add("Date of birth is in the future");

            // Same person, different address: not something the import can decide,
            // but the staff member needs to see it before creating a second record.
            var twin = existingStudents.FirstOrDefault(s =>
                row.DateOfBirth.HasValue
                && s.DateOfBirth == row.DateOfBirth
                && s.FullName.Equals(row.FullName, StringComparison.OrdinalIgnoreCase));

            if (twin != null)
                row.Errors.Add($"A student named {twin.FullName} with the same date of birth already exists");
        }
    }

    private void ValidateEmail(StudentImportRow row, List<StudentImportRow> allRows)
    {
        if (string.IsNullOrWhiteSpace(row.Email))
        {
            row.Errors.Add("Email is required — it is the student's login");
            return;
        }

        if (!IsValidEmail(row.Email))
        {
            row.Errors.Add($"'{row.Email}' is not a valid email address");
            return;
        }

        // Same address twice in one file: keep the first, refuse the rest, because
        // only one of them can own the login.
        var earlier = allRows.FirstOrDefault(r => r.RowNumber < row.RowNumber && r.Email == row.Email);
        if (earlier != null)
            row.Errors.Add($"Duplicate of row {earlier.RowNumber} in this file");

        if (_userRepo.IsEmailTaken(row.Email))
            row.Errors.Add($"{row.Email} already has an account");
    }

    private static bool IsValidEmail(string email)
        => System.Text.RegularExpressions.Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");

    private static bool IsValidPhone(string phone)
        => System.Text.RegularExpressions.Regex.IsMatch(phone, @"^0\d{9}$");

    // ---- 3. Import ---------------------------------------------
    public int Import(List<StudentImportRow> rows, string defaultPassword)
    {
        var valid = rows.Where(r => r.IsValid).ToList();
        if (valid.Count == 0)
            throw new InvalidOperationException("There is no valid row to import.");

        foreach (var row in valid)
        {
            var user = new User { Email = row.Email, Role = "STUDENT", IsActive = true };

            // Everyone starts on the same password, so UserService flags each account
            // to replace it at first login (see ChangePasswordWindow).
            _userService.Save(user, defaultPassword, mustChangePassword: true);

            _studentRepo.Save(new Student
            {
                UserId = user.Id,
                FullName = row.FullName,
                DateOfBirth = row.DateOfBirth,
                Gender = row.Gender,
                Phone = row.Phone,
                Email = row.Email,
                Address = row.Address,
                Balance = 0,
                Status = "ACTIVE"
            });
        }

        return valid.Count;
    }
}
