using BusinessObjects;

namespace DataAccessObjects;

// GradeTypeDAO — EF data access for GradeType (CRUD + queries).

public class GradeTypeDAO
{
    public static List<GradeType> GetAll()
    {
        using var context = new LanguageCenterContext();
        return context.GradeTypes.ToList();
    }

    public static GradeType? GetById(int id)
    {
        using var context = new LanguageCenterContext();
        return context.GradeTypes.FirstOrDefault(x => x.GradeTypeId == id);
    }

    /// <summary>Grade types (with their weights) configured for a specific course — the grading structure for that subject.</summary>
    public static List<GradeType> GetByCourseId(int courseId)
    {
        using var context = new LanguageCenterContext();
        return context.GradeTypes
            .Where(x => x.CourseId == courseId)
            .OrderBy(x => x.GradeTypeId)
            .ToList();
    }

    public static void Save(GradeType entity)
    {
        using var context = new LanguageCenterContext();
        context.GradeTypes.Add(entity);
        context.SaveChanges();
    }

    public static void Update(GradeType entity)
    {
        using var context = new LanguageCenterContext();
        var existing = context.GradeTypes.Find(entity.GradeTypeId);
        if (existing == null) return;
        context.Entry(existing).CurrentValues.SetValues(entity);
        context.SaveChanges();
    }

    public static void Delete(int id)
    {
        using var context = new LanguageCenterContext();
        var existing = context.GradeTypes.Find(id);
        if (existing == null) return;
        context.GradeTypes.Remove(existing);
        context.SaveChanges();
    }
}
