using BusinessObjects;
using Microsoft.EntityFrameworkCore;

namespace DataAccessObjects;

// SemesterDAO — EF data access for Semester (CRUD + queries).
//
// Note: Semester.IsActive is a derived property and is NOT translatable to SQL.
// Any "which semester is current" query must compare the date columns, as
// GetActive does below.

public class SemesterDAO
{
    public static List<Semester> GetAll()
    {
        using var context = new LanguageCenterContext();
        return context.Semesters.OrderByDescending(s => s.StartDate).ToList();
    }

    public static Semester? GetById(int id)
    {
        using var context = new LanguageCenterContext();
        return context.Semesters.FirstOrDefault(s => s.SemesterId == id);
    }

    /// <summary>The semester containing today, or null if today falls in a gap between semesters.</summary>
    public static Semester? GetActive()
    {
        using var context = new LanguageCenterContext();
        var today = DateOnly.FromDateTime(DateTime.Today);
        return context.Semesters
            .FirstOrDefault(s => s.StartDate <= today && today <= s.EndDate);
    }

    /// <summary>
    /// Every semester whose dates clash with [start, end], excluding <paramref name="excludeId"/>
    /// (pass the row being edited so it does not overlap itself).
    /// </summary>
    public static List<Semester> GetOverlapping(DateOnly start, DateOnly end, int? excludeId = null)
    {
        using var context = new LanguageCenterContext();
        return context.Semesters
            .Where(s => excludeId == null || s.SemesterId != excludeId)
            .Where(s => s.StartDate <= end && start <= s.EndDate)
            .OrderBy(s => s.StartDate)
            .ToList();
    }

    /// <summary>True when another semester already uses this name (case-insensitive per DB collation).</summary>
    public static bool NameExists(string name, int? excludeId = null)
    {
        using var context = new LanguageCenterContext();
        return context.Semesters
            .Any(s => s.Name == name && (excludeId == null || s.SemesterId != excludeId));
    }

    public static int CountClasses(int semesterId)
    {
        using var context = new LanguageCenterContext();
        return context.Classes.Count(c => c.SemesterId == semesterId);
    }

    public static void Save(Semester semester)
    {
        using var context = new LanguageCenterContext();
        context.Semesters.Add(semester);
        context.SaveChanges();
    }

    public static void Update(Semester semester)
    {
        using var context = new LanguageCenterContext();
        var existing = context.Semesters.FirstOrDefault(s => s.SemesterId == semester.SemesterId);
        if (existing == null) return;
        existing.Name = semester.Name;
        existing.StartDate = semester.StartDate;
        existing.EndDate = semester.EndDate;
        existing.SetupEndDate = semester.SetupEndDate;
        context.SaveChanges();
    }

    public static void Delete(int id)
    {
        using var context = new LanguageCenterContext();
        var semester = context.Semesters.FirstOrDefault(s => s.SemesterId == id);
        if (semester == null) return;
        context.Semesters.Remove(semester);
        context.SaveChanges();
    }
}
