using BusinessObjects;

namespace DataAccessObjects;

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

    public static Semester? GetActive()
    {
        using var context = new LanguageCenterContext();
        return context.Semesters.FirstOrDefault(s => s.IsActive);
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
        existing.IsActive = semester.IsActive;
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
