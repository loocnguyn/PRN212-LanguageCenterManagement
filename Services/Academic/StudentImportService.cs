using System.Text;
using BusinessObjects;
using ClosedXML.Excel;
using Repositories;

namespace Services;

// ============================================================
//  StudentImportService — bulk-create students from a class list.
//
//  Expected columns, in this order (first line is a header and is skipped):
//    1 FullName | 2 DateOfBirth (dd/MM/yyyy) | 3 Gender | 4 Phone | 5 Email | 6 Address
//
//  Email is the account's login, which is why it is required here even though the
//  Students table would accept a null one. That is also the reason there is no
//  username to invent: the file already carries a unique identity for each person.
//
//  CONTENTS:
//    1. ReadAndValidate  — parse the file, then check every row
//    2. Parsing          — CSV and XLSX
//    3. Validation       — per row, against the file, against the database
//    4. Import           — create User + Student for the valid rows
// ============================================================

public class StudentImportService : IStudentImportService
{
    private readonly IUserRepository _userRepo = new UserRepository();
    private readonly IStudentRepository _studentRepo = new StudentRepository();

    // ---- 1. Read + validate ------------------------------------
    public List<StudentImportRow> ReadAndValidate(string filePath)
    {
        var rows = filePath.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)
            ? ReadCsv(filePath)
            : ReadExcel(filePath);

        Validate(rows);
        return rows;
    }

    // ---- 2. Parsing --------------------------------------------
    private static List<StudentImportRow> ReadCsv(string filePath)
    {
        var rows = new List<StudentImportRow>();
        var lines = File.ReadAllLines(filePath, Encoding.UTF8);

        // Line 1 is the header; data starts at line 2, and RowNumber is the real
        // line number so an error message points at the right line of the file.
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            var cells = SplitCsvLine(lines[i]);
            rows.Add(BuildRow(i + 1, cells));
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

            var cells = new string[6];
            for (int c = 0; c < cells.Length; c++)
                cells[c] = row.Cell(c + 1).GetString().Trim();

            rows.Add(BuildRow(rowNum, cells));
        }

        return rows;
    }

    /// <summary>
    /// Splits one CSV line, honouring "quoted, fields" so an address with a comma
    /// in it does not shift every later column one to the left.
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

        while (cells.Count < 6) cells.Add("");
        return cells.ToArray();
    }

    /// <summary>Turns raw cells into a row, recording anything that does not parse.</summary>
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

    // ---- 3. Validation -----------------------------------------
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

            if (string.IsNullOrWhiteSpace(row.Email))
            {
                row.Errors.Add("Email is required — it is the student's login");
            }
            else if (!IsValidEmail(row.Email))
            {
                row.Errors.Add($"'{row.Email}' is not a valid email address");
            }
            else
            {
                // Same address twice in one file: keep the first, refuse the rest,
                // because only one of them can own the login.
                var earlier = rows.FirstOrDefault(r => r != row
                                                    && r.RowNumber < row.RowNumber
                                                    && r.Email == row.Email);
                if (earlier != null)
                    row.Errors.Add($"Duplicate of row {earlier.RowNumber} in this file");

                if (_userRepo.IsEmailTaken(row.Email))
                    row.Errors.Add($"{row.Email} already has an account");
            }

            if (row.Phone != null && !IsValidPhone(row.Phone))
                row.Errors.Add("Phone must be 10 digits starting with 0");

            if (row.Gender != null && row.Gender != "Male" && row.Gender != "Female")
                row.Errors.Add("Gender must be Male or Female");

            if (row.DateOfBirth.HasValue && row.DateOfBirth.Value > DateOnly.FromDateTime(DateTime.Today))
                row.Errors.Add("Date of birth is in the future");

            // Same person, different address: not an error the import can decide on,
            // but the staff member needs to see it before creating a second record.
            var sameName = existingStudents.FirstOrDefault(s =>
                s.FullName.Equals(row.FullName, StringComparison.OrdinalIgnoreCase)
                && s.DateOfBirth == row.DateOfBirth
                && row.DateOfBirth.HasValue);

            if (sameName != null)
                row.Errors.Add($"A student named {sameName.FullName} with the same date of birth already exists");
        }
    }

    private static bool IsValidEmail(string email)
        => System.Text.RegularExpressions.Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");

    private static bool IsValidPhone(string phone)
        => System.Text.RegularExpressions.Regex.IsMatch(phone, @"^0\d{9}$");

    // ---- 4. Import ---------------------------------------------
    public int Import(List<StudentImportRow> rows, string defaultPassword)
    {
        var valid = rows.Where(r => r.IsValid).ToList();
        if (valid.Count == 0)
            throw new InvalidOperationException("There is no valid row to import.");

        var created = 0;
        foreach (var row in valid)
        {
            var user = new User
            {
                Email = row.Email,
                Role = "STUDENT",
                IsActive = true
            };

            // Everyone gets the same starting password, so every imported account is
            // flagged to replace it at first login (see ChangePasswordWindow).
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(defaultPassword);
            user.MustChangePassword = true;
            _userRepo.Save(user);

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

            created++;
        }

        return created;
    }
}
