using BusinessObjects;

namespace DataAccessObjects;

// StudentDAO — EF data access for Student (CRUD + queries).

public class StudentDAO
{
    public static List<Student> GetAll()
    {
        using var context = new LanguageCenterContext();
        return context.Students.ToList();
    }

    public static Student? GetById(int id)
    {
        using var context = new LanguageCenterContext();
        return context.Students.FirstOrDefault(x => x.StudentId == id);
    }

    /// <summary>The student profile belonging to a user account, or null.</summary>
    public static Student? GetByUserId(int userId)
    {
        using var context = new LanguageCenterContext();
        return context.Students.FirstOrDefault(x => x.UserId == userId);
    }

    public static void Save(Student entity)
    {
        using var context = new LanguageCenterContext();
        context.Students.Add(entity);
        context.SaveChanges();
    }

    public static void Update(Student entity)
    {
        using var context = new LanguageCenterContext();
        var existing = context.Students.Find(entity.StudentId);
        if (existing == null) return;
        context.Entry(existing).CurrentValues.SetValues(entity);
        context.SaveChanges();
    }

    public static void Delete(int id)
    {
        using var context = new LanguageCenterContext();
        var existing = context.Students.Find(id);
        if (existing == null) return;
        context.Students.Remove(existing);
        context.SaveChanges();
    }
}
