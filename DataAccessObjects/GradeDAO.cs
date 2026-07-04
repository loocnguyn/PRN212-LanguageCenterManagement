using BusinessObjects;
using Microsoft.EntityFrameworkCore;

namespace DataAccessObjects;

public class GradeDAO
{
    public static List<Grade> GetAll()
    {
        using var context = new LanguageCenterContext();
        return context.Grades.ToList();
    }

    public static Grade? GetById(int id)
    {
        using var context = new LanguageCenterContext();
        return context.Grades.FirstOrDefault(x => x.GradeId == id);
    }

    public static void Save(Grade entity)
    {
        using var context = new LanguageCenterContext();
        context.Grades.Add(entity);
        context.SaveChanges();
    }

    public static void Update(Grade entity)
    {
        using var context = new LanguageCenterContext();
        var existing = context.Grades.Find(entity.GradeId);
        if (existing == null) return;
        context.Entry(existing).CurrentValues.SetValues(entity);
        context.SaveChanges();
    }

    public static void Delete(int id)
    {
        using var context = new LanguageCenterContext();
        var existing = context.Grades.Find(id);
        if (existing == null) return;
        context.Grades.Remove(existing);
        context.SaveChanges();
    }

    public static List<Grade> GetByEnrollmentId(int enrollmentId)
    {
        using var context = new LanguageCenterContext();
        return context.Grades
            .Where(g => g.EnrollmentId == enrollmentId)
            .Include(g => g.GradeType)
            .ToList();
    }

    public static void Upsert(Grade entity)
    {
        using var context = new LanguageCenterContext();
        var existing = context.Grades
            .FirstOrDefault(g => g.EnrollmentId == entity.EnrollmentId && g.GradeTypeId == entity.GradeTypeId);
        if (existing != null)
        {
            existing.Score = entity.Score;
            existing.MaxScore = entity.MaxScore;
            existing.Note = entity.Note;
            existing.GradedAt = entity.GradedAt;
        }
        else
        {
            context.Grades.Add(entity);
        }
        context.SaveChanges();
    }
}
