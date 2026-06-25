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
        context.Grades.Update(entity);
        context.SaveChanges();
    }

    public static void Delete(int id)
    {
        using var context = new LanguageCenterContext();
        var entity = context.Grades.Find(id);
        if (entity != null)
        {
            context.Grades.Remove(entity);
            context.SaveChanges();
        }
    }
}

