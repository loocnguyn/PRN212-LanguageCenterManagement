using BusinessObjects;
using Microsoft.EntityFrameworkCore;

namespace DataAccessObjects;

// ============================================================
//  GradeDAO — per-enrollment grade rows (one per grade component).
//  CONTENTS:
//    1. CRUD      — GetAll/GetById/Save/Update/Delete
//    2. Queries   — by enrollment, by many enrollments, by student
//    3. Upsert    — insert-or-update a student's grade for a component
// ============================================================
public class GradeDAO
{
    public static List<Grade> GetAll()
    {
        using var context = new LanguageCenterContext();
        return context.Grades
            .Include(g => g.Component)
            .Include(g => g.Enrollment)
            .ToList();
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
            .Include(g => g.Component)
            .ToList();
    }

    /// <summary>Batch-load grades for multiple enrollment IDs (avoids N+1).</summary>
    public static List<Grade> GetByEnrollmentIds(List<int> enrollmentIds)
    {
        if (enrollmentIds == null || enrollmentIds.Count == 0)
            return new List<Grade>();

        using var context = new LanguageCenterContext();
        return context.Grades
            .Where(g => enrollmentIds.Contains(g.EnrollmentId))
            .Include(g => g.Component)
            .ToList();
    }

    public static void Upsert(Grade entity)
    {
        using var context = new LanguageCenterContext();
        var existing = context.Grades
            .FirstOrDefault(g => g.EnrollmentId == entity.EnrollmentId && g.ComponentId == entity.ComponentId);
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

    public static void BulkUpsert(List<Grade> entities)
    {
        if (entities == null || !entities.Any()) return;

        using var context = new LanguageCenterContext();
        using var transaction = context.Database.BeginTransaction();
        try
        {
            foreach (var entity in entities)
            {
                var existing = context.Grades
                    .FirstOrDefault(g => g.EnrollmentId == entity.EnrollmentId && g.ComponentId == entity.ComponentId);
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
            }
            context.SaveChanges();
            transaction.Commit();
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            throw new Exception($"Error during bulk saving grades: {ex.Message}", ex);
        }
    }

    public static List<Grade> GetByStudentId(int studentId)
    {
        using var context = new LanguageCenterContext();
        return context.Grades
            .Where(g => g.Enrollment.StudentId == studentId)
            .Include(g => g.Component)
            .Include(g => g.Enrollment)
            .ThenInclude(e => e.Class)
            .ThenInclude(c => c.Course)
            .Include(g => g.Enrollment)
            .ThenInclude(e => e.Class)
            .ThenInclude(c => c.Semester)
            .ToList();
    }
}
