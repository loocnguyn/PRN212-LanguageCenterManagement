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
        var existing = context.Grades.Find(entity.GradeId)
            ?? throw new Exception("Grade not found.");
        context.Entry(existing).CurrentValues.SetValues(entity);
        context.SaveChanges();
    }

    public static void Delete(int id)
    {
        using var context = new LanguageCenterContext();
        var existing = context.Grades.Find(id)
            ?? throw new Exception("Grade not found.");
        context.Grades.Remove(existing);
        context.SaveChanges();
    }
}
